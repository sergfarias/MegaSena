using MegaSena.Domain;
using MegaSena.Application.Portas;

namespace MegaSena.Application.CasosDeUso;

public sealed class GerarJogoUseCase
{
    private readonly IGeradorDeSorteioFactory _geradorFactory;
    private readonly IUniversoFactory _universoFactory;

    public GerarJogoUseCase(
        IGeradorDeSorteioFactory geradorFactory,
        IUniversoFactory universoFactory)
    {
        _geradorFactory = geradorFactory;
        _universoFactory = universoFactory;
    }

    // AQUI ESTÁ A MUDANÇA: Agora o método recebe 4 parâmetros.
    public ResultadoSorteio Executar(int ultimos, int menos, bool fisico, int maxPares)
    {
        var universo = _universoFactory.Criar(ultimos, menos);

        // A Factory decide se usa o Físico ou o Rápido
        var gerador = _geradorFactory.Criar(fisico);

        // A regra agora é criada aqui dentro
        var regra = new MaximoParesConsecutivos(maxPares);

        var jogo = gerador.Sortear(universo, regra);
        return new ResultadoSorteio(jogo, universo, fisico);
    }
}


//using MegaSena.Domain;
//using MegaSena.Application.Portas;

//namespace MegaSena.Application.CasosDeUso;

//public sealed class GerarJogoUseCase
//{
//    private readonly IGeradorDeSorteio _gerador;
//    private readonly IUniversoFactory _universoFactory;
//    //private readonly IRegraDeJogo _regra;

//    public GerarJogoUseCase(IGeradorDeSorteio gerador, IUniversoFactory universoFactory, IRegraDeJogo regra)
//    {
//        _gerador = gerador;
//        _universoFactory = universoFactory;
//        //_regra = regra;
//    }

//    public ResultadoSorteio Executar(int ultimos, int menos)
//    {
//        var universo = _universoFactory.Criar(ultimos, menos);
//        var jogo = _gerador.Sortear(universo, _regra);
//        return new ResultadoSorteio(jogo, universo, _gerador.Nome == "Físico");
//    }
//}