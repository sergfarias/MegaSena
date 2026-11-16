using MegaSena.Api.Contracts;
using MegaSena.Application.CasosDeUso;
using MegaSena.Application.Portas;
using MegaSena.Domain;
using MegaSena.Domain.Calculadora;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MegaSena.Api.Services;

namespace MegaSena.Api.Controllers;

[ApiController]
[Route("api/MegaSena")]
public class EstatisticasController : ControllerBase
{
    private readonly IUniversoFactory _universoFactory;
    private readonly IGeradorDeSorteioFactory _geradorFactory;
    private readonly IConcursoRepository _concursoRepository;
    private readonly TestarUniformidadeUseCase _testarUniformidadeUseCase;
    private readonly CompararEstrategiasUseCase _compararEstrategiasUseCase;
    private readonly ISimulacaoGate _gate;
    private readonly ILogger<EstatisticasController> _logger;

    public EstatisticasController(
        IUniversoFactory universoFactory,
        IGeradorDeSorteioFactory geradorFactory,
        IConcursoRepository concursoRepository,
        TestarUniformidadeUseCase testarUniformidadeUseCase,
        CompararEstrategiasUseCase compararEstrategiasUseCase,
        ISimulacaoGate gate,
        ILogger<EstatisticasController> logger)
    {
        _universoFactory = universoFactory;
        _geradorFactory = geradorFactory;
        _concursoRepository = concursoRepository;
        _testarUniformidadeUseCase = testarUniformidadeUseCase;
        _compararEstrategiasUseCase = compararEstrategiasUseCase;
        _gate = gate;
        _logger = logger;
    }

