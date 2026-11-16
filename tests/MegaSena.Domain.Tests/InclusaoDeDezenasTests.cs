
using System.Numerics;
using MegaSena.Domain;
using MegaSena.Domain.Calculadora;
using Xunit;

namespace MegaSena.Domain.Tests;

public class InclusaoDeDezenasTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void ProbabilidadesDeInclusao_BatemComForcaBruta(int maxPares)
    {
        var universo = new Universo(Enumerable.Range(1, 12));

        var dp = ContadorDeJogosValidos.ProbabilidadesDeInclusao(universo, maxPares);

        long total = 0;
        var contem = new long[61];
        for (int mascara = 0; mascara < (1 << 12); mascara++)
        {
            if (BitOperations.PopCount((uint)mascara) != 6) continue;

            var dezenas = Enumerable.Range(1, 12).Where(d => (mascara & (1 << (d - 1))) != 0).ToArray();
            if (Jogo.DasDezenas(dezenas).ParesConsecutivos() > maxPares) continue;

            total++;
            foreach (var d in dezenas) contem[d]++;
        }

        Assert.True(total > 0);
        for (int d = 1; d <= 12; d++)
            Assert.Equal((double)contem[d] / total, dp[d], precision: 12);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void ProbabilidadesDeInclusao_SomamSeis(int maxPares)
    {
        var p = ContadorDeJogosValidos.ProbabilidadesDeInclusao(Universo.Completo(), maxPares);

        Assert.Equal(6.0, p.Sum(), precision: 9);
    }

    [Fact]
    public void ProbabilidadesDeInclusao_SemRestricao_SaoTodasIguaisA6SobreN()
    {
        var p = ContadorDeJogosValidos.ProbabilidadesDeInclusao(Universo.Completo(), maxPares: 5);

        for (int d = 1; d <= 60; d++)
            Assert.Equal(6.0 / 60.0, p[d], precision: 12);
    }

    [Fact]
    public void ProbabilidadesDeInclusao_ComRegra_PontasTemMaisChanceQueOMeio()
    {
        var p = ContadorDeJogosValidos.ProbabilidadesDeInclusao(Universo.Completo(), maxPares: 1);

        Assert.True(p[1] > p[30]);
        Assert.Equal(p[1], p[60], precision: 12);   // simetria
        Assert.Equal(p[7], p[54], precision: 12);
    }

    [Fact]
    public void ProbabilidadesDeInclusao_RegraImpossivel_RetornaZeros()
    {
        // 8 dezenas consecutivas, 0 pares: não existe jogo de 6 sem nenhum par consecutivo
        var p = ContadorDeJogosValidos.ProbabilidadesDeInclusao(new Universo(Enumerable.Range(1, 8)), maxPares: 0);

        Assert.All(p, x => Assert.Equal(0.0, x));
    }

    [Fact]
    public void CalcularComProbabilidades_FrequenciasExatamenteIguaisAoEsperado_DaZero()
    {
        var universo = Universo.Completo();
        var p = ContadorDeJogosValidos.ProbabilidadesDeInclusao(universo, maxPares: 1);
        int n = 100_000;

        var freq = new int[61];
        for (int d = 1; d <= 60; d++) freq[d] = (int)Math.Round(n * p[d]);

        var chi2 = CalculadoraQuiQuadrado.CalcularComProbabilidades(freq, n, universo.Dezenas, p, out int gl);

        Assert.True(chi2 < 1.0);
        Assert.Equal(59, gl);
    }
}
