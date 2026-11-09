using MegaSena.Domain.Calculadora;

namespace MegaSena.Application.CasosDeUso;

public sealed record ResultadoProbabilidade(
    int Dezenas,
    decimal Custo,
    double ProbSena,
    double ProbQuina,
    double ProbQuadra,
    double ProbPeloMenosQuadra,
    decimal? RetornoEsperado,      // R$ esperados de volta; null sem dados
    decimal? RetornoPercentual,    // 0..1
    string Fonte);

public sealed class CalcularProbabilidadeUseCase
{
    public ResultadoProbabilidade Executar(
        int dezenas, decimal? taxaRetorno = null, int concursosAmostrados = 0)
    {
        var custo = PrecoAposta.Calcular(dezenas);

        double pSena = ProbabilidadeHipergeometrica.Exata(dezenas, 6);
        double pQuina = ProbabilidadeHipergeometrica.Exata(dezenas, 5);
        double pQuadra = ProbabilidadeHipergeometrica.Exata(dezenas, 4);
        double pMaiorIgualQuadra = ProbabilidadeHipergeometrica.PeloMenos(dezenas, 4);

        return new ResultadoProbabilidade(
            dezenas,
            custo,
            pSena,
            pQuina,
            pQuadra,
            pMaiorIgualQuadra,
            taxaRetorno is null ? null : custo * taxaRetorno,
            taxaRetorno,
            taxaRetorno is null
                ? "indisponível"
                : $"média dos últimos {concursosAmostrados} concursos");
    }
}