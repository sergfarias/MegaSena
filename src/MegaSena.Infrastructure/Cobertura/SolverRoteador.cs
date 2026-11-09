using MegaSena.Domain;
using MegaSena.Domain.Cobertura;

namespace MegaSena.Infrastructure.Cobertura;

public sealed class SolverRoteador : ICoberturaSolver
{
    private readonly GulosoCoberturaSolver _guloso;
    private readonly SolverExato _exato;

    public SolverRoteador(GulosoCoberturaSolver guloso, SolverExato exato)
    {
        _guloso = guloso;
        _exato = exato;
    }

    public MegaSena.Domain.Cobertura.Cobertura Resolver(
        IReadOnlySet<int> dezenas,
        int garantia,
        int universoSorteado,
        int maxPares,
        CancellationToken ct = default)
    {
        // 1. Sena (6/6): todo alvo é um jogo. Sem guloso, sem busca.
        if (garantia == 6 && universoSorteado == 6)
            return TodosOsJogosValidos(dezenas, garantia, universoSorteado, maxPares, ct);

        // 2. Limite superior via guloso
        var ub = _guloso.Resolver(dezenas, garantia, universoSorteado, maxPares, ct);

        // 3. Acima de 9 dezenas (ou solução trivial) não tenta o exato
        if (dezenas.Count > 9 || ub.Jogos.Count <= 1)
            return ub;

        // 4. Exato com orçamento próprio de 4 s; se estourar, devolve o guloso
        using var orcamento = CancellationTokenSource.CreateLinkedTokenSource(ct);
        orcamento.CancelAfter(TimeSpan.FromSeconds(4));

        try
        {
            return _exato.Resolver(dezenas, garantia, universoSorteado, maxPares, ub, orcamento.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return ub;
        }
    }

    private static MegaSena.Domain.Cobertura.Cobertura TodosOsJogosValidos(
        IReadOnlySet<int> dezenas, int garantia, int universoSorteado, int maxPares, CancellationToken ct)
    {
        var arr = dezenas.OrderBy(x => x).ToArray();
        var jogos = new List<Jogo>();
        long iter = 0;

        foreach (var c in Combinatoria.Combinacoes(arr, 6))
        {
            if ((++iter & 1023) == 0) ct.ThrowIfCancellationRequested();
            var jogo = Jogo.DasDezenas(c);
            if (jogo.ParesConsecutivos() <= maxPares)
                jogos.Add(jogo);
        }

        return new MegaSena.Domain.Cobertura.Cobertura(dezenas, 6, garantia, universoSorteado, jogos);
    }
}
