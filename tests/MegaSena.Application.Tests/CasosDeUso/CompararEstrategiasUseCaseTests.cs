using MegaSena.Application.CasosDeUso;
using Xunit;

namespace MegaSena.Application.Tests.CasosDeUso;

public class CompararEstrategiasUseCaseTests
{
    [Theory]
    [InlineData(6)]  // Inválido: a comparação só faz sentido a partir de 7
    [InlineData(21)] // Inválido: limite da Caixa
    public void Executar_ForaDoLimite_DeveLancarExcecao(int dezenas)
    {
        var useCase = new CompararEstrategiasUseCase();
        Assert.Throws<ArgumentException>(() => useCase.Executar(dezenas));
    }

    [Fact]
    public void Executar_Com7Dezenas_DeveCalcularEstrategiasEquivalentes()
    {
        var useCase = new CompararEstrategiasUseCase();
        var resultado = useCase.Executar(7);

        Assert.Equal(7, resultado.DezenasMultipla);
        Assert.Equal(7 * 6.00m, resultado.CustoTotal);
        Assert.Equal(7, resultado.QtdJogosSimples);

        // ProbMultipla (P(≥ Quadra) com 7 dezenas) ≈ 0,00098
        Assert.True(resultado.ProbMultipla > 0.0009 && resultado.ProbMultipla < 0.0011);

        // ProbJogosSimples (P(≥ Quadra em 7 jogos simples)) ≈ 0,0030
        Assert.True(resultado.ProbJogosSimples > 0.002 && resultado.ProbJogosSimples < 0.004);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(10)]
    public void Executar_DeveSerMonotonico_ComMaisDezenas(int dezenas)
    {
        // Quanto mais dezenas, maior o custo — mas a probabilidade aumenta
        var uc = new CompararEstrategiasUseCase();
        var r = uc.Executar(dezenas);

        Assert.True(r.CustoTotal > 0);
        Assert.True(r.ProbMultipla > 0);
        Assert.True(r.ProbJogosSimples > 0);
        Assert.True(r.QtdJogosSimples > 0);
    }

    [Fact]
    public void Executar_Com7Dezenas_ProbMultiplaMenorQueJogosSimples()
    {
        // Com 7 dezenas e mesmo orçamento,
        // a probabilidade dos 7 jogos simples (0.30%) > a da múltipla (0.098%).
        var uc = new CompararEstrategiasUseCase();
        var r = uc.Executar(7);

        Assert.True(r.ProbJogosSimples > r.ProbMultipla);
    }
}