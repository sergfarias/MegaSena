using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using MegaSena.Application.Portas;
using MegaSena.Application.Servicos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace MegaSena.Api.Extensions;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddAutenticacaoJwt(this IServiceCollection services, IConfiguration config)
    {
        var jwtKey = config["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key não configurada no appsettings ou variáveis de ambiente.");

        if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
            throw new InvalidOperationException("A chave Jwt:Key deve ter pelo menos 32 bytes (32 caracteres).");

        var issuer = config["Jwt:Issuer"] ?? "MegaSenaApi";
        var audience = config["Jwt:Audience"] ?? "MegaSenaFront";
        var key = Encoding.UTF8.GetBytes(jwtKey);

        services.AddAuthentication(x =>
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
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience
            };

            x.Events = new JwtBearerEvents
            {
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { Erro = "Sessão expirada ou inválida. Faça login novamente." });
                },
                OnForbidden = async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new { Erro = "Você não tem permissão para acessar este recurso." });
                },
                OnTokenValidated = async context =>
                {
                    var usuarioRepo = context.HttpContext.RequestServices.GetRequiredService<IUsuarioRepository>();

                    var idClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out var usuarioId))
                    {
                        context.Fail("Token sem identificador de usuário.");
                        return;
                    }

                    var usuario = await usuarioRepo.ObterPorIdAsync(usuarioId);
                    if (usuario == null || !usuario.Ativo)
                    {
                        context.Fail("Conta inativa ou removida.");
                        return;
                    }

                    var carimbo = context.Principal?.FindFirst("pwd")?.Value;
                    if (carimbo != AuthService.CarimboDaSenha(usuario.SenhaHash))
                        context.Fail("Senha alterada desde a emissão do token.");
                }
            };
        });

        return services;
    }

    public static IServiceCollection AddCorsDoFront(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
    {
        var origens = config.GetSection("Cors:Origens").Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            options.AddPolicy("AllowFront", policy =>
            {
                if (env.IsDevelopment())
                {
                    policy.SetIsOriginAllowed(_ => true).AllowAnyMethod().AllowAnyHeader()
                          .WithExposedHeaders("Retry-After");
                    return;
                }

                if (origens.Length == 0)
                    throw new InvalidOperationException("Cors:Origens não configurado no appsettings para produção.");

                policy.WithOrigins(origens).AllowAnyMethod().AllowAnyHeader()
                      .WithExposedHeaders("Retry-After");
            });
        });

        return services;
    }

    public static IServiceCollection AddRateLimitingDaApi(this IServiceCollection services, IHostEnvironment env)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { Erro = "Muitas requisições. Por favor, aguarde alguns instantes e tente novamente." },
                    cancellationToken: token);
            };

            string[] nomes = { "fechamento", "contato", "auth", "senha", "uniformidade", "sortear-fisico" };

            if (env.IsEnvironment("Testing"))
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                    _ => RateLimitPartition.GetNoLimiter("test"));
                foreach (var nome in nomes)
                    options.AddPolicy(nome, _ => RateLimitPartition.GetNoLimiter("test"));
                return;
            }

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                PorIp(ctx, 60, TimeSpan.FromMinutes(1)));

            options.AddPolicy("fechamento", ctx =>
            {
                var usuarioId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return RateLimitPartition.GetFixedWindowLimiter(
                    usuarioId ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
                    _ => Janela(5, TimeSpan.FromMinutes(1)));
            });

            options.AddPolicy("contato", ctx => PorIp(ctx, 3, TimeSpan.FromMinutes(10)));
            options.AddPolicy("auth", ctx => PorIp(ctx, 5, TimeSpan.FromMinutes(5)));
            options.AddPolicy("senha", ctx => PorIp(ctx, 3, TimeSpan.FromMinutes(15)));
            options.AddPolicy("uniformidade", ctx => PorIp(ctx, 10, TimeSpan.FromMinutes(1)));

            // Só o modo físico (CPU-intensivo) é limitado; o rápido fica só com o limite global.
            options.AddPolicy("sortear-fisico", ctx =>
            {
                var fisico = bool.TryParse(ctx.Request.Query["fisico"].ToString(), out var f) && f;
                return fisico
                    ? PorIp(ctx, 10, TimeSpan.FromMinutes(1))
                    : RateLimitPartition.GetNoLimiter("rapido");
            });
        });

        return services;
    }

    private static RateLimitPartition<string> PorIp(HttpContext ctx, int limite, TimeSpan janela) =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
            _ => Janela(limite, janela));

    private static FixedWindowRateLimiterOptions Janela(int limite, TimeSpan janela) => new()
    {
        PermitLimit = limite,
        Window = janela,
        QueueLimit = 0,
        AutoReplenishment = true
    };
}
