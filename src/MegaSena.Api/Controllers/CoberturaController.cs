using MegaSena.Api.Models;
using MegaSena.Application.CasosDeUso;
using MegaSena.Application.Portas;
using MegaSena.Infrastructure.Seguranca;
using MegaSena.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Text.Json;

namespace MegaSena.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("fechamento")]
public class CoberturaController : ControllerBase
{
    private const int MaxDezenas = 20;
    private const int MaxJogosBacktest = 1000;
    private const int JanelaMin = 50;
    private const int JanelaMax = 2000;

    // No máximo 2 cálculos simultâneos no servidor (cada solve de 20 dezenas usa ~300 MB)
    private static readonly SemaphoreSlim _solver = new(2);

    private readonly GerarCoberturaUseCase _useCase;
    private readonly AuditoriaService _auditoriaService;
    private readonly ILoteRepository _loteRepository;
    private readonly ILogger<CoberturaController> _logger;
    private readonly BacktestLoteUseCase _backtestUseCase;

    public CoberturaController(
        GerarCoberturaUseCase useCase,
        AuditoriaService auditoriaService,
        ILoteRepository loteRepository,
        ILogger<CoberturaController> logger, BacktestLoteUseCase backtestUseCase)
    {
        _useCase = useCase;
        _auditoriaService = auditoriaService;
        _loteRepository = loteRepository;
        _logger = logger;
        _backtestUseCase = backtestUseCase;
    }

    [HttpPost("gerar")]
    public async Task<IActionResult> GerarFechamento([FromBody] FechamentoRequest request)
    {
        var usuarioIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(usuarioIdString) || !Guid.TryParse(usuarioIdString, out var usuarioId))
            return Unauthorized(new { Erro = "Usuário não autenticado corretamente." });

        // ---- Validações de entrada ----
        if (request?.Dezenas == null || request.Dezenas.Count < 6)
            return BadRequest(new { Erro = "É necessário escolher pelo menos 6 dezenas." });

        if (request.Dezenas.Count > MaxDezenas)
            return BadRequest(new { Erro = $"O limite máximo para fechamento é de {MaxDezenas} dezenas." });

        if (request.Dezenas.Any(d => d < 1 || d > 60))
            return BadRequest(new { Erro = "As dezenas devem estar entre 1 e 60." });

        var dezenasSet = request.Dezenas.ToHashSet();
        if (dezenasSet.Count != request.Dezenas.Count)
            return BadRequest(new { Erro = "Há dezenas repetidas na lista." });

        if (request.MaxPares < 0 || request.MaxPares > 5)
            return BadRequest(new { Erro = "maxPares deve estar entre 0 e 5." });

        // ---- ITEM 3: Novas validações específicas ----
        if (request.Garantia < 1 || request.Garantia > 6)
            return BadRequest(new { Erro = "A garantia deve estar entre 1 e 6." });

        if (request.UniversoSorteado < request.Garantia || request.UniversoSorteado > 6)
            return BadRequest(new { Erro = "O universo sorteado deve ser maior ou igual à garantia e no máximo 6." });
        // --------------------------------------------

        // ---- Limite global de execuções simultâneas ----
        if (!await _solver.WaitAsync(0))
            return StatusCode(429, new { Erro = "Servidor ocupado. Tente novamente em instantes." });

        try
        {
            // Token ligado ao cancelamento da requisição + timeout de 15 s
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted);
            cts.CancelAfter(TimeSpan.FromSeconds(15));

            var resultado = await Task.Run(() =>
                _useCase.Executar(
                    dezenasSet,
                    request.Garantia,
                    request.UniversoSorteado,
                    request.MaxPares,
                    cts.Token), cts.Token);

