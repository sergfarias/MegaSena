using System.Net;
using MegaSena.Api.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace MegaSena.Api.Tests;

/// <summary>
/// T4: rate limiting real. Sobe um host mínimo FORA do ambiente Testing (que desliga os limiters)
/// usando AddRateLimitingDaApi, e simula clientes diferentes por X-Forwarded-For.
/// Cada teste cria o próprio host, então os contadores não se misturam.
/// Requer o Patch 5 (A4): ApiServiceCollectionExtensions.AddRateLimitingDaApi.
/// </summary>
internal static class HostDeRateLimit
{
    public const string ClienteA = "203.0.113.10";
    public const string ClienteB = "203.0.113.11";

    private static readonly string[] Politicas =
        { "fechamento", "contato", "auth", "senha", "uniformidade", "sortear-fisico" };

    /// <param name="enderecoDoProxy">IP que a aplicação enxerga como origem da conexão (o proxy). Padrão: loopback.</param>
    /// <param name="proxiesConhecidos">IPs adicionados a ForwardedHeaders:KnownProxies (como no Program.cs).</param>
    public static async Task<WebApplication> CriarAsync(
        IPAddress? enderecoDoProxy = null,
        IEnumerable<IPAddress>? proxiesConhecidos = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseTestServer();
        builder.Services.AddRateLimitingDaApi(builder.Environment);

        var app = builder.Build();

        // O TestServer não define RemoteIpAddress; aqui simulamos o proxy da hospedagem.
        var origem = enderecoDoProxy ?? IPAddress.Loopback;
        app.Use((ctx, next) =>
        {
            ctx.Connection.RemoteIpAddress = origem;
            return next();
        });

        var forwarded = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor,
            ForwardLimit = 1
        };
        foreach (var ip in proxiesConhecidos ?? Array.Empty<IPAddress>())
            forwarded.KnownProxies.Add(ip);
        app.UseForwardedHeaders(forwarded);

        app.UseRateLimiter();

        app.MapGet("/health", () => "ok");
        foreach (var nome in Politicas)
            app.MapGet("/" + nome, () => "ok").RequireRateLimiting(nome);

        await app.StartAsync();
        return app;
    }

    public static Task<HttpResponseMessage> GetAsync(HttpClient client, string url, string ipDoCliente)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("X-Forwarded-For", ipDoCliente);
        return client.SendAsync(req);
    }
}

public class RateLimitingRealTests
{
    [Theory]
    [InlineData("auth", 5)]
    [InlineData("senha", 3)]
    [InlineData("contato", 3)]
    [InlineData("uniformidade", 10)]
    [InlineData("fechamento", 5)]
    public async Task PoliticaNomeada_BloqueiaAposOLimite_ComRetryAfter_ESoParaAqueleIp(string politica, int limite)
    {
        await using var app = await HostDeRateLimit.CriarAsync();
        var client = app.GetTestClient();

        for (int i = 0; i < limite; i++)
        {
            var ok = await HostDeRateLimit.GetAsync(client, "/" + politica, HostDeRateLimit.ClienteA);
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        }

        var bloqueada = await HostDeRateLimit.GetAsync(client, "/" + politica, HostDeRateLimit.ClienteA);
        Assert.Equal(HttpStatusCode.TooManyRequests, bloqueada.StatusCode);
        Assert.NotNull(bloqueada.Headers.RetryAfter);

        // Outro cliente (outro X-Forwarded-For) não é afetado
        var outro = await HostDeRateLimit.GetAsync(client, "/" + politica, HostDeRateLimit.ClienteB);
        Assert.Equal(HttpStatusCode.OK, outro.StatusCode);
    }

