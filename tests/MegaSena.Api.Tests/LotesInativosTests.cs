using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MegaSena.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MegaSena.Api.Tests;

/// <summary>N17: lote inativo não aparece na lista do usuário e o detalhe devolve 404 (mesma regra nos dois).</summary>
public class LotesInativosTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LotesInativosTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GerarLoteAsync()
    {
        var resp = await _client.PostAsJsonAsync("/api/Cobertura/gerar", new
        {
            dezenas = new[] { 2, 5, 9, 14, 20, 31, 44 },
            garantia = 6,
            universoSorteado = 6,
            maxPares = 1
        });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("hashAuditoria").GetString()!;
    }

    private async Task<List<string>> HashesDaListaAsync()
    {
        var resp = await _client.GetAsync("/api/Auditoria/meus-lotes");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.EnumerateArray()
            .Select(e => e.GetProperty("hash").GetString()!)
            .ToList();
    }

    [Fact]
    public async Task LoteInativo_SomeDaListaEODetalheRetorna404()
    {
        var (token, _) = await AuthHelper.RegistrarELogarAsync(_client);
        AuthHelper.Autenticar(_client, token);

        var hash = await GerarLoteAsync();

        // Ativo: aparece na lista e abre
        Assert.Contains(await HashesDaListaAsync(), h => string.Equals(h, hash, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/Auditoria/meus-lotes/{hash}")).StatusCode);

        // Desativa direto no banco InMemory
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var lote = db.Jogos.ToList().First(j => string.Equals(j.Hash, hash, StringComparison.OrdinalIgnoreCase));
            lote.Ativo = false;
            await db.SaveChangesAsync();
        }

        // Inativo: some da lista e o detalhe não abre
        Assert.DoesNotContain(await HashesDaListaAsync(), h => string.Equals(h, hash, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/Auditoria/meus-lotes/{hash}")).StatusCode);
    }
}
