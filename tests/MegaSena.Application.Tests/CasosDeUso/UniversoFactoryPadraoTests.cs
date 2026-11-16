using MegaSena.Application.Portas;
using Xunit;

namespace MegaSena.Application.Tests;

/// <summary>Copiar para tests\MegaSena.Application.Tests\CasosDeUso\ (ou para a raiz do projeto).</summary>
public class UniversoFactoryPadraoTests
{
    private sealed class HistoricoFalso : IHistoricoRepository
    {
        public Task<IReadOnlyList<int[]>> ObterUltimosSorteiosAsync(int quantidade, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<int[]>>(quantidade <= 0
                ? Array.Empty<int[]>()
                : new[] { new[] { 1, 2, 3, 4, 5, 6 } });

        public Task<IReadOnlyList<int>> ObterMenosSorteadasAsync(int quantidade, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<int>>(quantidade <= 0
                ? Array.Empty<int>()
                : new[] { 6, 7, 8 });   // o 6 repete: não pode ser contado duas vezes
    }

    [Fact]
    public async Task CriarAsync_ExcluiUltimosEMenosSorteadas_SemContarRepetidasDuasVezes()
    {
        var universo = await new UniversoFactoryPadrao(new HistoricoFalso()).CriarAsync(ultimos: 1, menos: 3);

        // excluídas: 1..6 e 7, 8  => 8 dezenas fora
        Assert.Equal(52, universo.Tamanho);
        Assert.False(universo.Contem(1));
        Assert.False(universo.Contem(8));
        Assert.True(universo.Contem(9));
    }

    [Fact]
    public async Task CriarAsync_SemFiltros_DevolveAsSessentaDezenas()
    {
        var universo = await new UniversoFactoryPadrao(new HistoricoFalso()).CriarAsync(ultimos: 0, menos: 0);

        Assert.Equal(60, universo.Tamanho);
    }
}