    [Fact]
    public async Task LimiteGlobal_60PorMinutoPorIp()
    {
        await using var app = await HostDeRateLimit.CriarAsync();
        var client = app.GetTestClient();

        for (int i = 0; i < 60; i++)
        {
            var ok = await HostDeRateLimit.GetAsync(client, "/health", HostDeRateLimit.ClienteA);
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        }

        var bloqueada = await HostDeRateLimit.GetAsync(client, "/health", HostDeRateLimit.ClienteA);
        Assert.Equal(HttpStatusCode.TooManyRequests, bloqueada.StatusCode);
        Assert.NotNull(bloqueada.Headers.RetryAfter);

        var outro = await HostDeRateLimit.GetAsync(client, "/health", HostDeRateLimit.ClienteB);
        Assert.Equal(HttpStatusCode.OK, outro.StatusCode);
    }

    [Fact]
    public async Task SorteioRapido_NaoSofreOLimiteDaPolicyFisica()
    {
        await using var app = await HostDeRateLimit.CriarAsync();
        var client = app.GetTestClient();

        // 15 > 10 (limite do físico) e < 60 (limite global): todas devem passar
        for (int i = 0; i < 15; i++)
        {
            var resp = await HostDeRateLimit.GetAsync(client, "/sortear-fisico", HostDeRateLimit.ClienteA);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }

        var explicitoFalso = await HostDeRateLimit.GetAsync(client, "/sortear-fisico?fisico=false", HostDeRateLimit.ClienteA);
        Assert.Equal(HttpStatusCode.OK, explicitoFalso.StatusCode);
    }

    [Fact]
    public async Task SorteioFisico_LimitadoA10PorMinuto()
    {
        await using var app = await HostDeRateLimit.CriarAsync();
        var client = app.GetTestClient();

        for (int i = 0; i < 10; i++)
        {
            var ok = await HostDeRateLimit.GetAsync(client, "/sortear-fisico?fisico=true", HostDeRateLimit.ClienteA);
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        }

        var bloqueada = await HostDeRateLimit.GetAsync(client, "/sortear-fisico?fisico=true", HostDeRateLimit.ClienteA);
        Assert.Equal(HttpStatusCode.TooManyRequests, bloqueada.StatusCode);
        Assert.NotNull(bloqueada.Headers.RetryAfter);

        var outro = await HostDeRateLimit.GetAsync(client, "/sortear-fisico?fisico=true", HostDeRateLimit.ClienteB);
        Assert.Equal(HttpStatusCode.OK, outro.StatusCode);
    }

    // ----- L7: ForwardedHeaders -------------------------------------------------

    [Fact]
    public async Task ProxyNaoConfiavel_XForwardedForEhIgnorado_ClientesCompartilhamOBucket()
    {
        // Proxy em 10.0.0.1 e KnownProxies vazio: é exatamente o cenário de risco do L7.
        await using var app = await HostDeRateLimit.CriarAsync(enderecoDoProxy: IPAddress.Parse("10.0.0.1"));
        var client = app.GetTestClient();

        for (int i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.OK,
                (await HostDeRateLimit.GetAsync(client, "/auth", HostDeRateLimit.ClienteA)).StatusCode);

        // O cliente B é outra pessoa, mas cai no mesmo bucket do proxy
        var b = await HostDeRateLimit.GetAsync(client, "/auth", HostDeRateLimit.ClienteB);
        Assert.Equal(HttpStatusCode.TooManyRequests, b.StatusCode);
    }

    [Fact]
    public async Task ProxyConfiguradoEmKnownProxies_ClientesTemBucketsSeparados()
    {
        var proxy = IPAddress.Parse("10.0.0.1");
        await using var app = await HostDeRateLimit.CriarAsync(
            enderecoDoProxy: proxy, proxiesConhecidos: new[] { proxy });
        var client = app.GetTestClient();

        for (int i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.OK,
                (await HostDeRateLimit.GetAsync(client, "/auth", HostDeRateLimit.ClienteA)).StatusCode);

        Assert.Equal(HttpStatusCode.TooManyRequests,
            (await HostDeRateLimit.GetAsync(client, "/auth", HostDeRateLimit.ClienteA)).StatusCode);

        Assert.Equal(HttpStatusCode.OK,
            (await HostDeRateLimit.GetAsync(client, "/auth", HostDeRateLimit.ClienteB)).StatusCode);
    }
}
