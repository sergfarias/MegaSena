
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using MegaSena.Application.Models;
using MegaSena.Application.Portas;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace MegaSena.Api.Tests;

/// <summary>S1 (refresh token com rotação) e S2 (cadastro neutro, login também por e-mail).</summary>
public class SessaoRefreshTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SessaoRefreshTests(CustomWebApplicationFactory factory) => _factory = factory;

    private sealed class EmailCapturador : IEmailSender
    {
        public readonly List<EmailMensagem> Enviados = new();

        public Task<bool> EnviarAsync(EmailMensagem mensagem, CancellationToken ct = default)
        {
            lock (Enviados) Enviados.Add(mensagem);
            return Task.FromResult(true);
        }
    }

    private (HttpClient Client, EmailCapturador Email) CriarCliente()
    {
        var email = new EmailCapturador();
        var client = _factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.RemoveAll<IEmailSender>();
            s.AddSingleton<IEmailSender>(email);
        })).CreateClient();
        return (client, email);
    }

    private sealed record Sessao(string Login, string Email, string Token, string RefreshToken);

    private static async Task<Sessao> RegistrarELogarAsync(HttpClient client)
    {
        var sufixo = Guid.NewGuid().ToString("N");
        var login = $"u{sufixo}"[..20];
        var email = $"u{sufixo}@teste.com";

        var reg = await client.PostAsJsonAsync("/api/Auth/registrar", new { nome = "Teste", login, email, senha = "senha123456" });
        Assert.Equal(HttpStatusCode.OK, reg.StatusCode);

        return await LogarAsync(client, login, email, "senha123456");
    }

    private static async Task<Sessao> LogarAsync(HttpClient client, string loginOuEmail, string email, string senha)
    {
        var resp = await client.PostAsJsonAsync("/api/Auth/login", new { login = loginOuEmail, senha });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return new Sessao(
            doc.RootElement.GetProperty("usuario").GetProperty("login").GetString()!,
            email,
            doc.RootElement.GetProperty("token").GetString()!,
            doc.RootElement.GetProperty("refreshToken").GetString()!);
    }

    private static Task<HttpResponseMessage> Renovar(HttpClient client, string refreshToken) =>
        client.PostAsJsonAsync("/api/Auth/refresh", new { refreshToken });

    private static async Task<HttpStatusCode> MeusLotesAsync(HttpClient client, string token)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/Auditoria/meus-lotes");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (await client.SendAsync(req)).StatusCode;
    }

    // ----------------------------------------------------------------- S1

    [Fact]
    public async Task Login_DevolveRefreshToken_ERefreshGeraTokenQueFunciona()
    {
        var (client, _) = CriarCliente();
        var sessao = await RegistrarELogarAsync(client);

        var resp = await Renovar(client, sessao.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var novoToken = doc.RootElement.GetProperty("token").GetString()!;
        var novoRefresh = doc.RootElement.GetProperty("refreshToken").GetString()!;

        Assert.NotEqual(sessao.RefreshToken, novoRefresh);   // rotação: o refresh muda a cada uso
        Assert.Equal(HttpStatusCode.OK, await MeusLotesAsync(client, novoToken));
    }

    [Fact]
    public async Task Refresh_ReusoDeTokenJaTrocado_DerrubaAFamiliaInteira()
    {
        var (client, _) = CriarCliente();
        var sessao = await RegistrarELogarAsync(client);

        var primeira = await Renovar(client, sessao.RefreshToken);
        using var doc = JsonDocument.Parse(await primeira.Content.ReadAsStringAsync());
        var refreshNovo = doc.RootElement.GetProperty("refreshToken").GetString()!;

        // Alguém apresenta de novo o token antigo (já trocado): recusa...
        Assert.Equal(HttpStatusCode.Unauthorized, (await Renovar(client, sessao.RefreshToken)).StatusCode);

        // ...e o token novo, que era legítimo, também deixa de valer (a cadeia foi revogada)
        Assert.Equal(HttpStatusCode.Unauthorized, (await Renovar(client, refreshNovo)).StatusCode);
    }

    [Fact]
    public async Task Logout_RevogaORefreshToken()
    {
        var (client, _) = CriarCliente();
        var sessao = await RegistrarELogarAsync(client);

        var sair = await client.PostAsJsonAsync("/api/Auth/logout", new { refreshToken = sessao.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, sair.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await Renovar(client, sessao.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task Refresh_TokenInventado_RetornaUnauthorized()
    {
        var (client, _) = CriarCliente();

        Assert.Equal(HttpStatusCode.Unauthorized, (await Renovar(client, "isto-nao-e-um-refresh-token")).StatusCode);
    }

    [Fact]
    public async Task Refresh_AposRedefinirSenha_RetornaUnauthorized()
    {
        var (client, email) = CriarCliente();
        var sessao = await RegistrarELogarAsync(client);

        await client.PostAsJsonAsync("/api/Auth/esqueci-senha", new { email = sessao.Email });
        var msg = Assert.Single(email.Enviados);
        var tokenReset = Regex.Match(msg.CorpoHtml, @"#token=([A-Za-z0-9_\-]+)").Groups[1].Value;
        var reset = await client.PostAsJsonAsync("/api/Auth/resetar-senha", new { token = tokenReset, novaSenha = "novaSenha98765" });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await Renovar(client, sessao.RefreshToken)).StatusCode);

        // Quem entra com a senha nova ganha uma sessão nova que funciona
        var nova = await LogarAsync(client, sessao.Login, sessao.Email, "novaSenha98765");
        Assert.Equal(HttpStatusCode.OK, (await Renovar(client, nova.RefreshToken)).StatusCode);
    }

    // ----------------------------------------------------------------- S2

    [Fact]
    public async Task Registrar_LoginJaEmUso_ResponderIgualEAindaCriaAConta()
    {
        var (client, _) = CriarCliente();
        var primeira = await RegistrarELogarAsync(client);

        var segundoEmail = $"outro{Guid.NewGuid():N}@teste.com";
        var resp = await client.PostAsJsonAsync("/api/Auth/registrar",
            new { nome = "Outra Pessoa", login = primeira.Login, email = segundoEmail, senha = "senha123456" });

        // Mesma resposta de qualquer cadastro novo: nada revela que o login já existia
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("Cadastro recebido", await resp.Content.ReadAsStringAsync());

        // A conta nova existe e entra pelo e-mail; o login dela ganhou um número
        var segunda = await LogarAsync(client, segundoEmail, segundoEmail, "senha123456");
        Assert.NotEqual(primeira.Login, segunda.Login);
        Assert.StartsWith(primeira.Login, segunda.Login);

        // E a primeira conta continua intacta
        Assert.Equal(HttpStatusCode.OK, await MeusLotesAsync(client, primeira.Token));
    }

    [Fact]
    public async Task Login_ComEmail_FuncionaIgualAoLogin()
    {
        var (client, _) = CriarCliente();
        var sessao = await RegistrarELogarAsync(client);

        var porEmail = await LogarAsync(client, sessao.Email, sessao.Email, "senha123456");

        Assert.Equal(sessao.Login, porEmail.Login);
    }
}

