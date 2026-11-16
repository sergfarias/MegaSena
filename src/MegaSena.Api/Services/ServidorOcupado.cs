using Microsoft.AspNetCore.Mvc;

namespace MegaSena.Api.Services;

/// <summary>Resposta 429 padrão quando o SolverGate não tem vaga, com Retry-After.</summary>
public static class ServidorOcupado
{
    public static IActionResult Resposta(ControllerBase controller)
    {
        controller.Response.Headers.RetryAfter = "5";
        return controller.StatusCode(429, new { Erro = "Servidor ocupado. Tente novamente em instantes." });
    }
}