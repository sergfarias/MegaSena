using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace MegaSena.Api.Tests;

/// <summary>Testes de contrato: o que o front espera receber da API.</summary>
public class ContratosTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ContratosTests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    private static async Task<JsonElement> LerJsonAsync(HttpResponseMessage resp)
    {
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    private static long C(int n, int k)
    {
        if (k < 0 || k > n) return 0;
        long r = 1;
        for (int i = 1; i <= k; i++) r = r * (n - k + i) / i;
        return r;
    }

    [Fact]
    public async Task Contar_DeveRetornarDetalhesComTamanhoReal()
    {
        var resp = await _client.GetAsync("/api/MegaSena/contar");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var json = await LerJsonAsync(resp);
        var detalhes = json.GetProperty("detalhes");

        Assert.Equal(60, detalhes.GetProperty("completo").GetProperty("tamanho").GetInt32());

        foreach (var nome in new[] { "completo", "semUltimos", "semUltimosEMenos" })
        {
            var d = detalhes.GetProperty(nome);
            int tamanho = d.GetProperty("tamanho").GetInt32();
            Assert.Equal(C(tamanho, 6), d.GetProperty("totalCombinacoes").GetInt64());

            double aproveitamento = d.GetProperty("aproveitamento").GetDouble();
            Assert.InRange(aproveitamento, 0.0, 1.0);
        }

        // Campos antigos continuam existindo (compatibilidade)
        Assert.Equal(
            detalhes.GetProperty("completo").GetProperty("jogosValidos").GetInt64(),
            json.GetProperty("universo60").GetInt64());
    }

    [Theory]
    [InlineData("?dezenas=12", 12)]
    [InlineData("?dezenasMultipla=10", 10)]
    [InlineData("", 8)]
    public async Task CompararEstrategias_DeveUsarOParametroInformado(string query, int esperado)
    {
        var resp = await _client.GetAsync("/api/MegaSena/comparar-estrategias" + query);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var json = await LerJsonAsync(resp);
        Assert.Equal(esperado, json.GetProperty("dezenasMultipla").GetInt32());
    }

    [Fact]
    public async Task Sortear_ComFiltros_DeveAvisarQueNaoAumentaAChance()
    {
        var comFiltro = await LerJsonAsync(await _client.GetAsync("/api/MegaSena/sortear?ultimos=3&menos=4"));
        Assert.Equal(JsonValueKind.String, comFiltro.GetProperty("avisoFiltros").ValueKind);

        var semFiltro = await LerJsonAsync(await _client.GetAsync("/api/MegaSena/sortear?ultimos=0&menos=0"));
        Assert.Equal(JsonValueKind.Null, semFiltro.GetProperty("avisoFiltros").ValueKind);
    }

    [Fact]
    public async Task Sortear_Fisico_DeveTrazerAvisoDeDemonstracaoVisual()
    {
        var json = await LerJsonAsync(await _client.GetAsync("/api/MegaSena/sortear?fisico=true&ultimos=0&menos=0"));
        Assert.Equal(JsonValueKind.String, json.GetProperty("avisoMotor").ValueKind);
    }

    [Fact]
    public async Task EndpointProtegido_SemToken_RetornaUnauthorizedEmJson()
    {
        var resp = await _client.GetAsync("/api/Auditoria/meus-lotes");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        Assert.Equal("application/json", resp.Content.Headers.ContentType?.MediaType);

        var json = await LerJsonAsync(resp);
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("erro").GetString()));
    }
}