            if (resultado.TotalJogos == 0 || !resultado.GarantiaTotal)
            {
                return BadRequest(new
                {
                    Erro = "Não foi possível garantir a cobertura com essas restrições. " +
                           "Tente aumentar as dezenas, diminuir a garantia ou permitir mais pares consecutivos (maxPares).",
                    CombinacoesCobertas = resultado.CombinacoesCobertas,
                    TotalCombinacoes = resultado.TotalCombinacoesSorteaveis
                });
            }

            var jogosArray = resultado.Cobertura.Jogos.Select(j => j.Dezenas().ToArray()).ToList();

            var payloadAuditoria = new
            {
                Parametros = new
                {
                    Dezenas = request.Dezenas.OrderBy(x => x).ToArray(),
                    request.Garantia,
                    request.UniversoSorteado,
                    request.MaxPares
                },
                Jogos = jogosArray,
                Nonce = Guid.NewGuid(),
                CriadoEmUtc = DateTime.UtcNow
            };
            var jsonOriginal = JsonSerializer.Serialize(payloadAuditoria);
            var hashString = _auditoriaService.GerarHashAuditoria(jsonOriginal);

            await _loteRepository.SalvarLoteAsync(usuarioId, hashString, jsonOriginal, jogosArray);

            return Ok(new
            {
                resultado.TotalJogos,
                resultado.CustoTotal,
                TotalCombinacoes = resultado.TotalCombinacoesSorteaveis,
                resultado.CombinacoesCobertas,
                resultado.GarantiaTotal,
                resultado.MinimoProvado, // <-- ITEM 4 adicionado aqui
                HashAuditoria = hashString,
                Jogos = jogosArray
            });
        }
        catch (OperationCanceledException)
        {
            if (HttpContext.RequestAborted.IsCancellationRequested)
                return StatusCode(499); // cliente desistiu
            return StatusCode(408, new { Erro = "O cálculo demorou muito (mais de 15 segundos). Tente reduzir a quantidade de dezenas ou a garantia." });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Parâmetros inválidos.");
            return BadRequest(new { Erro = "Parâmetros inválidos para a operação." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar fechamento.");
            return StatusCode(500, new { Erro = "Erro interno ao calcular a cobertura." });
        }
        finally
        {
            _solver.Release();
        }
    }

    [HttpPost("backtest")]
    public async Task<IActionResult> Backtest(
        [FromBody] List<int[]> jogos,
        [FromServices] IConcursoRepository concursoRepo,
        [FromQuery] int janela = 500)
    {
        try
        {
            if (jogos is null || jogos.Count == 0)
                return BadRequest(new { Erro = "Envie pelo menos 1 jogo para o backtest." });

            if (jogos.Count > MaxJogosBacktest)
                return BadRequest(new { Erro = $"Máximo de {MaxJogosBacktest} jogos por backtest." });

            if (jogos.Any(j => j is null || j.Length != 6))
                return BadRequest(new { Erro = "Cada jogo deve ter exatamente 6 dezenas." });

            janela = Math.Clamp(janela, JanelaMin, JanelaMax);

            var lote = jogos.Select(j => Jogo.DasDezenas(j)).ToList();

            var concursos = await concursoRepo.ObterUltimosAsync(janela);
            if (concursos.Count < JanelaMin)
                return UnprocessableEntity(new { Erro = $"Apenas {concursos.Count} concursos disponíveis. Mínimo de {JanelaMin}." });

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            //var resultado = new BacktestLoteUseCase().Executar(lote, concursos, janela, cts.Token);
            var resultado = _backtestUseCase.Executar(lote, concursos, janela, cts.Token);
            return Ok(resultado);
        }
        catch (OperationCanceledException)
        {
            if (HttpContext.RequestAborted.IsCancellationRequested)
                return StatusCode(499);
            return StatusCode(408, new { Erro = "O backtest demorou muito. Reduza a quantidade de jogos ou a janela." });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Parâmetros inválidos.");
            return BadRequest(new { Erro = "Parâmetros inválidos para a operação." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no backtest.");
            return StatusCode(500, new { Erro = "Erro interno no backtest." });
        }
    }
}