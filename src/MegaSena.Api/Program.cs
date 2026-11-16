using MegaSena.Api.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. INJEÇÃO DE DEPENDÊNCIA (Dividida em Extensions)
// ==========================================
builder.Services.AddHealthChecks();

// JWT, CORS, RateLimit
builder.Services.AddAutenticacaoJwt(builder.Configuration);
builder.Services.AddCorsDoFront(builder.Configuration, builder.Environment);
builder.Services.AddRateLimitingDaApi(builder.Environment);

// MVC (JSON em camelCase e erros de validação), serviços de domínio e infraestrutura (banco, e-mail, jobs)
builder.Services.AddMvcCustomizado();
builder.Services.AddServicosDeDominio();
builder.Services.AddInfraestrutura(builder.Configuration, builder.Environment);

var app = builder.Build();

// ==========================================
// 2. PIPELINE DA APLICAÇÃO
// ==========================================
var forwarded = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    ForwardLimit = 1
};

foreach (var ip in app.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? Array.Empty<string>())
{
    if (IPAddress.TryParse(ip, out var endereco))
        forwarded.KnownProxies.Add(endereco);
}

app.UseForwardedHeaders(forwarded);

app.UseCors("AllowFront");
app.UseHttpsRedirection();
if (!app.Environment.IsDevelopment())
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