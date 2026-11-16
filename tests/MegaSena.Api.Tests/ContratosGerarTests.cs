using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace MegaSena.Api.Tests;

/// <summary>Testes de contrato: o que o front espera receber da API.</summary>

public class ContratoGerarTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ContratoGerarTests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Gerar_DeveRetornarCriadoEmUtcEChanceDaGarantia()
    {
        var (token, _) = await AuthHelper.RegistrarELogarAsync(_client);
        AuthHelper.Autenticar(_client, token);

        var resp = await _client.PostAsJsonAsync("/api/Cobertura/gerar", new
        {
            dezenas = new[] { 2, 5, 9, 14, 20, 31, 44 },
            garantia = 6,
            universoSorteado = 6,
            maxPares = 1
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var json = doc.RootElement;

        Assert.True(json.GetProperty("minimoProvado").GetBoolean());

        // 7 dezenas escolhidas, 6 sorteadas todas entre elas: 7 / C(60,6)
        double chance = json.GetProperty("chanceGarantiaValer").GetDouble();
        Assert.InRange(chance, 1e-8, 1e-6);

        var criadoEm = json.GetProperty("criadoEmUtc").GetDateTime().ToUniversalTime();
        Assert.True(Math.Abs((DateTime.UtcNow - criadoEm).TotalMinutes) < 5);
    }

    [Fact]
    public async Task MeuLoteDetalhado_AposGerar_DeveConfirmarIntegridade()
    {
        var (token, _) = await AuthHelper.RegistrarELogarAsync(_client);
        AuthHelper.Autenticar(_client, token);

        var gerar = await _client.PostAsJsonAsync("/api/Cobertura/gerar", new
        {
            dezenas = new[] { 2, 5, 9, 14, 20, 31, 44 },
            garantia = 6,
            universoSorteado = 6,
            maxPares = 1
        });
        Assert.Equal(HttpStatusCode.OK, gerar.StatusCode);

        using var docGerar = JsonDocument.Parse(await gerar.Content.ReadAsStringAsync());
        var hash = docGerar.RootElement.GetProperty("hashAuditoria").GetString();

        var resp = await _client.GetAsync($"/api/Auditoria/meus-lotes/{hash}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        using var docLote = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.True(docLote.RootElement.GetProperty("integridadeConfirmada").GetBoolean());
    }
}
