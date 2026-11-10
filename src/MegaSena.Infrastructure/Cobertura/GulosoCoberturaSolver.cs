using System.Numerics;
using MegaSena.Domain;
using MegaSena.Domain.Cobertura;

namespace MegaSena.Infrastructure.Cobertura;

public sealed class GulosoCoberturaSolver : ICoberturaSolver
{
    public MegaSena.Domain.Cobertura.Cobertura Resolver(
        IReadOnlySet<int> dezenas,
        int garantia,
        int universoSorteado,
        int maxPares,
        CancellationToken ct = default)
    {
        var arr = dezenas.OrderBy(x => x).ToArray();

        // 1. Gera os alvos e candidatos
        var alvos = Combinatoria.Combinacoes(arr, universoSorteado).Select(Mascara).ToArray();
        var candidatos = Combinatoria.Combinacoes(arr, 6)
            .Select(c => new { Valores = c, Jogo = Jogo.DasDezenas(c) })
            .Where(x => x.Jogo.ParesConsecutivos() <= maxPares)
            .Select(x => Mascara(x.Valores))
            .ToArray();

        if (candidatos.Length == 0)
            return new MegaSena.Domain.Cobertura.Cobertura(dezenas, 6, garantia, universoSorteado, new List<Jogo>());

        int palavras = (alvos.Length + 63) / 64;
        var coberturaPorCandidato = new ulong[candidatos.Length][];

        var parallelOptions = new ParallelOptions
        {
            CancellationToken = ct
        };

        try
        {
            // Pre-calcula a cobertura de cada candidato (Paralelizado e Cancelável)
            Parallel.For(0, candidatos.Length, parallelOptions, i =>
            {
                var bits = new ulong[palavras];
                for (int j = 0; j < alvos.Length; j++)
                {
                    if (BitOperations.PopCount(candidatos[i] & alvos[j]) >= garantia)
                        bits[j >> 6] |= 1UL << (j & 63);
                }
                coberturaPorCandidato[i] = bits;
            });
        }
        catch (OperationCanceledException)
        {
            ct.ThrowIfCancellationRequested();
        }

        var descobertos = new ulong[palavras];
        for (int j = 0; j < alvos.Length; j++)
            descobertos[j >> 6] |= 1UL << (j & 63);

        var escolhidos = new List<int>();
        int totalDescobertos = alvos.Length;

        var filaPrioridade = new PriorityQueue<int, int>();
        var ganhos = new int[candidatos.Length];

        for (int i = 0; i < candidatos.Length; i++)
        {
            ganhos[i] = ContarDescobertos(coberturaPorCandidato[i], descobertos, palavras);
            filaPrioridade.Enqueue(i, -ganhos[i]);
        }

        while (totalDescobertos > 0 && filaPrioridade.Count > 0)
        {
            ct.ThrowIfCancellationRequested();

            filaPrioridade.TryDequeue(out int melhorCand, out int prioridadeNeg);
            int ganhoAntigo = -prioridadeNeg;

            int ganhoAtual = ContarDescobertos(coberturaPorCandidato[melhorCand], descobertos, palavras);

            if (ganhoAtual == 0) continue;

            if (ganhoAtual < ganhoAntigo)
            {
                filaPrioridade.Enqueue(melhorCand, -ganhoAtual);
                continue;
            }

            escolhidos.Add(melhorCand);
            var bitsCand = coberturaPorCandidato[melhorCand];
            for (int w = 0; w < palavras; w++)
            {
                ulong antes = descobertos[w];
                descobertos[w] &= ~bitsCand[w];
                totalDescobertos -= BitOperations.PopCount(antes & ~descobertos[w]);
            }
        }

        // =========================================================
        // Remoção de Redundantes Otimizada (TrailingZeroCount)
        // =========================================================
        var contagemAlvos = new int[alvos.Length];
        foreach (var idx in escolhidos)
        {
            var bits = coberturaPorCandidato[idx];
            for (int w = 0; w < palavras; w++)
            {
                ulong m = bits[w];
                while (m != 0)
                {
                    int bit = BitOperations.TrailingZeroCount(m);
                    int index = (w * 64) + bit;
                    if (index < contagemAlvos.Length) // GUARDA
                        contagemAlvos[index]++;
                    m &= m - 1;
                }
            }
        }

        for (int k = escolhidos.Count - 1; k >= 0; k--)
        {
            int idxCand = escolhidos[k];
            var bits = coberturaPorCandidato[idxCand];
            bool podeRemover = true;

            for (int w = 0; w < palavras; w++)
            {
                ulong m = bits[w];
                while (m != 0)
                {
                    int bit = BitOperations.TrailingZeroCount(m);
                    int index = (w * 64) + bit;
                    if (index < contagemAlvos.Length && contagemAlvos[index] < 2) // GUARDA
                    {
                        podeRemover = false;
                        break;
                    }
                    m &= m - 1;
                }
                if (!podeRemover) break;
            }

            if (podeRemover)
            {
                for (int w = 0; w < palavras; w++)
                {
                    ulong m = bits[w];
                    while (m != 0)
                    {
                        int bit = BitOperations.TrailingZeroCount(m);
                        int index = (w * 64) + bit;
                        if (index < contagemAlvos.Length) // GUARDA
                            contagemAlvos[index]--;
                        m &= m - 1;
                    }
                }
                escolhidos.RemoveAt(k);
            }
        }

        var jogos = escolhidos.Select(i => JogoDeMascara(candidatos[i])).ToList();
        return new MegaSena.Domain.Cobertura.Cobertura(dezenas, 6, garantia, universoSorteado, jogos);
    }

    private static int ContarDescobertos(ulong[] bitsCand, ulong[] descobertos, int palavras)
    {
        int ganho = 0;
        for (int w = 0; w < palavras; w++)
            ganho += BitOperations.PopCount(bitsCand[w] & descobertos[w]);
        return ganho;
    }

    private static ulong Mascara(IEnumerable<int> ds)
        => ds.Aggregate(0UL, (m, d) => m | (1UL << (d - 1)));

    private static Jogo JogoDeMascara(ulong mascara)
    {
        var ds = new List<int>();
        ulong m = mascara;
        while (m != 0)
        {
            int bit = BitOperations.TrailingZeroCount(m);
            ds.Add(bit + 1);
            m &= m - 1;
        }
        return Jogo.DasDezenas(ds);
    }
}