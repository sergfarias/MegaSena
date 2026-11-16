
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

    public async Task<ResultadoSorteio> ExecutarAsync(
        int ultimos, int menos, bool fisico, int maxPares, CancellationToken ct = default)
    {
        var universo = await _universoFactory.CriarAsync(ultimos, menos, ct);

        // A Factory decide se usa o Físico ou o Rápido
        var gerador = _geradorFactory.Criar(fisico);

        // A regra agora é criada aqui dentro
        var regra = new MaximoParesConsecutivos(maxPares);

        var jogo = gerador.Sortear(universo, regra);
        return new ResultadoSorteio(jogo, universo, fisico);
    }
}
