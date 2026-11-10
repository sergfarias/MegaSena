using MegaSena.Domain;

namespace MegaSena.Domain.Calculadora;

public static class ProbabilidadeHipergeometrica
{
    /// <summary>
    /// P(acertar exatamente 'acertos' dezenas com uma aposta de 'dezenasAposta').
    /// </summary>
    public static double Exata(int dezenasAposta, int acertos)
    {
        if (acertos < 0 || acertos > 6 || acertos > dezenasAposta) return 0;

        // Fórmula da Hipergeométrica: (C(aposta, acertos) * C(60 - aposta, 6 - acertos)) / C(60, 6)
        // Fazemos cast para double para garantir a divisão decimal
        double combinacaoAposta = Binomial(dezenasAposta, acertos);
        double combinacaoResto = Binomial(60 - dezenasAposta, 6 - acertos);
        double combinacaoTotal = Binomial(60, 6);

        return (combinacaoAposta * combinacaoResto) / combinacaoTotal;
    }

    public static double PeloMenos(int dezenasAposta, int minimo)
    {
        double soma = 0;
        for (int a = minimo; a <= Math.Min(6, dezenasAposta); a++)
            soma += Exata(dezenasAposta, a);
        return soma;
    }

    public static long Binomial(int n, int k)
    {
        if (k > n) return 0;
        if (k == 0 || k == n) return 1;
        long r = 1;
        for (int i = 1; i <= k; i++) r = r * (n - k + i) / i;
        return r;
    }
}