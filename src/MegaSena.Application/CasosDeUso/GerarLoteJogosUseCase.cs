
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

    public async Task<IEnumerable<Jogo>> ExecutarAsync(
        int quantidade, int ultimos, int menos, int maxPares, bool fisico = false, CancellationToken ct = default)
    {
        var universo = await _universoFactory.CriarAsync(ultimos, menos, ct);
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
