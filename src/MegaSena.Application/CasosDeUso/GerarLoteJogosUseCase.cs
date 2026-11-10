using MegaSena.Application.Portas;
using MegaSena.Domain;

namespace MegaSena.Application.CasosDeUso;

public sealed class GerarLoteJogosUseCase
{
    private readonly IGeradorDeSorteioFactory _geradorFactory;
    private readonly IUniversoFactory _universoFactory;

    public GerarLoteJogosUseCase(
        IGeradorDeSorteioFactory geradorFactory,
        IUniversoFactory universoFactory)
    {
        _geradorFactory = geradorFactory;
        _universoFactory = universoFactory;
    }

    public IEnumerable<Jogo> Executar(int quantidade, int ultimos, int menos, int maxPares, bool fisico = false)
    {
        var universo = _universoFactory.Criar(ultimos, menos);
        var gerador = _geradorFactory.Criar(fisico);
        var regra = new MaximoParesConsecutivos(maxPares);

        var jogosGerados = new HashSet<Jogo>();
        int tentativas = 0;
        int maxTentativas = quantidade * 100;

        while (jogosGerados.Count < quantidade && tentativas < maxTentativas)
        {
            var jogo = gerador.Sortear(universo, regra);
            jogosGerados.Add(jogo);
            tentativas++;
        }

        if (jogosGerados.Count < quantidade)
            throw new InvalidOperationException($"Não foi possível gerar {quantidade} jogos únicos. Só consegui {jogosGerados.Count}.");

        return jogosGerados;
    }
}


//using MegaSena.Application.Portas;
//using MegaSena.Domain;

//namespace MegaSena.Application.CasosDeUso;

//public sealed class GerarLoteJogosUseCase
//{
//    private readonly IGeradorDeSorteio _gerador;
//    private readonly IUniversoFactory _universoFactory;
//    private readonly IRegraDeJogo _regra;

//    public GerarLoteJogosUseCase(IGeradorDeSorteio gerador, IUniversoFactory universoFactory, IRegraDeJogo regra)
//    {
//        _gerador = gerador;
//        _universoFactory = universoFactory;
//        _regra = regra;
//    }

//    public IEnumerable<Jogo> Executar(int quantidade, int ultimos, int menos)
//    {
//        var universo = _universoFactory.Criar(ultimos, menos);
//        var jogosGerados = new HashSet<Jogo>();

//        int tentativas = 0;
//        int maxTentativas = quantidade * 100;

//        while (jogosGerados.Count < quantidade && tentativas < maxTentativas)
//        {
//            // Passamos o universo e a regra para o gerador (conforme a sua interface)
//            var jogo = _gerador.Sortear(universo, _regra);

//            // Como o gerador já aplica a regra internamente, só precisamos adicionar
//            jogosGerados.Add(jogo);

//            tentativas++;
//        }

//        if (jogosGerados.Count < quantidade)
//            throw new InvalidOperationException($"Não foi possível gerar {quantidade} jogos únicos. Só consegui {jogosGerados.Count}.");

//        return jogosGerados;
//    }
//}