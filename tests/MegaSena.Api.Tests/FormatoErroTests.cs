using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace MegaSena.Api.Tests;

public class FormatoErroTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public FormatoErroTests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    private static async Task<JsonElement> LerJsonAsync(HttpResponseMessage resp)
    {
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    [Fact]
    public async Task Registrar_SenhaCurta_RetornaErroNoFormatoPadrao()
    {
        var resp = await _client.PostAsJsonAsync("/api/Auth/registrar", new
        {
            nome = "Teste",
            login = "usuario_formato",
            email = "formato@teste.com",
            senha = "123"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);

        var json = await LerJsonAsync(resp);
        Assert.Contains("8 caracteres", json.GetProperty("erro").GetString());
        Assert.True(json.GetProperty("detalhes").GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Registrar_CorpoVazio_RetornaErroNoFormatoPadrao()
    {
        var resp = await _client.PostAsJsonAsync("/api/Auth/registrar", new { });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);

        var json = await LerJsonAsync(resp);
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("erro").GetString()));
    }

    [Fact]
    public async Task Registrar_EmailInvalido_RetornaMensagemEmPortugues()
    {
        var resp = await _client.PostAsJsonAsync("/api/Auth/registrar", new
        {
            nome = "Teste",
            login = "usuario_email",
            email = "isso-nao-e-email",
            senha = "senha12345"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);

        var json = await LerJsonAsync(resp);
        Assert.Equal("E-mail inválido.", json.GetProperty("erro").GetString());
    }
}
