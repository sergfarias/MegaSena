using Microsoft.AspNetCore.Mvc;

namespace MegaSena.Api.Extensions;

public static class ApiMvcExtensions
{
    public static IServiceCollection AddMvcCustomizado(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var mensagens = new List<string>();
                    foreach (var (campo, entrada) in context.ModelState)
                    {
                        foreach (var e in entrada.Errors)
                        {
                            var msg = e.ErrorMessage;
                            if (string.IsNullOrWhiteSpace(msg) || msg.Contains("is required"))
                                msg = $"O campo '{campo}' é obrigatório.";
                            else if (msg.Contains("valid e-mail address"))
                                msg = "E-mail inválido.";
                            else if (msg.Contains("maximum length"))
                                msg = $"O campo '{campo}' excede o tamanho máximo permitido.";
                            mensagens.Add(msg);
                        }
                    }

                    return new BadRequestObjectResult(new
                    {
                        Erro = mensagens.FirstOrDefault() ?? "Requisição inválida.",
                        Detalhes = mensagens
                    });
                };
            });

        services.AddOpenApi();
        return services;
    }
}