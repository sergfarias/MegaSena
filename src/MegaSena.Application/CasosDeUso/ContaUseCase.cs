
using System.Net;
using System.Security.Cryptography;
using System.Text;
using MegaSena.Application.Models;
using MegaSena.Application.Portas;
using MegaSena.Application.Servicos;
using Microsoft.Extensions.Configuration;

namespace MegaSena.Application.CasosDeUso;

// S2: "login em uso" deixou de ser um resultado. O cadastro responde sempre da mesma forma.
public enum StatusRegistro { Criado, EmailEmUso, Falha }

public enum StatusLogin { Sucesso, CredenciaisInvalidas, ContaDesativada }

public sealed record ResultadoLogin(
    StatusLogin Status, string? Token = null, UsuarioModel? Usuario = null, string? RefreshToken = null);

public enum StatusRenovacao { Sucesso, Invalido }

public sealed record ResultadoRenovacao(StatusRenovacao Status, string? Token = null, string? RefreshToken = null);

public sealed class ContaUseCase
{
    // Hash BCrypt fictício: gasta o mesmo tempo quando o usuário não existe (evita enumeração por tempo)
    private const string HashFicticio = "$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy";

    private const int TamanhoMaximoLogin = 50;   // mesmo limite do RegistroRequest

    private readonly IUsuarioRepository _usuarios;
    private readonly AuthService _auth;
    private readonly IEmailSender _email;
    private readonly IConfiguration _config;
    private readonly IRefreshTokenRepository? _refresh;

    // O refresh token é opcional no construtor para não quebrar quem monta o caso de uso sem ele (testes);
    // com a injeção de dependência da API ele sempre vem preenchido.
    public ContaUseCase(
        IUsuarioRepository usuarios, AuthService auth, IEmailSender email, IConfiguration config,
        IRefreshTokenRepository? refresh = null)
    {
        _usuarios = usuarios;
        _auth = auth;
        _email = email;
        _config = config;
        _refresh = refresh;
    }

    // ------------------------------------------------------------------ Cadastro

    public async Task<StatusRegistro> RegistrarAsync(string nome, string login, string email, string senha)
    {
        if (await _usuarios.EmailExisteAsync(email))
        {
            _auth.GerarHashSenha("dummy-timing-attack");

            var dono = await _usuarios.ObterPorEmailAsync(email);
            if (dono != null && !string.IsNullOrWhiteSpace(dono.Email))
                _ = EnviarSemFalharAsync(MontarEmailContaExistente(dono));

            return StatusRegistro.EmailEmUso;
        }

        // S2: login já usado por outra pessoa não é erro. Escolhe-se um login livre (login, login2, login3...)
        // e a resposta continua igual, sem revelar quais logins existem. O e-mail também serve para entrar.
        var loginLivre = await EscolherLoginLivreAsync(login);

        var criado = await _usuarios.CriarAsync(nome, loginLivre, email, _auth.GerarHashSenha(senha));
        return criado ? StatusRegistro.Criado : StatusRegistro.Falha;
    }

    private async Task<string> EscolherLoginLivreAsync(string desejado)
    {
        if (!await _usuarios.LoginExisteAsync(desejado))
            return desejado;

        for (int i = 2; i <= 200; i++)
        {
            var sufixo = i.ToString();
            var raiz = desejado.Length + sufixo.Length > TamanhoMaximoLogin
                ? desejado[..(TamanhoMaximoLogin - sufixo.Length)]
                : desejado;
            var candidato = raiz + sufixo;

            if (!await _usuarios.LoginExisteAsync(candidato))
                return candidato;
        }

        // Último recurso: sufixo aleatório
        var aleatorio = Guid.NewGuid().ToString("N")[..8];
        return desejado[..Math.Min(desejado.Length, TamanhoMaximoLogin - aleatorio.Length)] + aleatorio;
    }

    // ------------------------------------------------------------------ Login e sessão

    public async Task<ResultadoLogin> LoginAsync(string login, string senha)
    {
        // S2: aceita o login ou o e-mail (o login pode ter ganho um número no cadastro)
        var usuario = await _usuarios.ObterPorLoginAsync(login);
        if (usuario == null && login.Contains('@'))
            usuario = await _usuarios.ObterPorEmailAsync(login);

        if (usuario == null)
        {
            _auth.VerificarSenha(senha, HashFicticio);
            return new ResultadoLogin(StatusLogin.CredenciaisInvalidas);
        }

        if (!_auth.VerificarSenha(senha, usuario.SenhaHash))
            return new ResultadoLogin(StatusLogin.CredenciaisInvalidas);

        if (!usuario.Ativo)
            return new ResultadoLogin(StatusLogin.ContaDesativada);

        var token = GerarJwt(usuario);
        var refresh = await EmitirRefreshAsync(usuario, familiaId: Guid.NewGuid());
        return new ResultadoLogin(StatusLogin.Sucesso, token, usuario, refresh);
    }

