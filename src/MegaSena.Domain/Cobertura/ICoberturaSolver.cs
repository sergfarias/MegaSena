namespace MegaSena.Domain.Cobertura;

public interface ICoberturaSolver
{
    Cobertura Resolver(
        IReadOnlySet<int> dezenas,
        int garantia,
        int universoSorteado,
        int maxPares,
        CancellationToken ct = default);
}