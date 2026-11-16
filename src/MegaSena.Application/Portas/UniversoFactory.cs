
using MegaSena.Domain;

namespace MegaSena.Application.Portas;

public interface IUniversoFactory
{
    Task<Universo> CriarAsync(int ultimos, int menos, CancellationToken ct = default);
}

public sealed class UniversoFactoryPadrao : IUniversoFactory
{
    private readonly IHistoricoRepository _historico;

    public UniversoFactoryPadrao(IHistoricoRepository historico)
    {
        _historico = historico;
    }

    public async Task<Universo> CriarAsync(int ultimos, int menos, CancellationToken ct = default)
    {
        var excluidas = new HashSet<int>();

        // Sequencial de propósito: os dois repositórios usam o mesmo DbContext, que não aceita uso paralelo.
        foreach (var sorteio in await _historico.ObterUltimosSorteiosAsync(ultimos, ct))
            foreach (var d in sorteio)
                excluidas.Add(d);

        foreach (var d in await _historico.ObterMenosSorteadasAsync(menos, ct))
            excluidas.Add(d);

        return Universo.Sem(excluidas);
    }
}
