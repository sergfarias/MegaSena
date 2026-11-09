using MegaSena.Application.CasosDeUso;
using Xunit;

namespace MegaSena.Application.Tests.CasosDeUso;

public class CalcularProbabilidadeUseCaseTests
{
    [Theory]
    [InlineData(5)]
    [InlineData(21)]
    public void Executar_ComDezenasInvalidas_DeveLancarExcecao(int dezenas)
    {
        var useCase = new CalcularProbabilidadeUseCase();
        Assert.Throws<ArgumentOutOfRangeException>(() => useCase.Executar(dezenas));
    }

    [Fact]
    public void Executar_ComApostaSimples_DeveRetornarProbabilidadesCorretas()
    {
        var useCase = new CalcularProbabilidadeUseCase();
        // Passa a taxaRetorno!
        var resultado = useCase.Executar(6, taxaRetorno: 0.45m, concursosAmostrados: 100);

        Assert.Equal(6, resultado.Dezenas);
        Assert.Equal(6.00m, resultado.Custo);
        Assert.Equal(1.0 / 50_063_860, resultado.ProbSena, 10);
        Assert.True(resultado.ProbPeloMenosQuadra > 0);
        Assert.NotNull(resultado.RetornoEsperado);
        Assert.True(resultado.RetornoEsperado > 0);
    }

    [Fact]
    public void Executar_SemTaxa_DeveRetornarRetornoNulo()
    {
        var useCase = new CalcularProbabilidadeUseCase();
        var resultado = useCase.Executar(6);

        Assert.Null(resultado.RetornoEsperado);
        Assert.Equal("indisponível", resultado.Fonte);
    }
}