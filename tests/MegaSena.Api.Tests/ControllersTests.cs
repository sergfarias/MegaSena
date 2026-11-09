using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace MegaSena.Api.Tests;

// Usa a mesma CustomWebApplicationFactory para rodar no ambiente "Testing" (InMemory)
public class ControllersTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ControllersTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Sortear_DeveRetornarOk()
    {
        var response = await _client.GetAsync("/api/MegaSena/sortear");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("jogos", body.ToLower());
    }

    [Fact]
    public async Task Contar_DeveRetornarOk()
    {
        var response = await _client.GetAsync("/api/MegaSena/contar");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Uniformidade_DeveRetornarOk()
    {
        var response = await _client.GetAsync("/api/MegaSena/uniformidade?n=100");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Probabilidade_DeveRetornarOk()
    {
        var response = await _client.GetAsync("/api/MegaSena/probabilidade?dezenas=6");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GerarLote_ComParametrosValidos_DeveRetornarOk()
    {
        var payload = new { quantidade = 10, ultimos = 3, menos = 4, maxPares = 1 };
        var response = await _client.PostAsJsonAsync("/api/MegaSena/gerar-lote", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuditarLote_EndpointRemovido_DeveRetornarNotFound()
    {
        // A auditoria agora é feita pelo hash gravado em /api/Cobertura/gerar
        var response = await _client.PostAsJsonAsync("/api/MegaSena/auditar-lote", "[]");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Login_ComCredenciaisInvalidas_DeveRetornarUnauthorized()
    {
        var payload = new { login = "naoexiste", senha = "errada" };
        var response = await _client.PostAsJsonAsync("/api/Auth/login", payload);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EnviarContato_DeveRetornarOk()
    {
        var payload = new
        {
            nome = "Teste Contato",
            email = "contato@teste.com",
            mensagem = "Mensagem de teste"
        };
        var response = await _client.PostAsJsonAsync("/api/Contato", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthCheck_DeveRetornarHealthy()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", body);
    }

    [Fact]
    public async Task Concursos_DeveRetornarOk()
    {
        var response = await _client.GetAsync("/api/MegaSena/concursos?quantidade=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

