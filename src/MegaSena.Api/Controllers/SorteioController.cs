using MegaSena.Api.Contracts;
using MegaSena.Application.CasosDeUso;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MegaSena.Api.Controllers;

[ApiController]
[Route("api/MegaSena")]
public class SorteioController : ControllerBase
{
    private readonly GerarJogoUseCase _gerarJogoUseCase;
    private readonly GerarLoteJogosUseCase _gerarLoteJogosUseCase;
    private readonly ILogger<SorteioController> _logger;

    public SorteioController(
        GerarJogoUseCase gerarJogoUseCase,
        GerarLoteJogosUseCase gerarLoteJogosUseCase,
        ILogger<SorteioController> logger)
    {
        _gerarJogoUseCase = gerarJogoUseCase;
        _gerarLoteJogosUseCase = gerarLoteJogosUseCase;
        _logger = logger;
    }

    [HttpGet("sortear")]
    [EnableRateLimiting("sortear-fisico")]
    [EndpointSummary("Sorteia um jogo de 6 dezenas")]
    [EndpointDescription("Os filtros 'ultimos' e 'menos' reduzem o universo de dezenas, mas NÃO aumentam a chance de acerto (sorteios são independentes). O modo 'fisico' é uma demonstração visual.")]
    [ProducesResponseType(typeof(SortearResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Sortear([FromQuery] bool fisico = false, [FromQuery] int ultimos = 3, [FromQuery] int menos = 4)
    {
        try
        {
            ultimos = Math.Clamp(ultimos, 0, 5);
            menos = Math.Clamp(menos, 0, 10);

            var resultado = await _gerarJogoUseCase.ExecutarAsync(ultimos, menos, fisico, maxPares: 1);

            return Ok(new SortearResponse(
                resultado.Jogo.Dezenas().ToArray(),
                resultado.Universo.Tamanho,
                fisico ? "Físico" : "Rápido",
                fisico ? Avisos.GloboFisico : null,
                (ultimos > 0 || menos > 0) ? Avisos.Filtros : null));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Parâmetros inválidos.");
            return BadRequest(new ErroResponse("Parâmetros inválidos para a operação."));
        }
        catch (InvalidOperationException)
        {
            _logger.LogWarning("Regra de jogo impossível para este universo.");
            return BadRequest(new ErroResponse("Regra de jogo impossível para este universo."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao sortear.");
            return StatusCode(500, new ErroResponse("Erro interno ao realizar o sorteio."));
        }
    }

    [HttpPost("gerar-lote")]
    [EndpointSummary("Gera um lote de jogos aleatórios (até 50)")]
    [ProducesResponseType(typeof(LoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GerarLote([FromBody] LoteRequest request)
    {
        try
        {
            if (request.Quantidade < 1 || request.Quantidade > 50)
                return BadRequest(new ErroResponse("A quantidade deve ser entre 1 e 50."));

            var jogos = (await _gerarLoteJogosUseCase.ExecutarAsync(
                request.Quantidade,
                Math.Clamp(request.Ultimos, 0, 5),
                Math.Clamp(request.Menos, 0, 10),
                Math.Clamp(request.MaxPares, 0, 5)))
                .Select(j => j.Dezenas().ToArray())
                .ToList();

            return Ok(new LoteResponse(jogos.Count, jogos));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Parâmetros inválidos.");
            return BadRequest(new ErroResponse("Parâmetros inválidos para a operação."));
        }
        catch (InvalidOperationException)
        {
            _logger.LogWarning("Regra de jogo impossível para este universo.");
            return BadRequest(new ErroResponse("Regra de jogo impossível para este universo."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao gerar lote.");
            return StatusCode(500, new ErroResponse("Erro interno ao gerar o lote."));
        }
    }
}