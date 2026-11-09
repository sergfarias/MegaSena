using MegaSena.Application.Portas;
using MegaSena.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MegaSena.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditoriaController : ControllerBase
{
    private readonly ILoteRepository _loteRepository;
    private readonly AuditoriaService _auditoriaService;

    public AuditoriaController(ILoteRepository loteRepository, AuditoriaService auditoriaService)
    {
        _loteRepository = loteRepository;
        _auditoriaService = auditoriaService;
    }

    [HttpGet("{hash}")]
    public async Task<IActionResult> BuscarLote(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return BadRequest(new { Erro = "O Hash é obrigatório." });

        var resultado = await _loteRepository.ObterLotePorHashAsync(hash.Trim().ToUpper());

        if (resultado == null)
            return NotFound(new { Erro = "Hash não encontrado. Verifique o código digitado." });

        var hashCalculadoNoBanco = _auditoriaService.GerarHashAuditoria(resultado.Value.JsonOriginal);
        var integridadeConfirmada = hashCalculadoNoBanco == hash.Trim().ToUpper();

        return Ok(new
        {
            Existe = true,
            DataGeracao = resultado.Value.DataGeracao,
            IntegridadeConfirmada = integridadeConfirmada,
            Mensagem = "Lote autêntico. Para visualizar os jogos, acesse sua conta e vá em 'Meus Lotes'."
        });
    }

    [HttpGet("meus-lotes")]
    [Authorize]
    public async Task<IActionResult> MeusLotes()
    {
        var usuarioIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(usuarioIdString) || !Guid.TryParse(usuarioIdString, out var usuarioId))
            return Unauthorized(new { Erro = "Usuário não autenticado corretamente." });

        var lotes = await _loteRepository.ObterLotesPorUsuarioAsync(usuarioId);
        if (lotes == null || !lotes.Any())
            return Ok(new List<object>());

        // Seção 3.5: Evitar devolver o JsonOriginal na listagem para economizar banda
        var lotesOtimizados = lotes.Select(l => new
        {
            l.Id,
            l.Hash,
            l.DataCriacao,
            l.QuantidadeJogos
            // JsonOriginal ocultado
        });

        return Ok(lotesOtimizados);
    }

    [HttpGet("meus-lotes/{hash}")]
    [Authorize]
    public async Task<IActionResult> MeuLoteDetalhado(string hash)
    {
        var usuarioIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(usuarioIdString) || !Guid.TryParse(usuarioIdString, out var usuarioId))
            return Unauthorized(new { Erro = "Usuário não autenticado." });

        var hashLimpo = hash.Trim().ToUpper();

        // Busca o lote
        // Uma única query por hash + usuário. Responde sempre 404 (não revela se o hash existe para outro usuário).
        var resultado = await _loteRepository.ObterLoteDoUsuarioPorHashAsync(usuarioId, hashLimpo);
        if (resultado == null)
            return NotFound(new { Erro = "Lote não encontrado." });

        // Recalcula a integridade
        var hashCalculado = _auditoriaService.GerarHashAuditoria(resultado.Value.JsonOriginal);
        var integridade = hashCalculado == hashLimpo;

        // Retorna o lote completo (só para o dono)
        return Ok(new
        {
            Hash = hashLimpo,
            DataGeracao = resultado.Value.DataGeracao,
            IntegridadeConfirmada = integridade,
            Jogos = resultado.Value.Jogos
                .Select(j => j.Select(d => d.ToString("00")).ToArray())
                .ToList(),
            QuantidadeJogos = resultado.Value.Jogos.Count()
        });
    }
}

