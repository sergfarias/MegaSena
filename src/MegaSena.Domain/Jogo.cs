using System.Numerics;

namespace MegaSena.Domain;

/// <summary>Um jogo de 6 dezenas representado como máscara de 60 bits.</summary>
public readonly record struct Jogo(ulong Bits)
{
    public const int DezenasPorJogo = 6;
    private const ulong MascaraValida = (1UL << 60) - 1;

    public static Jogo Vazio => new(0UL);

    /// <summary>Constrói a partir das dezenas (1..60). Lança se inválido.</summary>
    public static Jogo DasDezenas(IEnumerable<int> dezenas)
    {
        ulong bits = 0;
        int count = 0;
        foreach (var d in dezenas)
        {
            if (d is < 1 or > 60)
                throw new ArgumentOutOfRangeException(nameof(dezenas), $"Dezena {d} fora de 1..60.");
            ulong bit = 1UL << (d - 1);
            if ((bits & bit) != 0)
                throw new ArgumentException($"Dezena {d} repetida.", nameof(dezenas));
            bits |= bit;
            count++;
        }
        if (count != DezenasPorJogo)
            throw new ArgumentException($"Jogo precisa de {DezenasPorJogo} dezenas, veio {count}.", nameof(dezenas));
        return new Jogo(bits);
    }

    public int Quantidade => BitOperations.PopCount(Bits);

    public bool Contem(int dezena) =>
        dezena is >= 1 and <= 60 && (Bits & (1UL << (dezena - 1))) != 0;

    /// <summary>
    /// Quantos pares de dezenas consecutivas o jogo contém.
    /// Trio (10-11-12) conta como 2 pares. É exatamente PopCount(bits &amp; (bits &gt;&gt; 1)).
    /// </summary>
    public int ParesConsecutivos()
    {
        ulong adj = Bits & (Bits >> 1);
        return BitOperations.PopCount(adj);
    }

    /// <summary>
    /// Quantas "corridas" consecutivas de tamanho ≥ 2 o jogo contém.
    /// Trio (10-11-12) conta como 1 corrida. Soltas (30, 40, 50) NÃO contam.
    /// </summary>
    public int BlocosConsecutivos()
    {
        ulong bits = Bits;
        int corridas = 0;
        bool emCorrida = false;
        for (int i = 0; i < 60; i++)
        {
            bool setado = (bits & (1UL << i)) != 0;
            if (setado && !emCorrida)
            {
                bool proximo = i + 1 < 60 && (bits & (1UL << (i + 1))) != 0;
                if (proximo) { corridas++; emCorrida = true; }
            }
            else if (!setado) emCorrida = false;
        }
        return corridas;
    }

    public IEnumerable<int> Dezenas()
    {
        ulong b = Bits;
        while (b != 0)
        {
            int idx = BitOperations.TrailingZeroCount(b);
            yield return idx + 1;
            b &= b - 1;
        }
    }

    public override string ToString() =>
        string.Join(" - ", Dezenas().Select(d => d.ToString("00")));
}