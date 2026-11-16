using MegaSena.Application.Models;

namespace MegaSena.Application.Portas;

public interface IEmailSender
{
    /// <summary>Envia o e-mail. Devolve false se não foi possível enviar (a falha é registrada em log).</summary>
    Task<bool> EnviarAsync(EmailMensagem mensagem, CancellationToken ct = default);
}
