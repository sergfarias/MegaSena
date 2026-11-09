using MegaSena.Application.CasosDeUso;
using MegaSena.Application.Portas;
using MegaSena.Domain;
using Xunit;

namespace MegaSena.Application.Tests.CasosDeUso;

// Mock para simular um gerador
public class GeradorMock : IGeradorDeSorteio
{
    public string Nome => "Mock";
    private readonly Func<Jogo> _geradorLogica;

    public GeradorMock(Func<Jogo> geradorLogica) => _geradorLogica = geradorLogica;

    public Jogo Sortear(Universo universo, IRegraDeJogo regra) => _geradorLogica();
}

public class TestarUniformidadeUseCaseTests
{
    [Fact]
    public void Executar_ComGeradorViciado_DeveReprovar()
    {
        // Arrange: gerador que sempre devolve o mesmo jogo (viciado)
        var geradorViciado = new GeradorMock(() => Jogo.DasDezenas(new[] { 1, 2, 3, 4, 5, 6 }));
        var useCase = new TestarUniformidadeUseCase();
        var universo = Universo.Completo();

        // Act
        var r = useCase.Executar(geradorViciado, universo, 100);

        // Assert: p-valor ínfimo indica viés
        Assert.False(r.SemEvidenciaDeVies, "O teste de uniformidade deveria reprovar um gerador viciado.");
        Assert.True(r.PValor < 0.001);
    }

    

    [Theory]
    [InlineData(77.93, 59, 0.05)]
    [InlineData(52.19, 37, 0.05)]
    [InlineData(56.9, 59, 0.5533)]
    public void PValor_DeveBaterComValoresConhecidos(double chi2, int gl, double esperado)
    {
        var p = MegaSena.Domain.Calculadora.DistribuicaoQuiQuadrado.PValor(chi2, gl);
        Assert.Equal(esperado, p, precision: 3);
    }

}

