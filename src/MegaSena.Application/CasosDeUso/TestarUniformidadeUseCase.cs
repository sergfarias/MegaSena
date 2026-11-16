
using MegaSena.Application.Portas;
using MegaSena.Domain;
using MegaSena.Domain.Calculadora;

namespace MegaSena.Application.CasosDeUso;

public sealed record ResultadoUniformidade(
    double QuiQuadrado, int GrausDeLiberdade, double PValor, bool SemEvidenciaDeVies);

public sealed class TestarUniformidadeUseCase
{
    public ResultadoUniformidade Executar(
        IGeradorDeSorteio gerador, Universo universo, int n,
        IRegraDeJogo? regra = null, double nivel = 0.05)
    {
        regra ??= new RegraSemRestricao();
        int N = universo.Tamanho;

        if (N <= Jogo.DezenasPorJogo)
            throw new ArgumentException("O universo precisa ter mais de 6 dezenas para o teste de uniformidade.");

        double esperado = (double)n * Jogo.DezenasPorJogo / N;
        if (esperado < 5)
            throw new ArgumentException($"n muito pequeno: esperado por dezena = {esperado:F1} (mínimo 5).");

        // N10: com a regra de pares ativa, a distribuição esperada das dezenas não é uniforme.
        // (Com 5 pares ou mais a regra não restringe nada, pois um jogo só tem 5 pares possíveis.)
        double[]? probabilidades = null;
        if (regra is MaximoParesConsecutivos maxRegra && maxRegra.Maximo < 5)
        {
            probabilidades = ContadorDeJogosValidos.ProbabilidadesDeInclusao(universo, maxRegra.Maximo);
            if (probabilidades.Sum() <= 0)
                throw new ArgumentException("Regra de jogo impossível para este universo.");
        }

        var freq = new int[61]; // 1..60
        for (int i = 0; i < n; i++)
            foreach (var d in gerador.Sortear(universo, regra).Dezenas())
                freq[d]++;

        double chi2;
        int gl;
        if (probabilidades is null)
        {
            // Usa o serviço unificado
            chi2 = CalculadoraQuiQuadrado.Calcular(freq, n, universo.Dezenas);
            gl = N - 1;
        }
        else
        {
            chi2 = CalculadoraQuiQuadrado.CalcularComProbabilidades(freq, n, universo.Dezenas, probabilidades, out gl);
        }

        double p = DistribuicaoQuiQuadrado.PValor(chi2, gl);

        return new ResultadoUniformidade(chi2, gl, p, p >= nivel);
    }
}
