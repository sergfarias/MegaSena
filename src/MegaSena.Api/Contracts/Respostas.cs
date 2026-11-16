using System.Text.Json.Serialization;

namespace MegaSena.Api.Contracts;

public sealed record ErroResponse(
    string Erro,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? Detalhes = null);

public sealed record SortearResponse(int[] Jogos, int Universo, string Motor, string? AvisoMotor, string? AvisoFiltros);

public sealed record LoteResponse(int Total, IReadOnlyList<int[]> Jogos);

public sealed record DetalheUniversoResponse(int Tamanho, long TotalCombinacoes, long JogosValidos, double Aproveitamento);

public sealed record DetalhesContagemResponse(
    DetalheUniversoResponse Completo,
    DetalheUniversoResponse SemUltimos,
    DetalheUniversoResponse SemUltimosEMenos);

public sealed record ContarResponse(
    long Universo42,
    long Universo38,
    long Universo60,
    int MaxPares,
    int Ultimos,
    int Menos,
    DetalhesContagemResponse Detalhes,
    string Observacao);

public sealed record UniformidadeResponse(
    double QuiQuadrado,
    int GrausDeLiberdade,
    double PValor,
    bool SemEvidenciaDeVies,
    int Universo,
    string Filtro,
    string? AvisoMotor);

public sealed record QuiQuadradoResponse(double Chi2, int Gl, double PValor, bool SemEvidenciaDeVies, bool Disponivel);

public sealed record CompararEstrategiasResponse(
    int DezenasMultipla,
    decimal CustoTotal,
    int QtdJogosSimples,
    double ProbMultipla,
    double ProbJogosSimples,
    string Observacao,
    string Conclusao);

public sealed record ConcursoResumoResponse(
    int NumeroConcurso,
    DateTime DataHoraSorteio,
    string[] Dezenas,
    bool Acumulado,
    decimal? ValorArrecadado,
    int QtdGanhadoresSena);
