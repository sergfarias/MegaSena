namespace MegaSena.Domain.Calculadora;

public static class PrecoAposta
{
    public const decimal PrecoSimples = 6.00m; // R$ 6,00
    public const int DezenasSimples = 6;

    /// <summary>
    /// Preço oficial = C(d,6) * preço simples.
    /// </summary>
    public static decimal Calcular(int dezenas)
    {
        if (dezenas < 6 || dezenas > 20) // Limite oficial da Caixa
            throw new ArgumentOutOfRangeException(nameof(dezenas), "A aposta deve ter entre 6 e 20 dezenas.");

        return (decimal)ProbabilidadeHipergeometrica.Binomial(dezenas, DezenasSimples) * PrecoSimples;
    }
}