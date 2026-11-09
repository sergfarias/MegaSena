using MegaSena.Domain;
using MegaSena.Infrastructure.Cobertura;

namespace MegaSena.Infrastructure.Tests;

public class CoberturaSolverTests
{
    [Fact]
    public void SolverExato_DeveAcharSolucaoParaCasoSimples()
    {
        var dezenas = new HashSet<int> { 1, 2, 3, 4, 5, 6 };
        var solver = new SolverExato();
        var resultado = solver.Resolver(dezenas, garantia: 6, universoSorteado: 6, maxPares: 5);
        Assert.NotNull(resultado);
        Assert.Single(resultado.Jogos);
    }

    [Fact]
    public void SolverExato_ComUbFornecido_DeveRespeitarLimite()
    {
        // Se o guloso já achou N jogos, o exato não pode devolver MAIS que N.
        var dezenas = new HashSet<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        // Guloso "fake" com 12 jogos válidos
        var jogosFake = new List<Jogo>();
        for (int i = 0; i < 12; i++)
        {
            // Gera jogos válidos sem estourar o limite de 60
            var d1 = (i % 10) + 1;
            var d2 = (i % 10) + 11;
            var d3 = (i % 10) + 21;
            var d4 = (i % 10) + 31;
            var d5 = (i % 10) + 41;
            var d6 = (i % 10) + 51;
            jogosFake.Add(Jogo.DasDezenas(new[] { d1, d2, d3, d4, d5, d6 }));
        }

        var ubFake = new MegaSena.Domain.Cobertura.Cobertura(
            dezenas, 6, 5, 5, jogosFake);

        var solver = new SolverExato();
        var resultado = solver.Resolver(dezenas, garantia: 5, universoSorteado: 5, maxPares: 2, ub: ubFake);

        Assert.NotNull(resultado);
        Assert.True(resultado.Jogos.Count <= ubFake.Jogos.Count,
            "O SolverExato não pode devolver mais jogos que o ub.");
    }

    [Fact]
    public void Guloso_DeveAcharUmaSolucao()
    {
        var dezenas = new HashSet<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        var guloso = new GulosoCoberturaSolver();
        var resultado = guloso.Resolver(dezenas, garantia: 5, universoSorteado: 5, maxPares: 2);
        Assert.NotNull(resultado);
        Assert.True(resultado.Jogos.Count > 0);
    }
}