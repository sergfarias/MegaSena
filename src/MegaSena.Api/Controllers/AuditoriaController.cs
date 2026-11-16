
using MegaSena.Api.Contracts;
using MegaSena.Application.Portas;
using MegaSena.Infrastructure.Seguranca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace MegaSena.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditoriaController : ControllerBase
{
    private readonly ILoteRepository _loteRepository;
    private readonly AuditoriaService _auditoriaService;

    public AuditoriaController(
        ILoteRepository loteRepository,
        AuditoriaService auditoriaService)
    {
        _loteRepository = loteRepository;
        _auditoriaService = auditoriaService;
    }

    [HttpGet("{hash}")]
    [AllowAnonymous]
    public async Task<IActionResult> BuscarLote(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return BadRequest(new { Erro = "O Hash é obrigatório." });

        var hashLimpo = hash.Trim().ToUpperInvariant();

        var resultado = await _loteRepository.ObterLotePorHashAsync(hashLimpo);

        // O repositório retorna null para lotes inexistentes ou inativos.
        if (resultado == null)
            return NotFound(new
            {
                Erro = "Hash não encontrado. Verifique o código digitado."
            });

        var hashCalculadoNoBanco =
            _auditoriaService.GerarHashAuditoria(resultado.Value.JsonOriginal);

        var integridadeConfirmada =
            string.Equals(
                hashCalculadoNoBanco,
                hashLimpo,
                StringComparison.OrdinalIgnoreCase);

        return Ok(new
        {
            Existe = true,
            DataGeracao = resultado.Value.DataGeracao,
            QuantidadeJogos = resultado.Value.Jogos.Count(),
            IntegridadeConfirmada = integridadeConfirmada,
            Mensagem = "Lote autêntico. Para visualizar os jogos, acesse sua conta e vá em 'Meus Lotes'."
        });
    }

    [HttpGet("meus-lotes")]
    [Authorize]
    public async Task<IActionResult> MeusLotes()
    {
        var usuarioIdString =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(usuarioIdString) ||
            !Guid.TryParse(usuarioIdString, out var usuarioId))
        {
            return Unauthorized(new
            {
                Erro = "Usuário não autenticado corretamente."
            });
        }

        var lotes =
            await _loteRepository.ObterLotesPorUsuarioAsync(usuarioId);

        if (lotes == null || !lotes.Any())
            return Ok(new List<object>());

        // Não retorna o JSON original na listagem; só lotes ativos (o detalhe usa a mesma regra).
        var lotesOtimizados = lotes.Select(l => new
        {
            l.Id,
            l.Hash,
            l.DataCriacao,
            l.QuantidadeJogos
        });

        return Ok(lotesOtimizados);
    }

    [HttpGet("meus-lotes/{hash}")]
    [Authorize]
    public async Task<IActionResult> MeuLoteDetalhado(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return BadRequest(new { Erro = "O Hash é obrigatório." });

        var usuarioIdString =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(usuarioIdString) ||
            !Guid.TryParse(usuarioIdString, out var usuarioId))
        {
            return Unauthorized(new
            {
                Erro = "Usuário não autenticado."
            });
        }

        var hashLimpo = hash.Trim().ToUpperInvariant();

        // O repositório valida propriedade e status Ativo.
        var resultado =
            await _loteRepository.ObterLoteDoUsuarioPorHashAsync(
                usuarioId,
                hashLimpo);

        if (resultado == null)
            return NotFound(new { Erro = "Lote não encontrado." });

        // 1. Recalcula o hash do JSON original.
        var hashCalculado =
            _auditoriaService.GerarHashAuditoria(
                resultado.Value.JsonOriginal);

        var integridadeConfirmada =
            string.Equals(
                hashCalculado,
                hashLimpo,
                StringComparison.OrdinalIgnoreCase);

        // 2. Compara os jogos armazenados com os jogos do JSON original.
        if (integridadeConfirmada)
        {
            try
            {
                var payloadJson =
                    JsonSerializer.Deserialize<AuditoriaPayload>(
                        resultado.Value.JsonOriginal);

                if (payloadJson?.Jogos == null)
                {
                    integridadeConfirmada = false;
                }
                else
                {
                    var jogosDoJson = payloadJson.Jogos
                        .Select(j => string.Join(",", j.OrderBy(x => x)))
                        .OrderBy(x => x, StringComparer.Ordinal)
                        .ToList();

                    var jogosDoBanco = resultado.Value.Jogos
                        .Select(j => string.Join(",", j.OrderBy(x => x)))
                        .OrderBy(x => x, StringComparer.Ordinal)
                        .ToList();

                    if (!jogosDoJson.SequenceEqual(jogosDoBanco))
                        integridadeConfirmada = false;
                }
            }
            catch (JsonException)
            {
                integridadeConfirmada = false;
            }
        }

        var jogosFormatados = resultado.Value.Jogos
            .Select(j => j.Select(d => d.ToString("00")).ToArray())
            .ToList();

        return Ok(new
        {
            Hash = hashLimpo,
            DataGeracao = resultado.Value.DataGeracao,
            IntegridadeConfirmada = integridadeConfirmada,
            Jogos = jogosFormatados,
            QuantidadeJogos = jogosFormatados.Count
        });
    }
}
