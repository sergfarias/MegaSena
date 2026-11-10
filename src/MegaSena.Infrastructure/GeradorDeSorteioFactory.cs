using MegaSena.Application.Portas;

namespace MegaSena.Infrastructure;

public sealed class GeradorDeSorteioFactory : IGeradorDeSorteioFactory
{
    private readonly GeradorAleatorioRapido _rapido;
    private readonly SimuladorFisicoGerador _fisico;

    public GeradorDeSorteioFactory(
        GeradorAleatorioRapido rapido,
        SimuladorFisicoGerador fisico)
    {
        _rapido = rapido;
        _fisico = fisico;
    }

    public IGeradorDeSorteio Criar(bool fisico) => fisico ? _fisico : _rapido;
}