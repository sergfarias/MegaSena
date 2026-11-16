using MegaSena.Application.CasosDeUso;
using MegaSena.Application.Models;
using MegaSena.Application.Portas;
using MegaSena.Application.Servicos;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace MegaSena.Application.Tests.CasosDeUso;

public class ContaUseCaseTests
{
    private sealed class UsuarioFalso
    {
        public Guid Id = Guid.NewGuid();
        public string Nome = "";
        public string Login = "";
        public string? Email;
        public string SenhaHash = "";
        public bool Ativo = true;
        public string Perfil = "Cliente";
        public string? ResetToken;
        public DateTime? ResetExpira;
    }

    private sealed class RepositorioFalso : IUsuarioRepository
    {
        public readonly List<UsuarioFalso> Usuarios = new();

        private static UsuarioModel? Modelo(UsuarioFalso? u) =>
            u == null ? null : new UsuarioModel(u.Id, u.Nome, u.Login, u.Email, u.SenhaHash, u.Ativo, u.Perfil);

        public Task<bool> LoginExisteAsync(string login) => Task.FromResult(Usuarios.Any(u => u.Login == login));
        public Task<bool> EmailExisteAsync(string email) => Task.FromResult(Usuarios.Any(u => u.Email == email));

        public Task<UsuarioModel?> ObterPorLoginAsync(string login) =>
            Task.FromResult(Modelo(Usuarios.FirstOrDefault(u => u.Login == login)));

        public Task<UsuarioModel?> ObterPorEmailAsync(string email) =>
            Task.FromResult(Modelo(Usuarios.FirstOrDefault(u => u.Email == email)));

        public Task<UsuarioModel?> ObterPorIdAsync(Guid id) =>
            Task.FromResult(Modelo(Usuarios.FirstOrDefault(u => u.Id == id)));

        public Task<bool> CriarAsync(string nome, string login, string email, string senhaHash)
        {
            Usuarios.Add(new UsuarioFalso { Nome = nome, Login = login, Email = email, SenhaHash = senhaHash });
            return Task.FromResult(true);
        }

        public Task DefinirTokenResetAsync(Guid usuarioId, string tokenHash, DateTime expiraEmUtc)
        {
            var u = Usuarios.First(x => x.Id == usuarioId);
            u.ResetToken = tokenHash;
            u.ResetExpira = expiraEmUtc;
            return Task.CompletedTask;
        }

        public Task<bool> RedefinirSenhaAsync(string tokenHash, string novaSenhaHash, DateTime agoraUtc)
        {
            var u = Usuarios.FirstOrDefault(x => x.ResetToken == tokenHash);
            if (u == null || u.ResetExpira == null || u.ResetExpira < agoraUtc) return Task.FromResult(false);

            u.SenhaHash = novaSenhaHash;
            u.ResetToken = null;
            u.ResetExpira = null;
            return Task.FromResult(true);
        }
    }

    private sealed class EmailFalso : IEmailSender
    {
        public readonly List<EmailMensagem> Enviados = new();

        public Task<bool> EnviarAsync(EmailMensagem mensagem, CancellationToken ct = default)
        {
            Enviados.Add(mensagem);
            return Task.FromResult(true);
        }
    }

