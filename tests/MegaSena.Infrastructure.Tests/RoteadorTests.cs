using MegaSena.Infrastructure.Cobertura;
using System.Diagnostics;
using Xunit;

namespace MegaSena.Infrastructure.Tests;

public class RoteadorTests
{
    private static SolverRoteador NovoRoteador() => new(new GulosoCoberturaSolver(), new SolverExato());

    [Fact]
    public void Roteador_Sena_DevolveTodosOsJogosValidos()
    {
        // 7 dezenas sem consecutivas: C(7,6) = 7 jogos, todos válidos com maxPares 1
        var dezenas = new HashSet<int> { 2, 5, 9, 14, 20, 31, 44 };

        var r = NovoRoteador().Resolver(dezenas, garantia: 6, universoSorteado: 6, maxPares: 1);

        Assert.Equal(7, r.Jogos.Count);
    }

    [Fact]
    public void Roteador_Sena_RespeitaMaxPares()
    {
        // 1..7 são todas consecutivas: qualquer jogo de 6 tem pelo menos 4 pares; com maxPares 0 nenhum serve
        var dezenas = new HashSet<int> { 1, 2, 3, 4, 5, 6, 7 };

        var r = NovoRoteador().Resolver(dezenas, garantia: 6, universoSorteado: 6, maxPares: 0);

        Assert.Empty(r.Jogos);
    }

    [Fact]
    public void Roteador_9DezenasPesadas_RespondeDentroDoOrcamento()
    {
        // Caso que antes estourava memória e ignorava o timeout no exato
        var dezenas = new HashSet<int> { 3, 9, 14, 21, 27, 33, 41, 52, 58 };

        var sw = Stopwatch.StartNew();
        var r = NovoRoteador().Resolver(dezenas, garantia: 5, universoSorteado: 5, maxPares: 5);
        sw.Stop();

        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(10), $"Demorou {sw.Elapsed.TotalSeconds:F1}s");
        Assert.NotEmpty(r.Jogos);
    }

    [Fact]
    public void Roteador_TokenJaCancelado_LancaOperationCanceled()
    {
        var dezenas = new HashSet<int> { 3, 9, 14, 21, 27, 33, 41, 52, 58 };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() =>
            NovoRoteador().Resolver(dezenas, garantia: 5, universoSorteado: 5, maxPares: 5, ct: cts.Token));
    }
}
