using MegaSena.Api.Services;
using MegaSena.Application.CasosDeUso;
using MegaSena.Application.Portas;
using MegaSena.Application.Servicos;
using MegaSena.Domain.Cobertura;
using MegaSena.Infrastructure;
using MegaSena.Infrastructure.Cobertura;
using MegaSena.Infrastructure.Seguranca;

namespace MegaSena.Api.Extensions;

public static class ApiDomainExtensions
{
    public static IServiceCollection AddServicosDeDominio(this IServiceCollection services)
    {
        // Serviços Base
        services.AddTransient<AuthService>();
        services.AddTransient<AuditoriaService>();
        services.AddScoped<ContaUseCase>();
        services.AddTransient<CalcularProbabilidadeUseCase>();

        // Configurações Físicas e Geradores
        var baseDir = AppContext.BaseDirectory;
        var configFisica = ConfiguracaoFisica.Carregar(Path.Combine(baseDir, "simulador.json"));
        services.AddSingleton(configFisica);
        services.AddTransient<GeradorAleatorioRapido>();
        services.AddTransient<SimuladorFisicoGerador>();
        services.AddTransient<IGeradorDeSorteioFactory, GeradorDeSorteioFactory>();

        // Solvers e Cobertura
        services.AddSingleton<ISolverGate, SolverGate>();
        services.AddSingleton<ISimulacaoGate, SimulacaoGate>();
        services.AddTransient<GulosoCoberturaSolver>();
        services.AddTransient<SolverExato>();
        services.AddTransient<ICoberturaSolver, SolverRoteador>();
        services.AddTransient<GerarCoberturaUseCase>();

        // Outros Casos de Uso
        services.AddScoped<IUniversoFactory, UniversoFactoryPadrao>();
        services.AddTransient<GerarJogoUseCase>();
        services.AddTransient<GerarLoteJogosUseCase>();
        services.AddTransient<TestarUniformidadeUseCase>();
        services.AddTransient<BacktestLoteUseCase>();
        services.AddTransient<CompararEstrategiasUseCase>();

        // Background Provider de Retorno
        services.AddSingleton<IRetornoHistoricoProvider, RetornoHistoricoProvider>();

        return services;
    }
}