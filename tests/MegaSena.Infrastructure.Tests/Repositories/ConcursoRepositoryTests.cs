using MegaSena.Application.Models;
using MegaSena.Infrastructure.Data;
using MegaSena.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MegaSena.Infrastructure.Tests.Repositories;

public class ConcursoRepositoryTests
{
    private static AppDbContext CriarDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Adicionar_E_Obter_DeveFuncionar()
    {
        using var db = CriarDb();
        var repo = new ConcursoRepository(db);

        await repo.AdicionarAsync(new ConcursoDto
        {
            NumeroConcurso = 2700,
            DataHoraSorteio = DateTime.UtcNow,
            DezenasSorteadas = "01,05,12,33,44,55"
        });

        var recuperado = await repo.ObterPorNumeroAsync(2700);
        Assert.NotNull(recuperado);
        Assert.Equal("01,05,12,33,44,55", recuperado!.DezenasSorteadas);
    }

    [Fact]
    public async Task ObterUltimoNumero_DeveRetornarMaiorNumero()
    {
        using var db = CriarDb();
        var repo = new ConcursoRepository(db);

        await repo.AdicionarAsync(new ConcursoDto { NumeroConcurso = 2698, DezenasSorteadas = "01,02,03,04,05,06" });
        await repo.AdicionarAsync(new ConcursoDto { NumeroConcurso = 2700, DezenasSorteadas = "01,02,03,04,05,06" });
        await repo.AdicionarAsync(new ConcursoDto { NumeroConcurso = 2699, DezenasSorteadas = "01,02,03,04,05,06" });

        var ultimo = await repo.ObterUltimoNumeroAsync();
        Assert.Equal(2700, ultimo);
    }

    [Fact]
    public async Task Contar_DeveRetornarTotalDeConcursos()
    {
        using var db = CriarDb();
        var repo = new ConcursoRepository(db);

        await repo.AdicionarAsync(new ConcursoDto { NumeroConcurso = 1, DezenasSorteadas = "01,02,03,04,05,06" });
        await repo.AdicionarAsync(new ConcursoDto { NumeroConcurso = 2, DezenasSorteadas = "01,02,03,04,05,07" });

        Assert.Equal(2, await repo.ContarAsync());
    }
}