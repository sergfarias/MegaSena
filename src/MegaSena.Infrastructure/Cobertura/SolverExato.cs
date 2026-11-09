using MegaSena.Domain;
using System.Numerics;

namespace MegaSena.Infrastructure.Cobertura;

public sealed class SolverExato
{
    public MegaSena.Domain.Cobertura.Cobertura Resolver(
        IReadOnlySet<int> dezenas,
        int garantia,
        int universoSorteado,
        int maxPares,
        MegaSena.Domain.Cobertura.Cobertura? ub = null,
        CancellationToken ct = default)
    {
        // Fallback: se não passarem ub, usa o guloso
        if (ub is null)
        {
            var guloso = new GulosoCoberturaSolver();
            ub = guloso.Resolver(dezenas, garantia, universoSorteado, maxPares, ct);
        }

        if (ub.Jogos.Count <= 1) return ub;

        var arr = dezenas.OrderBy(x => x).ToArray();

        // 1. Gera Alvos e Candidatos convertidos para bitmasks (ulong)
        var alvos = Combinatoria.Combinacoes(arr, universoSorteado).Select(Mascara).ToArray();
        var candidatos = Combinatoria.Combinacoes(arr, 6)
            .Select(c => new { Valores = c, Jogo = Jogo.DasDezenas(c) })
            .Where(x => x.Jogo.ParesConsecutivos() <= maxPares)
            .Select(x => Mascara(x.Valores))
            .ToArray();

        if (candidatos.Length == 0) return ub;

        // 2. Pre-calcula quem cobre quem
        // bits[c][a] = true se o candidato c cobre o alvo a
        int palavrasAlvos = (alvos.Length + 63) / 64;
        var bits = new ulong[candidatos.Length][];

        for (int c = 0; c < candidatos.Length; c++)
        {
            bits[c] = new ulong[palavrasAlvos];
            for (int a = 0; a < alvos.Length; a++)
            {
                if (BitOperations.PopCount(candidatos[c] & alvos[a]) >= garantia)
                    bits[c][a >> 6] |= 1UL << (a & 63);
            }
        }

        // 3. Monta a lista de candidatos por alvo (para escolher o alvo mais restrito)
        var cobreAlvo = new List<int>[alvos.Length];
        for (int a = 0; a < alvos.Length; a++)
        {
            cobreAlvo[a] = new List<int>();
            for (int c = 0; c < candidatos.Length; c++)
            {
                if ((bits[c][a >> 6] & (1UL << (a & 63))) != 0)
                    cobreAlvo[a].Add(c);
            }
        }

        // 4. Estado inicial: todos os alvos descobertos
        var alvosDescobertos = new ulong[palavrasAlvos];
        for (int a = 0; a < alvos.Length; a++)
            alvosDescobertos[a >> 6] |= 1UL << (a & 63);

        var escolhaAtual = new List<int>();

        // 5. Iterative Deepening: Tenta achar solução com 1, 2, 3... jogos até o tamanho do ub - 1
        int limite = ub.Jogos.Count;
        for (int k = 1; k < limite; k++)
        {
            ct.ThrowIfCancellationRequested();

            if (Busca(alvosDescobertos, k, escolhaAtual, bits, cobreAlvo, alvos.Length, ct))
            {
                var jogos = escolhaAtual.Select(i => JogoDeMascara(candidatos[i])).ToList();
                return new MegaSena.Domain.Cobertura.Cobertura(dezenas, 6, garantia, universoSorteado, jogos);
            }
        }

        // Se não achou nada menor que o ub, devolve o ub
        return ub;
    }

    private static bool Busca(
        ulong[] descobertos,
        int restante,
        List<int> escolha,
        ulong[][] bits,
        List<int>[] cobreAlvo,
        int totalAlvos,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Encontra o alvo descoberto com menos candidatos
        int alvoRestrito = -1;
        int menorQtdCandidatos = int.MaxValue;

        for (int a = 0; a < totalAlvos; a++)
        {
            if ((descobertos[a >> 6] & (1UL << (a & 63))) != 0)
            {
                int qtd = cobreAlvo[a].Count;
                if (qtd < menorQtdCandidatos)
                {
                    menorQtdCandidatos = qtd;
                    alvoRestrito = a;
                    if (qtd == 0) return false; // Alvo impossível de cobrir
                }
            }
        }

        // Se não achou nenhum alvo descoberto, a cobertura está completa
        if (alvoRestrito == -1) return true;

        // Se não temos mais jogos para escolher e ainda faltam alvos, falhou
        if (restante == 0) return false;

        // Tenta cada candidato que cobre o alvo mais restrito
        foreach (int c in cobreAlvo[alvoRestrito])
        {
            // Atualiza o estado (marca os alvos que esse candidato cobre como cobertos)
            var proxDescobertos = new ulong[descobertos.Length];
            for (int w = 0; w < descobertos.Length; w++)
                proxDescobertos[w] = descobertos[w] & ~bits[c][w];

            escolha.Add(c);

            if (Busca(proxDescobertos, restante - 1, escolha, bits, cobreAlvo, totalAlvos, ct))
                return true;

            escolha.RemoveAt(escolha.Count - 1);
        }

        return false;
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