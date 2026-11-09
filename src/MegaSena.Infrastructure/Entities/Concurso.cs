namespace MegaSena.Infrastructure.Entities;

public class Concurso
{
    public Guid Id { get; set; }

    /// <summary>Número do concurso (ex: 2700)</summary>
    public int NumeroConcurso { get; set; }

    /// <summary>Data e hora do sorteio (UTC)</summary>
    public DateTime DataHoraSorteio { get; set; }

    /// <summary>Cidade onde o sorteio foi realizado</summary>
    public string? CidadeSorteio { get; set; }

    /// <summary>UF do sorteio (ex: "SP")</summary>
    public string? UfSorteio { get; set; }

    /// <summary>Dezenas ordenadas (ex: "01,05,12,33,44,55")</summary>
    public string DezenasSorteadas { get; set; } = string.Empty;

    /// <summary>Dezenas na ordem real em que saíram (para análise de padrões)</summary>
    public string? DezenasOrdemSorteio { get; set; }

    // ============ GANHADORES E PRÊMIOS POR FAIXA ============
    public int QtdGanhadoresSena { get; set; }
    public int QtdGanhadoresQuina { get; set; }
    public int QtdGanhadoresQuadra { get; set; }

    public decimal? ValorPremioSena { get; set; }
    public decimal? ValorPremioQuina { get; set; }
    public decimal? ValorPremioQuadra { get; set; }

    // ============ FINANCEIRO ============
    /// <summary>Total apostado no concurso</summary>
    public decimal? ValorArrecadado { get; set; }

    /// <summary>Indica se o prêmio principal acumulou</summary>
    public bool Acumulado { get; set; }

    /// <summary>Valor do prêmio acumulado para o próximo concurso</summary>
    public decimal? ValorAcumulado { get; set; }

    /// <summary>JSON completo do rateio de prêmios (para auditoria)</summary>
    public string? ListaRateioPremioJson { get; set; }

    // ============ NAVEGAÇÃO ENTRE CONCURSOS ============
    public int? NumeroConcursoAnterior { get; set; }
    public int? ProximoConcurso { get; set; }

    /// <summary>Data prevista do próximo concurso</summary>
    public DateTime? DataProximoConcurso { get; set; }

    // ============ CONTROLE INTERNO ============
    public DateTime DataImportacao { get; set; } = DateTime.UtcNow;
}