    /// <summary>
    /// S1: troca um refresh token por um JWT novo e um refresh novo (rotação: cada refresh vale uma vez).
    /// Reuso de um token já trocado derruba a cadeia inteira, porque indica que o token vazou.
    /// </summary>
    public async Task<ResultadoRenovacao> RenovarAsync(string refreshToken)
    {
        var invalido = new ResultadoRenovacao(StatusRenovacao.Invalido);
        if (_refresh is null || string.IsNullOrWhiteSpace(refreshToken)) return invalido;

        var agora = DateTime.UtcNow;
        var rt = await _refresh.ObterPorHashAsync(HashDoToken(refreshToken));
        if (rt is null) return invalido;

        // Token já usado ou revogado sendo apresentado de novo: derruba a família
        if (rt.UsadoEmUtc != null || rt.RevogadoEmUtc != null)
        {
            await _refresh.RevogarFamiliaAsync(rt.FamiliaId, agora);
            return invalido;
        }

        if (rt.ExpiraEmUtc <= agora) return invalido;

        var usuario = await _usuarios.ObterPorIdAsync(rt.UsuarioId);
        if (usuario is null || !usuario.Ativo || rt.CarimboSenha != AuthService.CarimboDaSenha(usuario.SenhaHash))
        {
            // Conta desativada, removida ou senha trocada desde a emissão
            await _refresh.RevogarFamiliaAsync(rt.FamiliaId, agora);
            return invalido;
        }

        // Perdeu a corrida contra outro pedido com o mesmo token: trata como reuso
        if (!await _refresh.TentarConsumirAsync(rt.Id, agora))
        {
            await _refresh.RevogarFamiliaAsync(rt.FamiliaId, agora);
            return invalido;
        }

        var novoRefresh = await EmitirRefreshAsync(usuario, rt.FamiliaId);
        return new ResultadoRenovacao(StatusRenovacao.Sucesso, GerarJwt(usuario), novoRefresh);
    }

    /// <summary>Encerra a sessão: revoga a cadeia do refresh token informado. Sempre termina sem erro.</summary>
    public async Task SairAsync(string refreshToken)
    {
        if (_refresh is null || string.IsNullOrWhiteSpace(refreshToken)) return;

        var rt = await _refresh.ObterPorHashAsync(HashDoToken(refreshToken));
        if (rt != null)
            await _refresh.RevogarFamiliaAsync(rt.FamiliaId, DateTime.UtcNow);
    }

    private string GerarJwt(UsuarioModel usuario) =>
        _auth.GerarTokenJwt(usuario.Id, usuario.Nome, usuario.Email ?? "", usuario.Perfil,
                            AuthService.CarimboDaSenha(usuario.SenhaHash));

    private async Task<string?> EmitirRefreshAsync(UsuarioModel usuario, Guid familiaId)
    {
        if (_refresh is null) return null;

        var bruto = NovoTokenOpaco();
        await _refresh.AdicionarAsync(
            usuario.Id, familiaId, HashDoToken(bruto),
            AuthService.CarimboDaSenha(usuario.SenhaHash),
            DateTime.UtcNow.AddDays(_auth.RefreshDias));
        return bruto;
    }

    private static string NovoTokenOpaco() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").Replace("=", "");

    // ------------------------------------------------------------------ Recuperação de senha

    /// <summary>
    /// Gera o token de recuperação e envia o e-mail em segundo plano.
    /// Devolve o token original (só para o controller expor em Development) ou null se o e-mail não existe.
    /// </summary>
    public async Task<string?> SolicitarRecuperacaoAsync(string email)
    {
        var usuario = await _usuarios.ObterPorEmailAsync(email);
        if (usuario == null) return null;

        var token = NovoTokenOpaco();

        await _usuarios.DefinirTokenResetAsync(usuario.Id, HashDoToken(token), DateTime.UtcNow.AddHours(1));

        if (!string.IsNullOrWhiteSpace(usuario.Email))
            _ = EnviarSemFalharAsync(MontarEmailRecuperacao(usuario, token));

        return token;
    }

    public Task<bool> RedefinirSenhaAsync(string token, string novaSenha) =>
        _usuarios.RedefinirSenhaAsync(HashDoToken(token), _auth.GerarHashSenha(novaSenha), DateTime.UtcNow);

    private static string HashDoToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private async Task EnviarSemFalharAsync(EmailMensagem mensagem)
    {
        try { await _email.EnviarAsync(mensagem); }
        catch { /* a falha já é registrada pelo remetente; a resposta ao usuário é sempre neutra */ }
    }

    private static EmailMensagem MontarEmailContaExistente(UsuarioModel usuario)
    {
        var nome = WebUtility.HtmlEncode(usuario.Nome);
        var corpo =
            $"<p>Olá, {nome}!</p>" +
            "<p>Alguém tentou criar uma nova conta no MegaSena SaaS usando este e-mail, mas ele já tem cadastro.</p>" +
            "<p>Se foi você, entre com o seu login ou e-mail, ou use a opção \"Esqueci minha senha\" na tela de login.</p>" +
            "<p>Se não foi você, ignore este e-mail: nada foi alterado.</p>";

        return new EmailMensagem(usuario.Email!, "Tentativa de cadastro - MegaSena SaaS", corpo);
    }

    private EmailMensagem MontarEmailRecuperacao(UsuarioModel usuario, string token)
    {
        var baseUrl = _config["App:FrontUrl"]
            ?? _config.GetSection("Cors:Origens").GetChildren().Select(c => c.Value).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

        var nome = WebUtility.HtmlEncode(usuario.Nome);
        string instrucao = baseUrl is null
            ? $"<p>Use este código na página de recuperação de senha:</p><p><code>{token}</code></p>"
            : $"<p><a href=\"{baseUrl.TrimEnd('/')}/recuperar-senha.html#token={Uri.EscapeDataString(token)}\">Clique aqui para criar uma nova senha</a>.</p>";

        var corpo =
            $"<p>Olá, {nome}!</p>" +
            "<p>Recebemos um pedido para redefinir a senha da sua conta no MegaSena SaaS.</p>" +
            instrucao +
            "<p>O link vale por 1 hora. Se você não fez esse pedido, ignore este e-mail: sua senha continua a mesma.</p>";

        return new EmailMensagem(usuario.Email!, "Recuperação de senha - MegaSena SaaS", corpo);
    }
}
