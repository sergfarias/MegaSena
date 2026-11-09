using MegaSena.Application.Models;
using MegaSena.Infrastructure.Data;
using MegaSena.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
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

        var resultado = new HistoricoDbRepository(db).ObterUltimosSorteios(1);

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

        var resultado = new HistoricoDbRepository(db).ObterUltimosSorteios(2);

        Assert.Equal(2, resultado.Count);
        Assert.Equal(new[] { 7, 8, 9, 11, 12, 13 }, resultado[0]);   // concurso 102
        Assert.Equal(new[] { 10, 20, 30, 40, 50, 60 }, resultado[1]); // concurso 101
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ObterUltimosSorteios_QuantidadeInvalida_DeveRetornarVazio(int quantidade)
    {
        using var db = CriarDb();
        Assert.Empty(new HistoricoDbRepository(db).ObterUltimosSorteios(quantidade));
    }

    [Fact]
    public void ObterUltimosSorteios_BancoVazio_DeveRetornarVazio()
    {
        using var db = CriarDb();
        Assert.Empty(new HistoricoDbRepository(db).ObterUltimosSorteios(3));
    }

    [Fact]
    public async Task ObterMenosSorteadas_DeveRetornarDezenasDeMenorFrequencia()
    {
        using var db = CriarDb();
        // 1..6 saíram uma vez; 7..60 nunca saíram. OrderBy é estável, então começa em 7.
        await new ConcursoRepository(db).AdicionarAsync(Concurso(1, "01,02,03,04,05,06"));

        var resultado = new HistoricoDbRepository(db).ObterMenosSorteadas(3);

        Assert.Equal(new[] { 7, 8, 9 }, resultado);
    }
}