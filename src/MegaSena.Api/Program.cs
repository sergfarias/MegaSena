using MegaSena.Api.Services;
using MegaSena.Application.CasosDeUso;
using MegaSena.Application.Portas;
using MegaSena.Application.Servicos;
using MegaSena.Domain.Cobertura;
using MegaSena.Infrastructure;
using MegaSena.Infrastructure.Cobertura;
using MegaSena.Infrastructure.Data;
using MegaSena.Infrastructure.Repositories;
using MegaSena.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Scalar.AspNetCore;
using System.Threading.RateLimiting;
using MegaSena.Infrastructure.Seguranca;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. CONFIGURAÇÕES DE SEGURANÇA (JWT e CORS)
// ==========================================

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key não configurada no appsettings ou variáveis de ambiente.");

if (System.Text.Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("A chave Jwt:Key deve ter pelo menos 32 bytes (32 caracteres).");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "MegaSenaApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "MegaSenaFront";
var key = System.Text.Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddHealthChecks();

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience
    };
});

// CORS CONFIGURADO E VALIDADO
var origens = builder.Configuration.GetSection("Cors:Origens").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFront", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(_ => true).AllowAnyMethod().AllowAnyHeader();
        }
        else
        {
            if (origens.Length == 0)
                throw new InvalidOperationException("Cors:Origens não configurado no appsettings para produção.");

            policy.WithOrigins(origens).AllowAnyMethod().AllowAnyHeader();
        }
    });
});

// ==========================================
// 2. RATE LIMITER (Proteção contra abuso)
// ==========================================
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
        }

        await context.HttpContext.Response.WriteAsJsonAsync(
            new { Erro = "Muitas requisições. Por favor, aguarde alguns instantes e tente novamente." },
            cancellationToken: token);
    };

    if (builder.Environment.IsEnvironment("Testing"))
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ => RateLimitPartition.GetNoLimiter("test"));
        options.AddPolicy("fechamento", _ => RateLimitPartition.GetNoLimiter("test"));
        options.AddPolicy("contato", _ => RateLimitPartition.GetNoLimiter("test"));
        options.AddPolicy("auth", _ => RateLimitPartition.GetNoLimiter("test"));
        options.AddPolicy("senha", _ => RateLimitPartition.GetNoLimiter("test"));
        return;
    }

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("fechamento", context =>
    {
        var usuarioId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: usuarioId ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

    options.AddPolicy("contato", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("senha", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

// ==========================================
// 3. INJEÇÃO DE DEPENDÊNCIA (Serviços)
// ==========================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddOpenApi();
builder.Services.AddTransient<AuthService>();
builder.Services.AddTransient<AuditoriaService>();
builder.Services.AddHttpClient<ResultadoService>();

// BANCO DE DADOS
var isTesting = builder.Environment.IsEnvironment("Testing");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (isTesting)
    {
        options.UseInMemoryDatabase("MegaSenaTestDb");
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
});

builder.Services.AddScoped<IHistoricoRepository, HistoricoDbRepository>();
builder.Services.AddScoped<IUniversoFactory, UniversoFactoryPadrao>();

var baseDir = AppContext.BaseDirectory;
var configFisica = ConfiguracaoFisica.Carregar(Path.Combine(baseDir, "simulador.json"));
builder.Services.AddSingleton(configFisica);
builder.Services.AddTransient<GeradorAleatorioRapido>();
builder.Services.AddTransient<SimuladorFisicoGerador>();

builder.Services.AddTransient<GulosoCoberturaSolver>();
builder.Services.AddTransient<SolverExato>();
builder.Services.AddTransient<ICoberturaSolver, SolverRoteador>();
builder.Services.AddTransient<GerarCoberturaUseCase>();
builder.Services.AddScoped<ILoteRepository, LoteRepository>();
builder.Services.AddScoped<IMensagemContatoRepository, MensagemContatoRepository>();

builder.Services.AddSingleton<IRetornoHistoricoProvider, RetornoHistoricoProvider>();
builder.Services.AddScoped<IConcursoRepository, ConcursoRepository>();
builder.Services.AddTransient<CalcularProbabilidadeUseCase>();

// NOVOS REGISTROS (Fábrica e Casos de Uso)
builder.Services.AddTransient<IGeradorDeSorteioFactory, GeradorDeSorteioFactory>();
builder.Services.AddTransient<GerarJogoUseCase>();
builder.Services.AddTransient<GerarLoteJogosUseCase>();
builder.Services.AddTransient<TestarUniformidadeUseCase>();
builder.Services.AddTransient<BacktestLoteUseCase>();
builder.Services.AddTransient<CompararEstrategiasUseCase>();

builder.Services.AddHttpClient("ApiCaixa")
    .AddTransientHttpErrorPolicy(policy =>
        policy.WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(5)));

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService<RetornoHistoricoService>();
    builder.Services.AddHostedService<ImportadorConcursosService>();
}

var app = builder.Build();

// ==========================================
// 4. PIPELINE DA APLICAÇÃO
// ==========================================

// ADICIONADO: Forwarded Headers para funcionar atrás de proxy (runasp.net)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseCors("AllowFront");
app.UseHttpsRedirection();
app.UseHsts();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }