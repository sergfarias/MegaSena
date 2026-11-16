using MegaSena.Application.Models;
using MegaSena.Application.Portas;
using Microsoft.Extensions.Logging;

namespace MegaSena.Infrastructure.Email;

/// <summary>Usado quando o e-mail não está configurado: não envia, apenas registra o aviso.</summary>
public sealed class NullEmailSender : IEmailSender
{
    private readonly ILogger<NullEmailSender> _log;

    public NullEmailSender(ILogger<NullEmailSender> log) => _log = log;

    public Task<bool> EnviarAsync(EmailMensagem mensagem, CancellationToken ct = default)
    {
        _log.LogWarning("E-mail NÃO enviado (Email:Host não configurado). Assunto: {Assunto}", mensagem.Assunto);
        return Task.FromResult(false);
    }
}
