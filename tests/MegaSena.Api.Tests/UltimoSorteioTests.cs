using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MegaSena.Application.Models;
using MegaSena.Application.Portas;
using MegaSena.Application.Servicos;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MegaSena.Api.Tests;

/// <summary>
/// Semeia um concurso no banco de teste: o /ultimo-sorteio responde pelo banco e nunca chega à API externa.
/// Depois deste teste, remova "/api/MegaSena/ultimo-sorteio" do dicionário de ApiTests.Bater_Em_Todos_Os_Endpoints_Publicos.
/// </summary>
public class UltimoSorteioTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public UltimoSorteioTests(CustomWebApplicationFactory factory) => _factory = factory;

    private sealed class HandlerQueFalha : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            throw new InvalidOperationException("Não deveria chamar a API externa quando há concurso no banco.");
    }

    [Fact]
    public async Task UltimoSorteio_ComConcursoNoBanco_ResponderPeloBancoSemRede()
    {
        var factory = _factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
            s.AddHttpClient<ResultadoService>().ConfigurePrimaryHttpMessageHandler(() => new HandlerQueFalha())));

        using (var scope = factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IConcursoRepository>();
            await repo.AdicionarAsync(new ConcursoDto
            {
                NumeroConcurso = 9999,
                DataHoraSorteio = new DateTime(2026, 11, 5),
                DezenasSorteadas = "01,05,12,33,44,55"
            });
        }

        var client = factory.CreateClient();
        var resp = await client.GetAsync("/api/MegaSena/ultimo-sorteio");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(9999, doc.RootElement.GetProperty("concurso").GetInt32());
        Assert.Equal(6, doc.RootElement.GetProperty("dezenas").GetArrayLength());
    }
}

