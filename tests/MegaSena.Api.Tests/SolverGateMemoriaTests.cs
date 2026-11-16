
using MegaSena.Api.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace MegaSena.Api.Tests;

public class SolverGateMemoriaTests
{
    private static SolverGate Criar(int maxConcorrentes, int memoriaMb) =>
        new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Solver:MaxConcorrentes"] = maxConcorrentes.ToString(),
            ["Solver:MemoriaMaximaMb"] = memoriaMb.ToString()
        }).Build());

    private const long Mb = 1024 * 1024;

    [Fact]
    public void TryEnter_ComOrcamento_RecusaQuandoSomaPassaDoLimite()
    {
        using var gate = Criar(maxConcorrentes: 4, memoriaMb: 100);

        var a = gate.TryEnter(60 * Mb);
        var b = gate.TryEnter(60 * Mb);   // 120 > 100: recusa
        var c = gate.TryEnter(30 * Mb);   // 90 <= 100: entra

        Assert.NotNull(a);
        Assert.Null(b);
        Assert.NotNull(c);

        a!.Dispose();
        var d = gate.TryEnter(60 * Mb);   // liberou 60: agora cabe (30 + 60 = 90)
        Assert.NotNull(d);
    }

    [Fact]
    public void TryEnter_SemOrcamento_SoLimitaPorQuantidade()
    {
        using var gate = Criar(maxConcorrentes: 2, memoriaMb: 0);

        Assert.NotNull(gate.TryEnter(10_000 * Mb));
        Assert.NotNull(gate.TryEnter(10_000 * Mb));
        Assert.Null(gate.TryEnter(1));
    }

    [Fact]
    public void TryEnter_SozinhoSempreEntra_MesmoAcimaDoOrcamento()
    {
        using var gate = Criar(maxConcorrentes: 2, memoriaMb: 100);

        Assert.NotNull(gate.TryEnter(500 * Mb));      // o controller é quem recusa os grandes demais
        Assert.Null(gate.TryEnter(1 * Mb));           // mas com ele rodando, nada mais cabe
    }

    [Fact]
    public void Vaga_DisposeDuasVezes_LiberaUmaVezSo()
    {
        using var gate = Criar(maxConcorrentes: 1, memoriaMb: 100);

        var vaga = gate.TryEnter(10 * Mb)!;
        vaga.Dispose();
        vaga.Dispose();

        var nova = gate.TryEnter(10 * Mb);
        Assert.NotNull(nova);
        Assert.Null(gate.TryEnter(10 * Mb));          // MaxConcorrentes = 1
    }

    [Fact]
    public void SimulacaoGate_PadraoUmaVagaSo()
    {
        var config = new ConfigurationBuilder().Build();
        using var gate = new SimulacaoGate(config);

        var a = gate.TryEnter();
        Assert.NotNull(a);
        Assert.Null(gate.TryEnter());

        a!.Dispose();
        Assert.NotNull(gate.TryEnter());
    }
}
