using System.Net;
using System.Net.Mail;
using System.Text;
using MegaSena.Application.Models;
using MegaSena.Application.Portas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MegaSena.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _log;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> log)
    {
        _options = options.Value;
        _log = log;
    }

    public async Task<bool> EnviarAsync(EmailMensagem mensagem, CancellationToken ct = default)
    {
        try
        {
            using var mail = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = LimparAssunto(mensagem.Assunto),
                Body = mensagem.CorpoHtml,
                IsBodyHtml = true,
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8
            };
            mail.To.Add(new MailAddress(mensagem.Para));

            if (!string.IsNullOrWhiteSpace(mensagem.ResponderPara))
                mail.ReplyToList.Add(new MailAddress(mensagem.ResponderPara));

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.UseSsl
            };

            if (!string.IsNullOrWhiteSpace(_options.User))
                client.Credentials = new NetworkCredential(_options.User, _options.Password);

            await client.SendMailAsync(mail, ct);
            return true;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Falha ao enviar e-mail (assunto: {Assunto}).", mensagem.Assunto);
            return false;
        }
    }

    // Evita quebra de linha no assunto (injeção de cabeçalho)
    private static string LimparAssunto(string assunto) =>
        assunto.Replace("\r", " ").Replace("\n", " ").Trim();
}