    private static (ContaUseCase Conta, RepositorioFalso Repo, EmailFalso Email) Criar()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "chave-de-teste-com-mais-de-32-bytes-aqui-123456",
                ["Jwt:Issuer"] = "MegaSenaApi",
                ["Jwt:Audience"] = "MegaSenaFront",
                ["App:FrontUrl"] = "https://front.teste/"
            })
            .Build();

        var repo = new RepositorioFalso();
        var email = new EmailFalso();
        return (new ContaUseCase(repo, new AuthService(config), email, config), repo, email);
    }

    [Fact]
    public async Task Registrar_UsuarioNovo_DeveCriar()
    {
        var (conta, repo, _) = Criar();

        var status = await conta.RegistrarAsync("Maria", "maria", "maria@teste.com", "senha12345");

        Assert.Equal(StatusRegistro.Criado, status);
        Assert.Single(repo.Usuarios);
        Assert.NotEqual("senha12345", repo.Usuarios[0].SenhaHash);
    }

    [Fact]
    public async Task Registrar_EmailRepetido_DeveRecusarSemCriarOutraConta()
    {
        var (conta, repo, _) = Criar();
        await conta.RegistrarAsync("Maria", "maria", "maria@teste.com", "senha12345");

        Assert.Equal(StatusRegistro.EmailEmUso, await conta.RegistrarAsync("Outra", "outra", "maria@teste.com", "senha12345"));
        Assert.Single(repo.Usuarios);
    }

    [Fact]
    public async Task Registrar_LoginRepetido_CriaComLoginNumerado()
    {
        var (conta, repo, _) = Criar();
        await conta.RegistrarAsync("Maria", "maria", "maria@teste.com", "senha12345");

        var status = await conta.RegistrarAsync("Outra", "maria", "outra@teste.com", "senha12345");

        Assert.Equal(StatusRegistro.Criado, status);
        Assert.Equal(new[] { "maria", "maria2" }, repo.Usuarios.Select(u => u.Login).ToArray());
        Assert.Equal(StatusLogin.Sucesso, (await conta.LoginAsync("outra@teste.com", "senha12345")).Status);
    }

    [Fact]
    public async Task Login_CredenciaisCorretas_DeveGerarToken()
    {
        var (conta, _, _) = Criar();
        await conta.RegistrarAsync("Maria", "maria", "maria@teste.com", "senha12345");

        var r = await conta.LoginAsync("maria", "senha12345");

        Assert.Equal(StatusLogin.Sucesso, r.Status);
        Assert.False(string.IsNullOrWhiteSpace(r.Token));
        Assert.Equal("maria", r.Usuario!.Login);
    }

    [Fact]
    public async Task Login_SenhaErradaOuUsuarioInexistente_DeveRecusarSemDistinguir()
    {
        var (conta, _, _) = Criar();
        await conta.RegistrarAsync("Maria", "maria", "maria@teste.com", "senha12345");

        Assert.Equal(StatusLogin.CredenciaisInvalidas, (await conta.LoginAsync("maria", "errada123")).Status);
        Assert.Equal(StatusLogin.CredenciaisInvalidas, (await conta.LoginAsync("ninguem", "senha12345")).Status);
    }

    [Fact]
    public async Task Login_ContaDesativada_DeveRecusar()
    {
        var (conta, repo, _) = Criar();
        await conta.RegistrarAsync("Maria", "maria", "maria@teste.com", "senha12345");
        repo.Usuarios[0].Ativo = false;

        var r = await conta.LoginAsync("maria", "senha12345");

        Assert.Equal(StatusLogin.ContaDesativada, r.Status);
        Assert.Null(r.Token);
    }

    [Fact]
    public async Task SolicitarRecuperacao_EmailExistente_DeveEnviarEmailComLink()
    {
        var (conta, repo, email) = Criar();
        await conta.RegistrarAsync("Maria", "maria", "maria@teste.com", "senha12345");

        var token = await conta.SolicitarRecuperacaoAsync("maria@teste.com");

        Assert.NotNull(token);
        var msg = Assert.Single(email.Enviados);
        Assert.Equal("maria@teste.com", msg.Para);
        Assert.Contains($"https://front.teste/recuperar-senha.html#token={token}", msg.CorpoHtml);
        Assert.NotEqual(token, repo.Usuarios[0].ResetToken); // só o hash fica guardado
    }

    [Fact]
    public async Task SolicitarRecuperacao_EmailInexistente_NaoDeveEnviarNada()
    {
        var (conta, _, email) = Criar();

        var token = await conta.SolicitarRecuperacaoAsync("ninguem@teste.com");

        Assert.Null(token);
        Assert.Empty(email.Enviados);
    }

    [Fact]
    public async Task RedefinirSenha_TokenValido_TrocaASenhaEInvalidaOToken()
    {
        var (conta, _, _) = Criar();
        await conta.RegistrarAsync("Maria", "maria", "maria@teste.com", "senha12345");
        var token = await conta.SolicitarRecuperacaoAsync("maria@teste.com");

        Assert.True(await conta.RedefinirSenhaAsync(token!, "novaSenha999"));

        Assert.Equal(StatusLogin.Sucesso, (await conta.LoginAsync("maria", "novaSenha999")).Status);
        Assert.Equal(StatusLogin.CredenciaisInvalidas, (await conta.LoginAsync("maria", "senha12345")).Status);
        Assert.False(await conta.RedefinirSenhaAsync(token!, "outraSenha000")); // token de uso único
    }

    [Fact]
    public async Task RedefinirSenha_TokenInvalidoOuExpirado_DeveRecusar()
    {
        var (conta, repo, _) = Criar();
        await conta.RegistrarAsync("Maria", "maria", "maria@teste.com", "senha12345");

        Assert.False(await conta.RedefinirSenhaAsync("token-que-nao-existe", "novaSenha999"));

        var token = await conta.SolicitarRecuperacaoAsync("maria@teste.com");
        repo.Usuarios[0].ResetExpira = DateTime.UtcNow.AddMinutes(-1);

        Assert.False(await conta.RedefinirSenhaAsync(token!, "novaSenha999"));
    }

    [Fact]
    public async Task Registrar_EmailRepetido_AvisaODonoDoEmailSemCriarOutroUsuario()
    {
        var (conta, repo, email) = Criar();
        await conta.RegistrarAsync("Maria", "maria", "maria@teste.com", "senha12345");

        var status = await conta.RegistrarAsync("Outra", "outra", "maria@teste.com", "senha12345");

        Assert.Equal(StatusRegistro.EmailEmUso, status);
        Assert.Single(repo.Usuarios);
        var msg = Assert.Single(email.Enviados);
        Assert.Equal("maria@teste.com", msg.Para);
    }
}