using MegaSena.Application.CasosDeUso;
using MegaSena.Infrastructure.Cobertura;
using Xunit;

namespace MegaSena.Infrastructure.Tests;

public class MinimoProvadoTests
{
    private static GerarCoberturaUseCase NovoUseCase() =>
        new(new SolverRoteador(new GulosoCoberturaSolver(), new SolverExato()));

    [Fact]
    public void Sena_DeveMarcarMinimoComoProvado()
    {
        var dezenas = new HashSet<int> { 2, 5, 9, 14, 20, 31, 44 };

        var r = NovoUseCase().Executar(dezenas, garantia: 6, universoSorteado: 6, maxPares: 1);

        Assert.True(r.GarantiaTotal);
        Assert.True(r.MinimoProvado);
    }

    [Fact]
    public void OitoDezenas_ExatoConclui_DeveMarcarMinimoComoProvado()
    {
        var dezenas = new HashSet<int> { 1, 4, 8, 13, 22, 30, 41, 55 };

        var r = NovoUseCase().Executar(dezenas, garantia: 4, universoSorteado: 4, maxPares: 2);

        Assert.True(r.GarantiaTotal);
        Assert.True(r.MinimoProvado);
    }

    [Fact]
    public void DozeDezenas_SoGuloso_NaoDeveMarcarMinimoComoProvado()
    {
        var dezenas = new HashSet<int> { 1, 4, 8, 13, 17, 22, 26, 30, 35, 41, 48, 55 };

        var r = NovoUseCase().Executar(dezenas, garantia: 4, universoSorteado: 4, maxPares: 5);

        Assert.True(r.GarantiaTotal);
        Assert.False(r.MinimoProvado);
    }
}
