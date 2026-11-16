using MegaSena.Api.Services;
using MegaSena.Application.Portas;
using MegaSena.Application.Servicos;
using MegaSena.Infrastructure.Data;
using MegaSena.Infrastructure.Email;
using MegaSena.Infrastructure.Repositories;
using MegaSena.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace MegaSena.Api.Extensions;

public static class ApiInfrastructureExtensions
{
    public static IServiceCollection AddInfraestrutura(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        // BANCO DE DADOS
        var isTesting = env.IsEnvironment("Testing");
        services.AddDbContext<AppDbContext>(options =>
        {
            if (isTesting)
                options.UseInMemoryDatabase("MegaSenaTestDb");
            else
                options.UseSqlServer(config.GetConnectionString("DefaultConnection"));
        });

        // REPOSITÓRIOS
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IHistoricoRepository, HistoricoDbRepository>();
        services.AddScoped<IConcursoRepository, ConcursoRepository>();
        services.AddScoped<ILoteRepository, LoteRepository>();
        services.AddScoped<IMensagemContatoRepository, MensagemContatoRepository>();

        // E-MAIL
        services.Configure<EmailOptions>(config.GetSection("Email"));
        if (!string.IsNullOrWhiteSpace(config["Email:Host"]))
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        else
            services.AddSingleton<IEmailSender, NullEmailSender>();

        // CACHE E HTTP CLIENTS
        services.AddMemoryCache();
        services.AddHttpClient<ResultadoService>();
        services.AddHttpClient("ApiCaixa")
            .AddTransientHttpErrorPolicy(policy =>
                policy.WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(5)));

        // BACKGROUND JOBS
        if (!isTesting && config.GetValue("BackgroundJobs:Enabled", true))
        {
            services.AddHostedService<RetornoHistoricoService>();
            services.AddHostedService<ImportadorConcursosService>();
        }

        return services;
    }
}