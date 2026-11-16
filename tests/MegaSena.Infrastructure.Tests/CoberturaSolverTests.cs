using MegaSena.Application.CasosDeUso;
using MegaSena.Domain;
using MegaSena.Domain.Cobertura;
using MegaSena.Infrastructure.Cobertura;
using System.Diagnostics;
using Xunit; 

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
        var dezenas = new HashSet<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        var jogosFake = new List<Jogo>();
        for (int i = 0; i < 12; i++)
        {
            var d1 = (i % 10) + 1;
            var d2 = (i % 10) + 11;
            var d3 = (i % 10) + 21;
            var d4 = (i % 10) + 31;
            var d5 = (i % 10) + 41;
            var d6 = (i % 10) + 51;
            jogosFake.Add(Jogo.DasDezenas(new[] { d1, d2, d3, d4, d5, d6 }));
        }

        var ubFake = new MegaSena.Domain.Cobertura.Cobertura(dezenas, 6, 5, 5, jogosFake);
        var solver = new SolverExato();
        var resultado = solver.Resolver(dezenas, garantia: 5, universoSorteado: 5, maxPares: 2, ub: ubFake);

        Assert.NotNull(resultado);
        Assert.True(resultado.Jogos.Count <= ubFake.Jogos.Count);
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

    [Fact]
    public void Roteador_ComOrcamentoZero_DeveCairNoGulosoComMinimoNaoProvado()
    {
        // Arrange
        var dezenas = new HashSet<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        var guloso = new GulosoCoberturaSolver();
        var exato = new SolverExato();

        // Passamos TimeSpan.Zero para forçar o timeout imediato no exato
        var roteador = new SolverRoteador(guloso, exato, TimeSpan.Zero);

        // Act
        var resultado = roteador.Resolver(dezenas, garantia: 4, universoSorteado: 4, maxPares: 5);

        // Assert
        Assert.NotNull(resultado);
        Assert.NotEmpty(resultado.Jogos);
        Assert.False(resultado.MinimoProvado);
    }

    [Fact]
    public void Roteador_OrcamentoMinimoComCasoPesado_DeveCairNoGulosoComMinimoNaoProvado()
    {
        // Caso pesado: 9 dezenas, garantia 4, universo 6.
        var dezenas = new HashSet<int> { 3, 9, 14, 21, 27, 33, 41, 52, 58 };
        var guloso = new GulosoCoberturaSolver();

        // Usamos TimeSpan.Zero para garantir o estouro determinístico
        var roteador = new SolverRoteador(guloso, new SolverExato(), TimeSpan.Zero);

        var sw = Stopwatch.StartNew();
        var r = roteador.Resolver(dezenas, garantia: 4, universoSorteado: 6, maxPares: 5);
        sw.Stop();

        Assert.NotEmpty(r.Jogos);
        Assert.False(r.MinimoProvado, "Com orçamento zero, o exato não deve concluir.");
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(10),
            $"Demorou {sw.Elapsed.TotalSeconds:F1}s — o orçamento não está sendo respeitado.");
    }

    [Fact]
    public void Roteador_OrcamentoGeneroso_DeveProvarMinimo()
    {
        // Caso pequeno: 7 dezenas, garantia 6, universo 6 → o exato conclui trivialmente.
        var dezenas = new HashSet<int> { 2, 5, 9, 14, 20, 31, 44 };
        var roteador = new SolverRoteador(
            new GulosoCoberturaSolver(),
            new SolverExato(),
            TimeSpan.FromSeconds(30));

        var r = roteador.Resolver(dezenas, garantia: 6, universoSorteado: 6, maxPares: 1);

        Assert.NotEmpty(r.Jogos);
        Assert.True(r.MinimoProvado, "Com 30s de orçamento e caso trivial, o mínimo deve ser provado.");
        Assert.Equal(7, r.Jogos.Count);
    }

    [Fact]
    public void UseCase_OrcamentoMinimoComCasoPesado_DeveRetornarGarantiaTotal()
    {
        var dezenas = new HashSet<int> { 3, 9, 14, 21, 27, 33, 41, 52, 58 };
        var guloso = new GulosoCoberturaSolver();

        // Orçamento Zero para forçar o fallback
        var roteador = new SolverRoteador(guloso, new SolverExato(), TimeSpan.Zero);

        var useCase = new GerarCoberturaUseCase(roteador);

        var resultado = useCase.Executar(dezenas, garantia: 4, universoSorteado: 6, maxPares: 5);

        Assert.True(resultado.GarantiaTotal);
        Assert.False(resultado.MinimoProvado);
    }
}