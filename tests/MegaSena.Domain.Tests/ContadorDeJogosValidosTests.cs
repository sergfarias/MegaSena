using MegaSena.Domain;
using Xunit;

namespace MegaSena.Domain.Tests;

public class ContadorDeJogosValidosTests
{
    private static Universo UniversoDosExemplos()
    {
        // Exclusões dos 3 sorteios de exemplo do MegaSena.pdf
        var excluidas = new[]
        {
            4, 11, 23, 37, 48, 59,
            2, 15, 26, 33, 41, 52,
            7, 19, 28, 35, 44, 60
        };
        return Universo.Sem(excluidas); // 42 dezenas
    }

    [Fact]
    public void Universo_De_Exemplo_Tem_42_Dezenas()
        => Assert.Equal(42, UniversoDosExemplos().Tamanho);

    [Fact]
    public void Contagem_42_Bolas_Maximo_1_Par()
    {
        long jogos = ContadorDeJogosValidos.Contar(UniversoDosExemplos(), maxPares: 1);
        Assert.Equal(4_955_700, jogos);
    }

    [Fact]
    public void Distribuicao_Por_Pares_Bate_Com_PDF()
    {
        var dist = ContadorDeJogosValidos.DistribuicaoPorPares(UniversoDosExemplos());
        Assert.Equal(3_266_249, dist[0]);
        Assert.Equal(1_689_451, dist[1]);
        Assert.Equal(275_186, dist[2]);
        Assert.Equal(14_674, dist[3]);
        Assert.Equal(225, dist[4]);
        Assert.Equal(1, dist[5]);
        Assert.Equal(5_245_786, dist.Sum());
    }

    [Fact]
    public void Contagem_60_Bolas_Maximo_1_Par()
        => Assert.Equal(46_383_480,
            ContadorDeJogosValidos.Contar(Universo.Completo(), maxPares: 1));


    [Fact]
    public void ContarPorBlocos_42_Bolas_Maximo_1()
    {
        // Variante "trio conta como 1" do PDF: 5.061.302
        long jogos = ContadorDeJogosValidos.ContarPorBlocos(UniversoDosExemplos(), maxBlocos: 1);
        Assert.Equal(5_061_302, jogos);
    }

    [Fact]
    public void Contagem_38_Bolas_Maximo_1_Par()
    {
        var universo42 = UniversoDosExemplos();
        var dezenas42 = universo42.Dezenas.ToArray();
        const long alvo = 2_651_467;

        // Varre todas as C(42,4) = 111.930 combinações de exclusão.
        // Para cada uma, monta o universo de 38 e conta com a DP.
        // Se achar a combinação que bate com o PDF, guarda para o assert.
        int[]? exclusaoQueBate = null;

        for (int a = 0; a < dezenas42.Length; a++)
            for (int b = a + 1; b < dezenas42.Length; b++)
                for (int c = b + 1; c < dezenas42.Length; c++)
                    for (int d = c + 1; d < dezenas42.Length; d++)
                    {
                        var excluidas = new[] { dezenas42[a], dezenas42[b], dezenas42[c], dezenas42[d] };
                        var u38 = new Universo(dezenas42.Except(excluidas));
                        if (ContadorDeJogosValidos.Contar(u38, maxPares: 1) == alvo)
                        {
                            exclusaoQueBate = excluidas;
                            goto achou;
                        }
                    }

    achou:
        Assert.NotNull(exclusaoQueBate); // se null, o valor do PDF não é reprodutível assim
        var universo38 = new Universo(dezenas42.Except(exclusaoQueBate));
        Assert.Equal(38, universo38.Tamanho);
        Assert.Equal(alvo, ContadorDeJogosValidos.Contar(universo38, maxPares: 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public void Contar_MaxParesValido_NaoLancaExcecao(int maxPares)
    {
        var ex = Record.Exception(() =>
            ContadorDeJogosValidos.Contar(UniversoDosExemplos(), maxPares));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(10)]
    [InlineData(100)]
    public void Contar_MaxParesAcimaDe5_NaoDeveQuebrar(int maxPares)
    {
        // Esse é o teste que o PDF pediu (Seção 3.2).
        // O endpoint /contar deve aceitar esses valores sem dar 500.
        var ex = Record.Exception(() =>
            ContadorDeJogosValidos.Contar(UniversoDosExemplos(), maxPares));
        Assert.Null(ex);
    }

    [Fact]
    public void Contar_MaxPares5_DeveSerIgualAoTotalDeCombinacoes()
    {
        // Com maxPares=5, todos os jogos são válidos.
        // Logo, o total deve ser C(42,6) = 5.245.786
        var total = ContadorDeJogosValidos.Contar(UniversoDosExemplos(), maxPares: 5);
        Assert.Equal(5_245_786, total);
    }
}