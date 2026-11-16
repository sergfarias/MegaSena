using MegaSena.Api.Services;
using MegaSena.Application.Models;
using MegaSena.Application.Portas;
using MegaSena.Domain.Calculadora;
using MegaSena.Infrastructure.Data;
using MegaSena.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MegaSena.Api.Tests;

/// <summary>T9: taxa de retorno histórica (cálculo, provider e serviço em segundo plano).</summary>
public class RetornoHistoricoServiceTests
{
    [Fact]
    public void Taxa_DivideTotalPagoPeloTotalArrecadado_IgnorandoConcursosSemArrecadacao()
    {
        var taxa = RetornoHistorico.Taxa(new[]
        {
            new ConcursoFinanceiro(1000m, 400m),
            new ConcursoFinanceiro(1000m, 600m),
            new ConcursoFinanceiro(0m, 999m)
        });

        Assert.Equal(0.5m, taxa);
    }

    [Fact]
    public void Taxa_SemConcursos_RetornaZero()
    {
        Assert.Equal(0m, RetornoHistorico.Taxa(Array.Empty<ConcursoFinanceiro>()));
    }

    [Fact]
    public void Provider_AtualizarTrocaOValorAtual()
    {
        var provider = new RetornoHistoricoProvider();
        Assert.Null(provider.Atual);

        provider.Atualizar(new TaxaRetorno(0.43m, 200, DateTime.UtcNow));

        Assert.Equal(0.43m, provider.Atual!.Taxa);
        Assert.Equal(200, provider.Atual.Concursos);
    }

    private static ServiceProvider CriarProvider(string dbName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddScoped<IConcursoRepository, ConcursoRepository>();
        return services.BuildServiceProvider();
    }

    private static async Task SemearAsync(ServiceProvider sp, int quantidade)
    {
        using var scope = sp.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IConcursoRepository>();
        for (int n = 1; n <= quantidade; n++)
        {
            await repo.AdicionarAsync(new ConcursoDto
            {
                NumeroConcurso = n,
                DataHoraSorteio = DateTime.UtcNow,
                DezenasSorteadas = "01,02,03,04,05,06",
                ValorArrecadado = 1000m,
                ValorPremioQuadra = 10m,
                QtdGanhadoresQuadra = 40      // paga 400 de 1000 em cada concurso: taxa 0,40
            });
        }
    }

    [Fact]
    public async Task Servico_ComConcursosSuficientes_AtualizaOProvider()
    {
        using var sp = CriarProvider(Guid.NewGuid().ToString());
        await SemearAsync(sp, 60);

        var provider = new RetornoHistoricoProvider();
        var servico = new RetornoHistoricoService(
            sp.GetRequiredService<IServiceScopeFactory>(), provider,
            NullLogger<RetornoHistoricoService>.Instance);

        await servico.StartAsync(CancellationToken.None);

        for (int i = 0; i < 50 && provider.Atual is null; i++)
            await Task.Delay(100);

        await servico.StopAsync(CancellationToken.None);

        Assert.NotNull(provider.Atual);
        Assert.Equal(0.4m, provider.Atual!.Taxa);
        Assert.Equal(60, provider.Atual.Concursos);
    }

    [Fact]
    public async Task Servico_ComMenosDe50Concursos_NaoAtualizaOProvider()
    {
        using var sp = CriarProvider(Guid.NewGuid().ToString());
        await SemearAsync(sp, 10);

        var provider = new RetornoHistoricoProvider();
        var servico = new RetornoHistoricoService(
            sp.GetRequiredService<IServiceScopeFactory>(), provider,
            NullLogger<RetornoHistoricoService>.Instance);

        await servico.StartAsync(CancellationToken.None);
        await Task.Delay(500);
        await servico.StopAsync(CancellationToken.None);

        Assert.Null(provider.Atual);
    }
}
