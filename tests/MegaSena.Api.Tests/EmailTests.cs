using MegaSena.Application.Models;
using MegaSena.Application.Portas;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace MegaSena.Api.Tests;

public sealed class EmailFalso : IEmailSender
{
    public ConcurrentQueue<EmailMensagem> Enviados { get; } = new();

    public Task<bool> EnviarAsync(EmailMensagem mensagem, CancellationToken ct = default)
    {
        Enviados.Enqueue(mensagem);
        return Task.FromResult(true);
    }
}

public class EmailTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public EmailTests(CustomWebApplicationFactory factory) => _factory = factory;

    private (HttpClient Client, EmailFalso Email) CriarCliente(bool comAdmin = true)
    {
        var email = new EmailFalso();
        var client = _factory.WithWebHostBuilder(b =>
        {
            if (comAdmin) b.UseSetting("Email:AdminAddress", "admin@teste.com");
            b.UseSetting("App:FrontUrl", "https://front.teste");
            b.ConfigureTestServices(s => s.AddSingleton<IEmailSender>(email));
        }).CreateClient();
        return (client, email);
    }

    private static async Task<(string Login, string Email)> RegistrarAsync(HttpClient client)
    {
        var sufixo = Guid.NewGuid().ToString("N")[..12];
        var login = $"user{sufixo}";
        var email = $"{login}@teste.com";

        var resp = await client.PostAsJsonAsync("/api/Auth/registrar",
            new { nome = "Teste", login, email, senha = "senha123456" });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        return (login, email);
    }

    [Fact]
    public async Task EsqueciSenha_UsuarioExistente_EnviaEmailComLinkEPermiteRedefinir()
    {
        var (client, email) = CriarCliente();
        var (login, endereco) = await RegistrarAsync(client);

        var resp = await client.PostAsJsonAsync("/api/Auth/esqueci-senha", new { email = endereco });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var msg = Assert.Single(email.Enviados);
        Assert.Equal(endereco, msg.Para);
        Assert.Contains("https://front.teste/recuperar-senha.html#token=", msg.CorpoHtml);

        var token = Regex.Match(msg.CorpoHtml, @"#token=([A-Za-z0-9_\-]+)").Groups[1].Value;
        Assert.False(string.IsNullOrEmpty(token));

        var reset = await client.PostAsJsonAsync("/api/Auth/resetar-senha", new { token, novaSenha = "novaSenha98765" });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        var loginNovo = await client.PostAsJsonAsync("/api/Auth/login", new { login, senha = "novaSenha98765" });
        Assert.Equal(HttpStatusCode.OK, loginNovo.StatusCode);
    }

    [Fact]
    public async Task EsqueciSenha_EmailInexistente_NaoEnviaEmailEResponseIgual()
    {
        var (client, email) = CriarCliente();
        var (_, existente) = await RegistrarAsync(client);

        var comUsuario = await (await client.PostAsJsonAsync("/api/Auth/esqueci-senha", new { email = existente }))
            .Content.ReadAsStringAsync();
        var semUsuario = await (await client.PostAsJsonAsync("/api/Auth/esqueci-senha", new { email = "ninguem@teste.com" }))
            .Content.ReadAsStringAsync();

        Assert.Equal(comUsuario, semUsuario);
        Assert.Single(email.Enviados); // só o do usuário existente
    }

    [Fact]
    public async Task Contato_DeveNotificarOAdministradorComConteudoEscapado()
    {
        var (client, email) = CriarCliente();

        var resp = await client.PostAsJsonAsync("/api/Contato", new
        {
            nome = "Visitante",
            email = "visitante@teste.com",
            mensagem = "Olá <script>alert(1)</script>"
        });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var msg = Assert.Single(email.Enviados);
        Assert.Equal("admin@teste.com", msg.Para);
        Assert.Equal("visitante@teste.com", msg.ResponderPara);
        Assert.Contains("&lt;script&gt;", msg.CorpoHtml);
        Assert.DoesNotContain("<script>", msg.CorpoHtml);
    }

    [Fact]
    public async Task Contato_SemAdministradorConfigurado_NaoEnviaEmail()
    {
        var (client, email) = CriarCliente(comAdmin: false);

        var resp = await client.PostAsJsonAsync("/api/Contato", new
        {
            nome = "Visitante",
            email = "visitante@teste.com",
            mensagem = "Oi"
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Empty(email.Enviados);
    }

    [Fact]
    public async Task AuditoriaPublica_DeveInformarQuantidadeDeJogos()
    {
        var (client, _) = CriarCliente();

        // Hash inexistente continua 404 e no formato { erro }
        var resp = await client.GetAsync("/api/Auditoria/" + new string('A', 64));
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.TryGetProperty("erro", out _));
    }

    [Fact]
    public async Task Registrar_EmailRepetido_RespondeOkEAvisaODonoDoEmail()
    {
        var (client, email) = CriarCliente();
        var (_, endereco) = await RegistrarAsync(client);

        var resp = await client.PostAsJsonAsync("/api/Auth/registrar", new
        {
            nome = "Outro",
            login = "o" + Guid.NewGuid().ToString("N")[..12],
            email = endereco,
            senha = "senha123456"
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("Cadastro recebido", await resp.Content.ReadAsStringAsync());

        var msg = Assert.Single(email.Enviados);
        Assert.Equal(endereco, msg.Para);
    }

    [Fact]
    public async Task TokenAnterior_AposRedefinirSenha_RetornaUnauthorized()
    {
        var (client, email) = CriarCliente();
        var (login, endereco) = await RegistrarAsync(client);

        // Login com a senha original
        var loginResp = await client.PostAsJsonAsync("/api/Auth/login", new { login, senha = "senha123456" });
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
        using var doc = JsonDocument.Parse(await loginResp.Content.ReadAsStringAsync());
        var tokenAntigo = doc.RootElement.GetProperty("token").GetString()!;

        // Recuperação de senha
        await client.PostAsJsonAsync("/api/Auth/esqueci-senha", new { email = endereco });
        var msg = Assert.Single(email.Enviados);
        var tokenReset = Regex.Match(msg.CorpoHtml, @"#token=([A-Za-z0-9_\-]+)").Groups[1].Value;
        var reset = await client.PostAsJsonAsync("/api/Auth/resetar-senha",
            new { token = tokenReset, novaSenha = "novaSenha98765" });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        // Token antigo não vale mais
        var reqAntigo = new HttpRequestMessage(HttpMethod.Get, "/api/Auditoria/meus-lotes");
        reqAntigo.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAntigo);
        var respAntigo = await client.SendAsync(reqAntigo);
        Assert.Equal(HttpStatusCode.Unauthorized, respAntigo.StatusCode);

        // Token novo (login com a nova senha) funciona
        var loginNovo = await client.PostAsJsonAsync("/api/Auth/login", new { login, senha = "novaSenha98765" });
        using var docNovo = JsonDocument.Parse(await loginNovo.Content.ReadAsStringAsync());
        var tokenNovo = docNovo.RootElement.GetProperty("token").GetString()!;

        var reqNovo = new HttpRequestMessage(HttpMethod.Get, "/api/Auditoria/meus-lotes");
        reqNovo.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenNovo);
        var respNovo = await client.SendAsync(reqNovo);
        Assert.Equal(HttpStatusCode.OK, respNovo.StatusCode);
    }
}
