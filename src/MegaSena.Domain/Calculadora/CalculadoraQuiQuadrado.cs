
namespace MegaSena.Domain.Calculadora;

public static class CalculadoraQuiQuadrado
{
    /// <summary>
    /// Calcula o Qui-Quadrado para um conjunto de frequências de dezenas sorteadas, supondo que TODAS as
    /// dezenas do universo têm a mesma chance de aparecer (sorteio sem regra).
    /// Aplica o fator de correção (N-1)/(N-6) para sorteios sem reposição
    /// (ajusta o valor esperado do estatístico aos graus de liberdade).
    /// </summary>
    public static double Calcular(int[] frequencias, int totalSorteios, IReadOnlyList<int> dezenasUniverso)
    {
        int N = dezenasUniverso.Count;

        // O fator (N-1)/(N-6) divide por zero com N = 6 (e fica negativo abaixo disso).
        if (N <= Jogo.DezenasPorJogo)
            throw new ArgumentException("O universo precisa ter mais de 6 dezenas.", nameof(dezenasUniverso));

        // Quantidade esperada de vezes que cada dezena deveria sair
        double esperado = (double)totalSorteios * Jogo.DezenasPorJogo / N;

        if (esperado <= 0) return 0;

        double chi2 = 0;

        // Itera APENAS sobre as dezenas que realmente existem no universo
        foreach (var d in dezenasUniverso)
        {
            double diff = frequencias[d] - esperado;
            chi2 += diff * diff / esperado;
        }

        // fator (N–1)/(N–6) para ajustar o valor esperado aos graus de liberdade
        chi2 *= (N - 1.0) / (N - Jogo.DezenasPorJogo);

        return chi2;
    }

    /// <summary>
    /// Versão para quando há uma regra de jogo (por exemplo, máximo de pares consecutivos). Com a regra, as dezenas
    /// deixam de ter a mesma chance: as das pontas do universo entram em mais jogos válidos que as do meio.
    /// Cada dezena usa a frequência esperada exata = totalSorteios × P(dezena está no jogo), onde as
    /// probabilidades vêm de <see cref="ContadorDeJogosValidos.ProbabilidadesDeInclusao"/>.
    /// O fator (K-1)/(K-6) continua valendo, porque a soma das probabilidades de inclusão é sempre 6
    /// (K = quantidade de dezenas com probabilidade maior que zero).
    /// </summary>
    public static double CalcularComProbabilidades(
        int[] frequencias, int totalSorteios, IReadOnlyList<int> dezenasUniverso,
        double[] probabilidadeDeInclusao, out int grausDeLiberdade)
    {
        double chi2 = 0;
        int usadas = 0;

        foreach (var d in dezenasUniverso)
        {
            double esperado = totalSorteios * probabilidadeDeInclusao[d];
            if (esperado <= 0) continue;

            usadas++;
            double diff = frequencias[d] - esperado;
            chi2 += diff * diff / esperado;
        }

        if (usadas <= Jogo.DezenasPorJogo)
            throw new ArgumentException("A regra deixa 6 dezenas ou menos possíveis neste universo.", nameof(probabilidadeDeInclusao));

        chi2 *= (usadas - 1.0) / (usadas - Jogo.DezenasPorJogo);
        grausDeLiberdade = usadas - 1;
        return chi2;
    }
}

