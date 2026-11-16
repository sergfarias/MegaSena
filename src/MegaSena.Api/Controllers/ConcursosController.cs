using MegaSena.Api.Contracts;
using MegaSena.Application.Models;
using MegaSena.Application.Portas;
using MegaSena.Application.Servicos;
using Microsoft.AspNetCore.Mvc;

namespace MegaSena.Api.Controllers;

[ApiController]
[Route("api/MegaSena")]
public class ConcursosController : ControllerBase
{
    private readonly ResultadoService _resultadoService;
    private readonly IConcursoRepository _concursoRepository;

    public ConcursosController(ResultadoService resultadoService, IConcursoRepository concursoRepository)
    {
        _resultadoService = resultadoService;
        _concursoRepository = concursoRepository;
    }

    [HttpGet("ultimo-sorteio")]
    [EndpointSummary("Último sorteio (banco; fallback na API externa)")]
    [ProducesResponseType(typeof(ResultadoUltimoSorteio), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UltimoSorteio()
    {
        // 1) tenta o banco primeiro (já temos o concurso importado)
        var ultimos = await _concursoRepository.ObterUltimosAsync(1);
        if (ultimos.Count > 0)
        {
            var c = ultimos[0];
            return Ok(new ResultadoUltimoSorteio(
                Sucesso: true,
                Concurso: c.NumeroConcurso,
                Data: c.DataHoraSorteio.ToString("dd/MM/yyyy"),
                Dezenas: c.DezenasSorteadas.Split(',').Select(int.Parse).ToList()));
        }

        // 2) fallback: API externa (só quando o banco está vazio, ex.: primeiro boot)
        var resultado = await _resultadoService.ObterUltimoSorteioAsync();
        if (!resultado.Sucesso)
            return BadRequest(new ErroResponse(resultado.Erro ?? "Não foi possível obter o último sorteio no momento."));
        return Ok(resultado);
    }

    [HttpGet("concursos")]
    [EndpointSummary("Últimos concursos salvos no banco (1 a 500)")]
    [ProducesResponseType(typeof(IEnumerable<ConcursoResumoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Concursos([FromQuery] int quantidade = 20)
    {
        var lista = await _concursoRepository.ObterUltimosAsync(Math.Clamp(quantidade, 1, 500));

        return Ok(lista.Select(c => new ConcursoResumoResponse(
            c.NumeroConcurso,
            c.DataHoraSorteio,
            c.DezenasSorteadas.Split(','),
            c.Acumulado,
            c.ValorArrecadado,
            c.QtdGanhadoresSena)));
    }
}
