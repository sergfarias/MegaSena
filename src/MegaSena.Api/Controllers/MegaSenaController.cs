using MegaSena.Application.CasosDeUso;
using MegaSena.Application.Portas;
using MegaSena.Application.Servicos;
using MegaSena.Domain;
using MegaSena.Domain.Calculadora;
using MegaSena.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MegaSena.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MegaSenaController : ControllerBase
{
    private readonly IUniversoFactory _universoFactory;
    private readonly IGeradorDeSorteioFactory _geradorFactory; // NOVO
    private readonly ResultadoService _resultadoService;
    private readonly ILogger<MegaSenaController> _logger;
    private readonly IConcursoRepository _concursoRepository;

    // Casos de uso injetados
    private readonly GerarJogoUseCase _gerarJogoUseCase;
    private readonly GerarLoteJogosUseCase _gerarLoteJogosUseCase;
    private readonly TestarUniformidadeUseCase _testarUniformidadeUseCase;
    private readonly CompararEstrategiasUseCase _compararEstrategiasUseCase;

    public MegaSenaController(
           IUniversoFactory universoFactory,
           IGeradorDeSorteioFactory geradorFactory,
           ResultadoService resultadoService,
           ILogger<MegaSenaController> logger,
           IConcursoRepository concursoRepository,
           GerarJogoUseCase gerarJogoUseCase,
           GerarLoteJogosUseCase gerarLoteJogosUseCase,
           TestarUniformidadeUseCase testarUniformidadeUseCase,
           CompararEstrategiasUseCase compararEstrategiasUseCase)
    {
        _universoFactory = universoFactory;
        _geradorFactory = geradorFactory;
        _resultadoService = resultadoService;
        _logger = logger;
        _concursoRepository = concursoRepository;
        _gerarJogoUseCase = gerarJogoUseCase;
        _gerarLoteJogosUseCase = gerarLoteJogosUseCase;
        _testarUniformidadeUseCase = testarUniformidadeUseCase;
        _compararEstrategiasUseCase = compararEstrategiasUseCase;
    }


    //[HttpGet("sortear")]
    //public IActionResult Sortear([FromQuery] bool fisico = false, [FromQuery] int ultimos = 3, [FromQuery] int menos = 4)
    //{
    //    try
    //    {
    //        // Proteção contra valores absurdos (Seção 3.6)
    //        ultimos = Math.Clamp(ultimos, 0, 5);
    //        menos = Math.Clamp(menos, 0, 10);

    //        var regra = new MaximoParesConsecutivos(1);
    //        IGeradorDeSorteio gerador = fisico ? _geradorFisico : _geradorRapido;

    //        var useCase = new GerarJogoUseCase(gerador, _universoFactory, regra);

    //        // Executa o sorteio (agora lendo o histórico direto do banco, sem API externa)
    //        var resultado = useCase.Executar(ultimos, menos);

    //        return Ok(new
    //        {
    //            Jogos = resultado.Jogo.Dezenas(),
    //            Universo = resultado.Universo.Tamanho,
    //            Motor = gerador.Nome
    //        });
    //    }
    //    catch (ArgumentException ex)
    //    {
    //        _logger.LogWarning(ex, "Parâmetros inválidos.");
    //        return BadRequest(new { Erro = "Parâmetros inválidos para a operação." });
    //    }
    //    catch (InvalidOperationException)
    //    {
    //        _logger.LogWarning("Regra de jogo impossível para este universo.");
    //        return BadRequest(new { Erro = "Regra de jogo impossível para este universo." });
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Erro inesperado ao sortear.");
    //        return StatusCode(500, new { Erro = "Erro interno ao realizar o sorteio." });
    //    }
    //}


    [HttpGet("sortear")]
    public IActionResult Sortear([FromQuery] bool fisico = false, [FromQuery] int ultimos = 3, [FromQuery] int menos = 4)
    {
        try
        {
            ultimos = Math.Clamp(ultimos, 0, 5);
            menos = Math.Clamp(menos, 0, 10);

            // Usa o caso de uso injetado (sem o new!)
            var resultado = _gerarJogoUseCase.Executar(ultimos, menos, fisico, maxPares: 1);

            return Ok(new
            {
                Jogos = resultado.Jogo.Dezenas(),
                Universo = resultado.Universo.Tamanho,
                Motor = fisico ? "Físico" : "Rápido"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Parâmetros inválidos.");
            return BadRequest(new { Erro = "Parâmetros inválidos para a operação." });
        }
        catch (InvalidOperationException)
        {
            _logger.LogWarning("Regra de jogo impossível para este universo.");
            return BadRequest(new { Erro = "Regra de jogo impossível para este universo." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao sortear.");
            return StatusCode(500, new { Erro = "Erro interno ao realizar o sorteio." });
        }
    }

    [HttpGet("contar")]
    public IActionResult Contar([FromQuery] int maxPares = 1, [FromQuery] int ultimos = 3, [FromQuery] int menos = 4)
    {
        // Proteção contra valores absurdos
        ultimos = Math.Clamp(ultimos, 0, 5);
        menos = Math.Clamp(menos, 0, 10);
        maxPares = Math.Clamp(maxPares, 0, 5);

        var u42 = _universoFactory.Criar(ultimos, 0);
        var u38 = _universoFactory.Criar(ultimos, menos);

        return Ok(new
        {
            Universo42 = ContadorDeJogosValidos.Contar(u42, maxPares),
            Universo38 = ContadorDeJogosValidos.Contar(u38, maxPares),
            Universo60 = ContadorDeJogosValidos.Contar(Universo.Completo(), maxPares)
        });
    }

    //[HttpGet("uniformidade")]
    //public IActionResult Uniformidade([FromQuery] bool fisico = false, [FromQuery] int n = 300,
    //    [FromQuery] int ultimos = 3, [FromQuery] int menos = 4, [FromQuery] int? maxPares = null)
    //{
    //    try
    //    {
    //        int nMax = fisico ? 500 : 100_000;
    //        if (n < 50 || n > nMax)
    //            return BadRequest(new { Erro = $"n deve estar entre 50 e {nMax}." });

    //        var universo = _universoFactory.Criar(Math.Clamp(ultimos, 0, 10), Math.Clamp(menos, 0, 10));
    //        IGeradorDeSorteio gerador = fisico ? _geradorFisico : _geradorRapido;
    //        IRegraDeJogo regra = maxPares is null ? new RegraSemRestricao() : new MaximoParesConsecutivos(maxPares.Value);

    //        var r = new TestarUniformidadeUseCase().Executar(gerador, universo, n, regra);
    //        return Ok(new
    //        {
    //            r.QuiQuadrado,
    //            r.GrausDeLiberdade,
    //            r.PValor,
    //            r.SemEvidenciaDeVies,
    //            Universo = universo.Tamanho,
    //            Filtro = regra.Descricao
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Erro ao testar uniformidade.");
    //        return StatusCode(500, new { Erro = "Erro interno ao testar a uniformidade." });
    //    }
    //}

    [HttpGet("uniformidade")]
    public IActionResult Uniformidade([FromQuery] bool fisico = false, [FromQuery] int n = 300,
    [FromQuery] int ultimos = 3, [FromQuery] int menos = 4, [FromQuery] int? maxPares = null)
    {
        try
        {
            int nMax = fisico ? 500 : 100_000;
            if (n < 50 || n > nMax)
                return BadRequest(new { Erro = $"n deve estar entre 50 e {nMax}." });

            var universo = _universoFactory.Criar(Math.Clamp(ultimos, 0, 10), Math.Clamp(menos, 0, 10));

            // Usa a Factory injetada
            var gerador = _geradorFactory.Criar(fisico);

            IRegraDeJogo regra = maxPares is null ? new RegraSemRestricao() : new MaximoParesConsecutivos(maxPares.Value);

            // Usa o caso de uso injetado
            var r = _testarUniformidadeUseCase.Executar(gerador, universo, n, regra);

            return Ok(new
            {
                r.QuiQuadrado,
                r.GrausDeLiberdade,
                r.PValor,
                r.SemEvidenciaDeVies,
                Universo = universo.Tamanho,
                Filtro = regra.Descricao
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao testar uniformidade.");
            return StatusCode(500, new { Erro = "Erro interno ao testar a uniformidade." });
        }
    }



    [HttpGet("probabilidade")]
    public IActionResult Probabilidade([FromQuery] int dezenas = 6,
        [FromServices] CalcularProbabilidadeUseCase useCase = null!,
        [FromServices] IRetornoHistoricoProvider retorno = null!)
    {
        if (dezenas is < 6 or > 20) return BadRequest(new { Erro = "Dezenas deve estar entre 6 e 20." });
        var t = retorno.Atual;
        return Ok(useCase.Executar(dezenas, t?.Taxa, t?.Concursos ?? 0));
    }

    //[HttpPost("gerar-lote")]
    //public IActionResult GerarLote([FromBody] LoteRequest request)
    //{
    //    try
    //    {
    //        if (request.Quantidade < 1 || request.Quantidade > 50)
    //            return BadRequest(new { Erro = "A quantidade deve ser entre 1 e 50." });

    //        // Proteção contra valores absurdos (Seção 3.6)
    //        int ultimos = Math.Clamp(request.Ultimos, 0, 5);
    //        int menos = Math.Clamp(request.Menos, 0, 10);
    //        int maxPares = Math.Clamp(request.MaxPares, 0, 5);

    //        var regra = new MaximoParesConsecutivos(maxPares);
    //        var useCase = new GerarLoteJogosUseCase(_geradorRapido, _universoFactory, regra);
    //        var jogos = useCase.Executar(request.Quantidade, ultimos, menos);

    //        return Ok(new
    //        {
    //            Total = jogos.Count(),
    //            Jogos = jogos.Select(j => j.Dezenas().ToArray()).ToList()
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Erro ao gerar lote público.");
    //        return BadRequest(new { Erro = "Não foi possível gerar o lote com os parâmetros fornecidos." });
    //    }
    //}

    [HttpPost("gerar-lote")]
    public IActionResult GerarLote([FromBody] LoteRequest request)
    {
        try
        {
            if (request.Quantidade < 1 || request.Quantidade > 50)
                return BadRequest(new { Erro = "A quantidade deve ser entre 1 e 50." });

            // Usa o caso de uso injetado
            var jogos = _gerarLoteJogosUseCase.Executar(
                request.Quantidade,
                Math.Clamp(request.Ultimos, 0, 5),
                Math.Clamp(request.Menos, 0, 10),
                Math.Clamp(request.MaxPares, 0, 5));

            return Ok(new
            {
                Total = jogos.Count(),
                Jogos = jogos.Select(j => j.Dezenas().ToArray()).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar lote público.");
            return BadRequest(new { Erro = "Não foi possível gerar o lote com os parâmetros fornecidos." });
        }
    }


    [HttpGet("ultimo-sorteio")]
    public async Task<IActionResult> UltimoSorteio()
    {
        var resultado = await _resultadoService.ObterUltimoSorteioAsync();

        // Como o ResultadoService ainda devolve um objeto anônimo, 
        // verificamos via reflection (mas de forma mais segura).
        var tipo = resultado.GetType();
        var sucessoProp = tipo.GetProperty("Sucesso");

        if (sucessoProp != null && !(bool)sucessoProp.GetValue(resultado)!)
        {
            return BadRequest(new { Erro = "Não foi possível obter o último sorteio no momento." });
        }

        return Ok(resultado);
    }

    [HttpGet("concursos")]
    public async Task<IActionResult> Concursos([FromQuery] int quantidade = 20)
    {
        var lista = await _concursoRepository.ObterUltimosAsync(Math.Clamp(quantidade, 1, 500));
        return Ok(lista.Select(c => new
        {
            c.NumeroConcurso,
            c.DataHoraSorteio,
            Dezenas = c.DezenasSorteadas.Split(','),
            c.Acumulado,
            c.ValorArrecadado,
            c.QtdGanhadoresSena
        }));
    }

    [HttpGet("qui-quadrado")]
    public async Task<IActionResult> QuiQuadrado([FromQuery] int janela = 200)
    {
        try
        {
            // Proteção contra janela negativa ou absurda (Seção 3.8)
            janela = Math.Clamp(janela, 50, 2000);

            var concursos = await _concursoRepository.ObterUltimosAsync(janela);
            if (concursos.Count < 50)
            {
                // Retorna 422 Unprocessable Entity se não houver dados suficientes
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

            return Ok(new
            {
                Chi2 = chi2,
                Gl = gl,
                PValor = p,
                SemEvidenciaDeVies = p >= 0.05,
                Disponivel = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no cálculo do Qui-Quadrado.");
            return StatusCode(500, new { Erro = "Erro interno no cálculo do Qui-Quadrado." });
        }
    }

    //[HttpGet("comparar-estrategias")]
    //public IActionResult CompararEstrategias([FromQuery] int dezenasMultipla = 8)
    //{
    //    try
    //    {
    //        var useCase = new CompararEstrategiasUseCase();
    //        var resultado = useCase.Executar(dezenasMultipla);

    //        return Ok(new
    //        {
    //            resultado.DezenasMultipla,
    //            resultado.CustoTotal,
    //            resultado.QtdJogosSimples,
    //            ProbMultipla = resultado.ProbMultipla,
    //            ProbJogosSimples = resultado.ProbJogosSimples,
    //            Observacao = "A probabilidade de jogos simples é uma aproximação estatística (1 - (1-P)^K).",
    //            resultado.Conclusao
    //        });
    //    }
    //    catch (ArgumentException ex)
    //    {
    //        _logger.LogWarning(ex, "Parâmetros inválidos.");
    //        return BadRequest(new { Erro = "Parâmetros inválidos para a operação." });
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Erro ao comparar estratégias.");
    //        return StatusCode(500, new { Erro = "Erro interno ao comparar as estratégias." });
    //    }
    //}

    [HttpGet("comparar-estrategias")]
    public IActionResult CompararEstrategias([FromQuery] int dezenasMultipla = 8)
    {
        try
        {
            // Usa o caso de uso injetado
            var resultado = _compararEstrategiasUseCase.Executar(dezenasMultipla);

            return Ok(new
            {
                resultado.DezenasMultipla,
                resultado.CustoTotal,
                resultado.QtdJogosSimples,
                ProbMultipla = resultado.ProbMultipla,
                ProbJogosSimples = resultado.ProbJogosSimples,
                Observacao = "A probabilidade de jogos simples é uma aproximação estatística (1 - (1-P)^K).",
                resultado.Conclusao
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Parâmetros inválidos.");
            return BadRequest(new { Erro = "Parâmetros inválidos para a operação." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao comparar estratégias.");
            return StatusCode(500, new { Erro = "Erro interno ao comparar as estratégias." });
        }
    }

}