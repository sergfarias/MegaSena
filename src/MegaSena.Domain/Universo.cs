namespace MegaSena.Domain;

/// <summary>O globo: conjunto de dezenas disponíveis para sorteio.</summary>
public sealed class Universo
{
    private readonly int[] _dezenas; // ordenado crescente

    public IReadOnlyList<int> Dezenas => _dezenas;
    public int Tamanho => _dezenas.Length;

    public Universo(IEnumerable<int> dezenas)
    {
        var arr = dezenas.Distinct().OrderBy(x => x).ToArray();
        if (arr.Any(d => d is < 1 or > 60))
            throw new ArgumentOutOfRangeException(nameof(dezenas), "Dezenas devem estar em 1..60.");
        if (arr.Length < Jogo.DezenasPorJogo)
            throw new ArgumentException($"Universo precisa de ao menos {Jogo.DezenasPorJogo} dezenas.", nameof(dezenas));
        _dezenas = arr;
    }

    public static Universo Completo() => new(Enumerable.Range(1, 60));

    /// <summary>Remove as dezenas dos últimos sorteios (o globo reduzido do README).</summary>
    public static Universo Sem(IEnumerable<int> excluidas) =>
        new(Enumerable.Range(1, 60).Except(excluidas));

    public bool Contem(int d) => Array.BinarySearch(_dezenas, d) >= 0;
}