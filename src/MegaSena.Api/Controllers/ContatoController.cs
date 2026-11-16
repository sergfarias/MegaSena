using MegaSena.Api.Models;
using MegaSena.Application.Models;
using MegaSena.Application.Portas;
using MegaSena.Infrastructure.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.Net;

namespace MegaSena.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContatoController : ControllerBase
{
    private readonly IMensagemContatoRepository _repo;

    private readonly IEmailSender _email;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<ContatoController> _log;

    public ContatoController(
        IMensagemContatoRepository repo,
        IEmailSender email,
        IOptions<EmailOptions> emailOptions,
        ILogger<ContatoController> log)
    {
        _repo = repo;
        _email = email;
        _emailOptions = emailOptions.Value;
        _log = log;
    }

    /// <summary>
    /// Endpoint público para o formulário de contato do site.
    /// Não exige autenticação (qualquer visitante pode enviar).
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("contato")]
    public async Task<IActionResult> Enviar([FromBody] ContatoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Mensagem))
        {
            return BadRequest(new { Erro = "Todos os campos são obrigatórios." });
        }

        var mensagem = new MensagemContatoDto
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome.Trim(),
            Email = request.Email.Trim(),
            Mensagem = request.Mensagem.Trim(),
            DataEnvio = DateTime.UtcNow,
            Lida = false
        };

        await _repo.AdicionarAsync(mensagem);

        // Aviso ao administrador em segundo plano: não atrasa nem derruba a resposta
        _ = NotificarAdministradorAsync(mensagem);

        return Ok(new
        {
            Mensagem = "Recebemos sua mensagem! Responderemos em breve no e-mail informado."
        });
    }

    /// <summary>
    /// Endpoint administrativo para ver as mensagens recebidas.
    /// Requer autenticação E perfil de Administrador.
    /// </summary>
    [HttpGet("mensagens")]
    [Authorize(Roles = "Admin")] // <-- MUDANÇA AQUI
    public async Task<IActionResult> ListarMensagens()
    {
        var mensagens = await _repo.ObterTodasAsync();
        return Ok(mensagens);
    }

    /// <summary>
    /// Marca uma mensagem como lida.
    /// Requer autenticação E perfil de Administrador.
    /// </summary>
    [HttpPut("{id}/lida")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> MarcarLida(Guid id)
    {
        await _repo.MarcarComoLidaAsync(id);
        return NoContent();
    }

    private async Task NotificarAdministradorAsync(MensagemContatoDto m)
    {
        var admin = _emailOptions.AdminAddress;
        if (string.IsNullOrWhiteSpace(admin)) return;

        try
        {
            var corpo =
                $"<p><strong>Nome:</strong> {WebUtility.HtmlEncode(m.Nome)}</p>" +
                $"<p><strong>E-mail:</strong> {WebUtility.HtmlEncode(m.Email)}</p>" +
                $"<p><strong>Mensagem:</strong></p><p>{WebUtility.HtmlEncode(m.Mensagem).Replace("\n", "<br>")}</p>";

            await _email.EnviarAsync(new EmailMensagem(
                admin,
                $"[MegaSena SaaS] Nova mensagem de {m.Nome}",
                corpo,
                ResponderPara: m.Email));
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Falha ao notificar o administrador sobre nova mensagem de contato.");
        }
    }
}
