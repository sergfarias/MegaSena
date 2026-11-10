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

        double esperado = (double)n * Jogo.DezenasPorJogo / N;
        if (esperado < 5)
            throw new ArgumentException($"n muito pequeno: esperado por dezena = {esperado:F1} (mínimo 5).");

        var freq = new int[61]; // 1..60
        for (int i = 0; i < n; i++)
            foreach (var d in gerador.Sortear(universo, regra).Dezenas())
                freq[d]++;

        // Usa o serviço unificado
        double chi2 = CalculadoraQuiQuadrado.Calcular(freq, n, universo.Dezenas);

        int gl = N - 1;
        double p = DistribuicaoQuiQuadrado.PValor(chi2, gl);

        return new ResultadoUniformidade(chi2, gl, p, p >= nivel);
    }
}