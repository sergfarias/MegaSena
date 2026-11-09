using MegaSena.Domain;

namespace MegaSena.Application.Portas;

public interface IUniversoFactory
{
    Universo Criar(int ultimos, int menos);
}

public sealed class UniversoFactoryPadrao : IUniversoFactory
{
    private readonly IHistoricoRepository _historico;

    public UniversoFactoryPadrao(IHistoricoRepository historico)
    {
        _historico = historico;
    }

    public Universo Criar(int ultimos, int menos)
    {
        var excluidas = new HashSet<int>();

        foreach (var sorteio in _historico.ObterUltimosSorteios(ultimos))
            foreach (var d in sorteio)
                excluidas.Add(d);

        foreach (var d in _historico.ObterMenosSorteadas(menos))
            excluidas.Add(d);

        return Universo.Sem(excluidas);
    }
}