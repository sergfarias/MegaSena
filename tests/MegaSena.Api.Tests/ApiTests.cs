using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace MegaSena.Api.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}

public class ApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_HealthCheck_DeveRetornarOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_Contato_ComDadosValidos_DeveRetornarOk()
    {
        var payload = new { nome = "Teste", email = "teste@teste.com", mensagem = "Mensagem" };
        var response = await _client.PostAsJsonAsync("/api/Contato", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_Registrar_DeveRetornarOk()
    {
        var payload = new
        {
            nome = "Teste",
            login = $"teste{Guid.NewGuid():N}",
            email = $"teste{Guid.NewGuid():N}@teste.com",
            senha = "senha123456"
        };
        var response = await _client.PostAsJsonAsync("/api/Auth/registrar", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_Login_ComCredenciaisInvalidas_DeveRetornarUnauthorized()
    {
        var payload = new { login = "naoexiste", senha = "errada" };
        var response = await _client.PostAsJsonAsync("/api/Auth/login", payload);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // =========================================================
    // CORREÇÃO DO TESTE INÚTIL (Seção 4)
    // Agora ele verifica de verdade o status de cada endpoint.
    // =========================================================
    [Fact]
    public async Task Bater_Em_Todos_Os_Endpoints_Publicos()
    {
        // Dicionário com o endpoint e o status esperado
        var endpoints = new Dictionary<string, HttpStatusCode>
        {
            { "/health", HttpStatusCode.OK },
            { "/api/MegaSena/sortear", HttpStatusCode.OK },
            { "/api/MegaSena/contar", HttpStatusCode.OK },
            { "/api/MegaSena/probabilidade?dezenas=6", HttpStatusCode.OK },
            { "/api/MegaSena/concursos?quantidade=5", HttpStatusCode.OK },
            { "/api/MegaSena/ultimo-sorteio", HttpStatusCode.OK }, // Pode variar se a API cair
        };

        foreach (var ep in endpoints)
        {
            var resp = await _client.GetAsync(ep.Key);

            // Como o ultimo-sorteio depende de uma API externa, aceitamos OK ou BadRequest
            if (ep.Key == "/api/MegaSena/ultimo-sorteio")
                Assert.True(resp.StatusCode == HttpStatusCode.OK || resp.StatusCode == HttpStatusCode.BadRequest);
            else
                Assert.Equal(ep.Value, resp.StatusCode);
        }
    }
}