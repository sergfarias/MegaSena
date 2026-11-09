using MegaSena.Domain;
using MegaSena.Application.Portas;

namespace MegaSena.Application.CasosDeUso;

public sealed class GerarJogoUseCase
{
    private readonly IGeradorDeSorteio _gerador;
    private readonly IUniversoFactory _universoFactory;
    private readonly IRegraDeJogo _regra;

    public GerarJogoUseCase(IGeradorDeSorteio gerador, IUniversoFactory universoFactory, IRegraDeJogo regra)
    {
        _gerador = gerador;
        _universoFactory = universoFactory;
        _regra = regra;
    }

    public ResultadoSorteio Executar(int ultimos, int menos)
    {
        var universo = _universoFactory.Criar(ultimos, menos);
        var jogo = _gerador.Sortear(universo, _regra);
        return new ResultadoSorteio(jogo, universo, _gerador.Nome == "Físico");
    }
}