using MegaSena.Domain;

namespace MegaSena.Domain.Calculadora;

public static class CalculadoraQuiQuadrado
{
    /// <summary>
    /// Calcula o Qui-Quadrado para um conjunto de frequências de dezenas sorteadas.
    /// Aplica o fator de correção (N-6)/(N-1) para sorteios sem reposição.
    /// </summary>
    public static double Calcular(int[] frequencias, int totalSorteios, int tamanhoUniverso)
    {
        // Quantidade esperada de vezes que cada dezena deveria sair
        double esperado = (double)totalSorteios * Jogo.DezenasPorJogo / tamanhoUniverso;

        if (esperado <= 0) return 0;

        double chi2 = 0;
        for (int d = 1; d <= tamanhoUniverso; d++)
        {
            double diff = frequencias[d] - esperado;
            chi2 += diff * diff / esperado;
        }

        // Aplica o fator de correção para amostragem sem reposição (N-6)/(N-1)
        chi2 *= (tamanhoUniverso - 1.0) / (tamanhoUniverso - Jogo.DezenasPorJogo);

        return chi2;
    }
}