namespace MegaSena.Application.Models;

public class ConcursoDto
{
    public int NumeroConcurso { get; set; }
    public DateTime DataHoraSorteio { get; set; }
    public string? CidadeSorteio { get; set; }
    public string? UfSorteio { get; set; }
    public string DezenasSorteadas { get; set; } = string.Empty;
    public string? DezenasOrdemSorteio { get; set; }
    public int QtdGanhadoresSena { get; set; }
    public int QtdGanhadoresQuina { get; set; }
    public int QtdGanhadoresQuadra { get; set; }
    public decimal? ValorPremioSena { get; set; }
    public decimal? ValorPremioQuina { get; set; }
    public decimal? ValorPremioQuadra { get; set; }
    public decimal? ValorArrecadado { get; set; }
    public bool Acumulado { get; set; }
    public decimal? ValorAcumulado { get; set; }
    public string? ListaRateioPremioJson { get; set; }
    public int? NumeroConcursoAnterior { get; set; }
    public int? ProximoConcurso { get; set; }
    public DateTime? DataProximoConcurso { get; set; }
}
