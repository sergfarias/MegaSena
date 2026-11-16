
using MegaSena.Application.Models;
using MegaSena.Infrastructure.Data;
using MegaSena.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace MegaSena.Infrastructure.Tests.Repositories;

// Substitui o antigo HistoricoApiRepositoryTests (a classe HistoricoApiRepository foi removida).
public class HistoricoDbRepositoryTests
{
    private static AppDbContext CriarDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    // Criar o repositório com o Cache vazio exigido
    private static HistoricoDbRepository CriarRepo(AppDbContext db) =>
        new(db, new MemoryCache(new MemoryCacheOptions()));

    private static ConcursoDto Concurso(int numero, string dezenas) => new()
    {
        NumeroConcurso = numero,
        DataHoraSorteio = DateTime.UtcNow,
        DezenasSorteadas = dezenas
    };

    [Fact]
    public async Task ObterUltimosSorteios_ComConcurso_DeveRetornarLista()
    {
        using var db = CriarDb();
        await new ConcursoRepository(db).AdicionarAsync(Concurso(2700, "01,05,12,33,44,55"));

        var resultado = await CriarRepo(db).ObterUltimosSorteiosAsync(1);

        Assert.NotNull(resultado);
        Assert.Single(resultado);
        Assert.Equal(6, resultado[0].Length);
        Assert.Equal(new[] { 1, 5, 12, 33, 44, 55 }, resultado[0]);
    }

    [Fact]
    public async Task ObterUltimosSorteios_DeveRetornarMaisRecentesPrimeiro()
    {
        using var db = CriarDb();
        await new ConcursoRepository(db).AdicionarVariosAsync(new[]
        {
            Concurso(100, "01,02,03,04,05,06"),
            Concurso(101, "10,20,30,40,50,60"),
            Concurso(102, "07,08,09,11,12,13")
        });

        var resultado = await CriarRepo(db).ObterUltimosSorteiosAsync(2);

        Assert.Equal(2, resultado.Count);
        Assert.Equal(new[] { 7, 8, 9, 11, 12, 13 }, resultado[0]);   // concurso 102
        Assert.Equal(new[] { 10, 20, 30, 40, 50, 60 }, resultado[1]); // concurso 101
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ObterUltimosSorteios_QuantidadeInvalida_DeveRetornarVazio(int quantidade)
    {
        using var db = CriarDb();
        Assert.Empty(await CriarRepo(db).ObterUltimosSorteiosAsync(quantidade));
    }

    [Fact]
    public async Task ObterUltimosSorteios_BancoVazio_DeveRetornarVazio()
    {
        using var db = CriarDb();
        Assert.Empty(await CriarRepo(db).ObterUltimosSorteiosAsync(3));
    }

    [Fact]
    public async Task ObterMenosSorteadas_DeveRetornarDezenasDeMenorFrequencia()
    {
        using var db = CriarDb();
        // 1..6 saíram uma vez; 7..60 nunca saíram. OrderBy é estável, então começa em 7.
        await new ConcursoRepository(db).AdicionarAsync(Concurso(1, "01,02,03,04,05,06"));

        var resultado = await CriarRepo(db).ObterMenosSorteadasAsync(3);

        Assert.Equal(new[] { 7, 8, 9 }, resultado);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ObterMenosSorteadas_QuantidadeInvalida_DeveRetornarVazio(int quantidade)
    {
        using var db = CriarDb();
        Assert.Empty(await CriarRepo(db).ObterMenosSorteadasAsync(quantidade));
    }

    [Fact]
    public async Task ObterMenosSorteadas_UsaCache_ETemQueSerInvalidadoPelaChaveDoImportador()
    {
        using var db = CriarDb();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var repo = new HistoricoDbRepository(db, cache);
        var concursos = new ConcursoRepository(db);

        await concursos.AdicionarAsync(Concurso(1, "01,02,03,04,05,06"));
        Assert.Equal(new[] { 7, 8, 9 }, await repo.ObterMenosSorteadasAsync(3));

        // Entra um concurso novo, mas o cache de 1 hora ainda vale: resultado antigo
        await concursos.AdicionarAsync(Concurso(2, "07,08,09,10,11,12"));
        Assert.Equal(new[] { 7, 8, 9 }, await repo.ObterMenosSorteadasAsync(3));

        // O importador remove a chave "freq-dezenas": a próxima consulta recalcula
        cache.Remove("freq-dezenas");
        Assert.Equal(new[] { 13, 14, 15 }, await repo.ObterMenosSorteadasAsync(3));
    }
}
