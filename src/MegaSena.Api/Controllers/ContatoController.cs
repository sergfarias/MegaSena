using MegaSena.Api.Models;
using MegaSena.Application.Models;
using MegaSena.Application.Portas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MegaSena.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContatoController : ControllerBase
{
    private readonly IMensagemContatoRepository _repo;

    public ContatoController(IMensagemContatoRepository repo)
    {
        _repo = repo;
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
}

