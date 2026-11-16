using System.Net;
using MegaSena.Application.Portas;
using MegaSena.Infrastructure.Data;
using MegaSena.Infrastructure.Repositories;
using MegaSena.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MegaSena.Infrastructure.Tests;

/// <summary>
/// T9: importador. Requer o refactor descrito em editar_existentes/ImportadorConcursosService_refactor.md
/// (MapearConcurso público e estático, ImportarConcursosAsync interno, InternalsVisibleTo).
/// </summary>
public class ImportadorConcursosServiceTests
{
    private const string JsonCompleto = """
    {
      "numero": 2700,
      "dataApuracao": "05/11/2026",
      "dataProximoConcurso": "08/11/2026",
      "nomeMunicipioSorteio": "SAO PAULO",
      "ufSorteio": "SP",
      "listaDezenas": ["01","05","12","33","44","55"],
      "valorArrecadado": 1000000.50,
      "acumulado": true,
      "listaRateioPremio": [
        { "descricaoFaixa": "6 acertos", "numeroDeGanhadores": 0, "valorPremio": 0 },
        { "descricaoFaixa": "5 acertos", "numeroDeGanhadores": 30, "valorPremio": 50000.25 },
        { "descricaoFaixa": "4 acertos", "numeroDeGanhadores": 2000, "valorPremio": 900.10 }
      ]
    }
    """;

    // ----- Mapeamento do JSON -------------------------------------------------

    [Fact]
    public void MapearConcurso_JsonCompleto_PreencheCamposEFaixas()
    {
        var c = ImportadorConcursosService.MapearConcurso(JsonCompleto);

        Assert.Equal(2700, c.NumeroConcurso);
        Assert.Equal("01,05,12,33,44,55", c.DezenasSorteadas);
        Assert.Equal(new DateTime(2026, 11, 5), c.DataHoraSorteio);
        Assert.Equal(new DateTime(2026, 11, 8), c.DataProximoConcurso);
        Assert.Equal("SAO PAULO", c.CidadeSorteio);
        Assert.True(c.Acumulado);
        Assert.Equal(1000000.50m, c.ValorArrecadado);

        Assert.Equal(0, c.QtdGanhadoresSena);
        Assert.Equal(30, c.QtdGanhadoresQuina);
        Assert.Equal(50000.25m, c.ValorPremioQuina);
        Assert.Equal(2000, c.QtdGanhadoresQuadra);
        Assert.Equal(900.10m, c.ValorPremioQuadra);
    }

    [Fact]
    public void MapearConcurso_CamposOpcionaisNulos_NaoQuebra()
    {
        const string json = """
        {
          "numero": 1,
          "dataApuracao": "11/03/1996",
          "dataProximoConcurso": null,
          "nomeMunicipioSorteio": null,
          "ufSorteio": null,
          "listaDezenas": ["04","05","30","33","41","52"],
          "valorArrecadado": null,
          "acumulado": false
        }
        """;

        var c = ImportadorConcursosService.MapearConcurso(json);

        Assert.Equal(1, c.NumeroConcurso);
        Assert.Null(c.DataProximoConcurso);
        Assert.Null(c.CidadeSorteio);
        Assert.Null(c.UfSorteio);
        Assert.Null(c.ValorArrecadado);
        Assert.False(c.Acumulado);
        Assert.Equal(0, c.QtdGanhadoresSena);
    }

    [Fact]
    public void MapearConcurso_SemDezenas_LancaExcecao()
    {
        const string json = """{ "numero": 5, "dataApuracao": "01/01/2020" }""";
        Assert.ThrowsAny<Exception>(() => ImportadorConcursosService.MapearConcurso(json));
    }

    [Fact]
    public void MapearConcurso_DataInvalida_LancaExcecao()
    {
        const string json = """{ "numero": 5, "dataApuracao": "2020-01-01", "listaDezenas": ["01","02","03","04","05","06"] }""";
        Assert.ThrowsAny<Exception>(() => ImportadorConcursosService.MapearConcurso(json));
    }

    // ----- Rodada de importação ----------------------------------------------

    private sealed class HandlerFake : HttpMessageHandler
    {
        private readonly Func<string, HttpResponseMessage> _responder;
        public int Chamadas;

        public HandlerFake(Func<string, HttpResponseMessage> responder) => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Interlocked.Increment(ref Chamadas);
            return Task.FromResult(_responder(request.RequestUri!.AbsolutePath));
        }
    }

    private static ServiceProvider CriarProvider(HandlerFake handler, string dbName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddScoped<IConcursoRepository, ConcursoRepository>();
        services.AddHttpClient("ApiCaixa").ConfigurePrimaryHttpMessageHandler(() => handler);
        return services.BuildServiceProvider();
    }

    private static ImportadorConcursosService CriarServico(ServiceProvider sp, IMemoryCache cache) =>
        new(sp.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ImportadorConcursosService>.Instance,
            cache);

    [Fact]
    public async Task Importar_ComDezFalhasSeguidas_InterrompeARodadaELancaExcecao()
    {
        // /ultimo diz que existem 30 concursos; todos os demais respondem 500
        var handler = new HandlerFake(path => path.EndsWith("/ultimo")
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{ "numero": 30 }""") }
            : new HttpResponseMessage(HttpStatusCode.InternalServerError));

        using var sp = CriarProvider(handler, Guid.NewGuid().ToString());
        var servico = CriarServico(sp, new MemoryCache(new MemoryCacheOptions()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => servico.ImportarConcursosAsync(CancellationToken.None));

        Assert.Equal(1 + 10, handler.Chamadas); // 1 do /ultimo + 10 tentativas, e para
    }

    [Fact]
    public async Task Importar_ComTodosOsConcursos_SalvaNoBancoEInvalidaOCache()
    {
        var handler = new HandlerFake(path =>
        {
            if (path.EndsWith("/ultimo"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{ "numero": 3 }""") };

            var numero = int.Parse(path[(path.LastIndexOf('/') + 1)..]);
            var json = JsonCompleto.Replace("\"numero\": 2700", $"\"numero\": {numero}");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
        });

        var dbName = Guid.NewGuid().ToString();
        using var sp = CriarProvider(handler, dbName);
        var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set("freq-dezenas", new int[61]);
        var servico = CriarServico(sp, cache);

        await servico.ImportarConcursosAsync(CancellationToken.None);

        using var scope = sp.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IConcursoRepository>();
        var numeros = (await repo.ObterNumerosAsync()).OrderBy(n => n).ToList();

        Assert.Equal(new[] { 1, 2, 3 }, numeros);
        Assert.False(cache.TryGetValue("freq-dezenas", out _));
    }

    [Fact]
    public async Task Importar_ApiExternaIndisponivel_NaoLancaENaoSalvaNada()
    {
        var handler = new HandlerFake(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        using var sp = CriarProvider(handler, Guid.NewGuid().ToString());
        var servico = CriarServico(sp, new MemoryCache(new MemoryCacheOptions()));

        await servico.ImportarConcursosAsync(CancellationToken.None); // só registra aviso

        Assert.Equal(1, handler.Chamadas);
    }
}
