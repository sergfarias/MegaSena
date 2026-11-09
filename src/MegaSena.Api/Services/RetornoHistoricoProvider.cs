using MegaSena.Application.Portas;
using MegaSena.Domain.Calculadora;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MegaSena.Api.Services;

public sealed class RetornoHistoricoProvider : IRetornoHistoricoProvider
{
    private TaxaRetorno? _atual;
    public TaxaRetorno? Atual => Volatile.Read(ref _atual);
    public void Atualizar(TaxaRetorno taxa) => Volatile.Write(ref _atual, taxa);
}

public sealed class RetornoHistoricoService : BackgroundService
{
    private const int Janela = 200;
    private readonly IServiceScopeFactory _scopes;
    private readonly IRetornoHistoricoProvider _provider;
    private readonly ILogger<RetornoHistoricoService> _log;

    public RetornoHistoricoService(
        IServiceScopeFactory scopes,
        IRetornoHistoricoProvider provider,
        ILogger<RetornoHistoricoService> log)
    {
        _scopes = scopes;
        _provider = provider;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();

                // Usa o banco de dados em vez da API externa
                var repo = scope.ServiceProvider.GetRequiredService<IConcursoRepository>();
                var concursos = await repo.ObterUltimosAsync(Janela);

                if (concursos.Count >= 50)
                {
                    // Mapeia os dados do banco (ConcursoDto) para o modelo financeiro
                    var financeiros = concursos
                        .Where(c => c.ValorArrecadado.HasValue && c.ValorArrecadado > 0)
                        .Select(c => new ConcursoFinanceiro(
                            c.ValorArrecadado!.Value,
                            (c.ValorPremioSena ?? 0) * c.QtdGanhadoresSena +
                            (c.ValorPremioQuina ?? 0) * c.QtdGanhadoresQuina +
                            (c.ValorPremioQuadra ?? 0) * c.QtdGanhadoresQuadra
                        ))
                        .ToList();

                    if (financeiros.Count > 0)
                    {
                        var taxa = RetornoHistorico.Taxa(financeiros);
                        _provider.Atualizar(new TaxaRetorno(taxa, financeiros.Count, DateTime.UtcNow));
                        _log.LogInformation("Taxa de retorno atualizada do banco: {Taxa:P2} com {Qtd} concursos.", taxa, financeiros.Count);
                    }
                }
                else
                {
                    _log.LogWarning("Ainda não há concursos suficientes no banco para calcular a taxa ({Qtd}/50).", concursos.Count);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.LogWarning(ex, "Falha ao atualizar a taxa de retorno histórica.");
            }

            // Se não tem dados (ou deu erro), tenta de novo em 15 minutos. 
            // Se tem dados, atualiza a cada 24 horas (já que os sorteios são semanais).
            var delay = _provider.Atual == null ? TimeSpan.FromMinutes(15) : TimeSpan.FromHours(24);
            await Task.Delay(delay, ct);
        }
    }
}