    [HttpGet("contar")]
    [EndpointSummary("Conta os jogos válidos em cada universo de dezenas")]
    [EndpointDescription("Devolve, por universo, o tamanho real, o total de combinações C(n,6) e os jogos válidos após a regra de pares consecutivos (maxPares). Os campos universo42/38/60 são mantidos por compatibilidade e trazem só os jogos válidos.")]
    [ProducesResponseType(typeof(ContarResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Contar([FromQuery] int maxPares = 1, [FromQuery] int ultimos = 3, [FromQuery] int menos = 4)
    {
        ultimos = Math.Clamp(ultimos, 0, 5);
        menos = Math.Clamp(menos, 0, 10);
        maxPares = Math.Clamp(maxPares, 0, 5);

        var completo = DetalharUniverso(Universo.Completo(), maxPares);

        var uSemUltimos = await _universoFactory.CriarAsync(ultimos, 0);
        var semUltimos = DetalharUniverso(uSemUltimos, maxPares);

        var uSemUltimosEMenos = await _universoFactory.CriarAsync(ultimos, menos);
        var semUltimosEMenos = DetalharUniverso(uSemUltimosEMenos, maxPares);

        return Ok(new ContarResponse(
            Universo42: semUltimos.JogosValidos,
            Universo38: semUltimosEMenos.JogosValidos,
            Universo60: completo.JogosValidos,
            MaxPares: maxPares,
            Ultimos: ultimos,
            Menos: menos,
            Detalhes: new DetalhesContagemResponse(completo, semUltimos, semUltimosEMenos),
            Observacao: "Jogos válidos = combinações de 6 dezenas com no máximo 'maxPares' pares consecutivos. " +
                        "O tamanho do universo varia conforme as dezenas repetidas nos últimos sorteios."));
    }

    private static DetalheUniversoResponse DetalharUniverso(Universo universo, int maxPares)
    {
        long total = Combinatoria.C(universo.Tamanho, 6);
        long validos = ContadorDeJogosValidos.Contar(universo, maxPares);
        return new DetalheUniversoResponse(universo.Tamanho, total, validos, total == 0 ? 0 : (double)validos / total);
    }

    [HttpGet("uniformidade")]
    [EnableRateLimiting("uniformidade")]
    [EndpointSummary("Teste qui-quadrado de uniformidade do gerador")]
    [EndpointDescription("Testa o gerador escolhido. No modo 'fisico' o teste avalia o gerador físico (simulação de bolas num globo com jato de ar e colisões); por causa do custo, n fica entre 50 e 100 nesse modo.")]
    [ProducesResponseType(typeof(UniformidadeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Uniformidade(
     [FromQuery] bool fisico = false,
     [FromQuery] int n = 300,
     [FromQuery] int ultimos = 3,
     [FromQuery] int menos = 4,
     [FromQuery] int? maxPares = null)
    {
        try
        {
            int nMax = fisico ? 100 : 20_000;
            if (n < 50 || n > nMax)
                return BadRequest(new ErroResponse($"n deve estar entre 50 e {nMax}."));

            // Só o modo físico é pesado: limita a simultaneidade (o modo rápido não entra no gate)
            using var vaga = fisico ? _gate.TryEnter() : null;
            if (fisico && vaga is null)
                return ServidorOcupado.Resposta(this);

            var universo = await _universoFactory.CriarAsync(Math.Clamp(ultimos, 0, 5), Math.Clamp(menos, 0, 10));
            var gerador = _geradorFactory.Criar(fisico);

            IRegraDeJogo regra = maxPares is null
                ? new RegraSemRestricao()
                : new MaximoParesConsecutivos(maxPares.Value);

            var r = _testarUniformidadeUseCase.Executar(gerador, universo, n, regra);

            return Ok(new UniformidadeResponse(
                r.QuiQuadrado,
                r.GrausDeLiberdade,
                r.PValor,
                r.SemEvidenciaDeVies,
                universo.Tamanho,
                regra.Descricao,
                fisico ? Avisos.GloboFisico : null));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Parâmetros inválidos em uniformidade.");
            return BadRequest(new ErroResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao testar uniformidade.");
            return StatusCode(500, new ErroResponse("Erro interno ao testar a uniformidade."));
        }
    }

    [HttpGet("probabilidade")]
    [EndpointSummary("Probabilidades e retorno esperado de uma aposta")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status400BadRequest)]
    public IActionResult Probabilidade(
        [FromServices] CalcularProbabilidadeUseCase useCase,
        [FromServices] IRetornoHistoricoProvider retorno,
        [FromQuery] int dezenas = 6)
    {
        if (dezenas is < 6 or > 20)
            return BadRequest(new ErroResponse("Dezenas deve estar entre 6 e 20."));

        var t = retorno.Atual;
        return Ok(useCase.Executar(dezenas, t?.Taxa, t?.Concursos ?? 0));
    }

    [HttpGet("qui-quadrado")]
    [EndpointSummary("Qui-quadrado sobre os concursos reais")]
    [ProducesResponseType(typeof(QuiQuadradoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> QuiQuadrado([FromQuery] int janela = 200)
    {
        try
        {
            janela = Math.Clamp(janela, 50, 2000);

            var concursos = await _concursoRepository.ObterUltimosAsync(janela);
            if (concursos.Count < 50)
            {
                return UnprocessableEntity(new
                {
                    Erro = $"Apenas {concursos.Count} concursos disponíveis. Mínimo de 50 para o cálculo.",
                    Disponivel = false
                });
            }

            var freq = new int[61];
            int n = concursos.Count;

            foreach (var c in concursos)
            {
                foreach (var d in c.DezenasSorteadas.Split(','))
                {
                    if (int.TryParse(d.Trim(), out int num) && num >= 1 && num <= 60)
                        freq[num]++;
                }
            }

            var universo = Universo.Completo();
            double chi2 = CalculadoraQuiQuadrado.Calcular(freq, n, universo.Dezenas);
            int gl = universo.Tamanho - 1;
            double p = DistribuicaoQuiQuadrado.PValor(chi2, gl);

            return Ok(new QuiQuadradoResponse(chi2, gl, p, p >= 0.05, true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no cálculo do Qui-Quadrado.");
            return StatusCode(500, new ErroResponse("Erro interno no cálculo do Qui-Quadrado."));
        }
    }

    [HttpGet("comparar-estrategias")]
    [EndpointSummary("Compara uma aposta múltipla com jogos simples de mesmo custo")]
    [EndpointDescription("Informe 'dezenas' (ou 'dezenasMultipla', nome antigo). A probabilidade dos jogos simples é uma aproximação 1-(1-P)^K, que supõe jogos independentes.")]
    [ProducesResponseType(typeof(CompararEstrategiasResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status400BadRequest)]
    public IActionResult CompararEstrategias([FromQuery] int? dezenas = null, [FromQuery] int? dezenasMultipla = null)
    {
        try
        {
            var r = _compararEstrategiasUseCase.Executar(dezenas ?? dezenasMultipla ?? 8);

            return Ok(new CompararEstrategiasResponse(
                r.DezenasMultipla,
                r.CustoTotal,
                r.QtdJogosSimples,
                r.ProbMultipla,
                r.ProbJogosSimples,
                "A probabilidade de jogos simples é uma aproximação estatística (1 - (1-P)^K).",
                r.Conclusao));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Parâmetros inválidos.");
            return BadRequest(new ErroResponse("Parâmetros inválidos para a operação."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao comparar estratégias.");
            return StatusCode(500, new ErroResponse("Erro interno ao comparar as estratégias."));
        }
    }
}
