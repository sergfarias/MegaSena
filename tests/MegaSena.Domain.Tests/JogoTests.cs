using MegaSena.Domain;
using Xunit;

namespace MegaSena.Domain.Tests;

public class JogoTests
{
    [Fact]
    public void Pares_Consecutivos_Trio_Conta_Dois()
    {
        var j = Jogo.DasDezenas(new[] { 10, 11, 12, 30, 40, 50 });
        Assert.Equal(2, j.ParesConsecutivos());
    }

    [Fact]
    public void Pares_Consecutivos_Dois_Pares_Separados()
    {
        var j = Jogo.DasDezenas(new[] { 5, 6, 20, 21, 40, 41 });
        Assert.Equal(3, j.ParesConsecutivos());
    }

    [Fact]
    public void Pares_Consecutivos_Zero()
    {
        var j = Jogo.DasDezenas(new[] { 1, 3, 5, 7, 9, 11 });
        Assert.Equal(0, j.ParesConsecutivos());
    }

    [Fact]
    public void Blocos_Trio_Conta_Um()
    {
        // {10,11,12} = 1 corrida; {30}, {40}, {50} são soltas → não contam
        var j = Jogo.DasDezenas(new[] { 10, 11, 12, 30, 40, 50 });
        Assert.Equal(1, j.BlocosConsecutivos());
    }

    [Fact]
    public void Blocos_Duas_Corridas_Separadas()
    {
        // {10,11,12} e {30,31} = 2 corridas; {40}, {50} soltas
        var j = Jogo.DasDezenas(new[] { 10, 11, 12, 30, 31, 40 });
        Assert.Equal(2, j.BlocosConsecutivos());
    }


    [Fact]
    public void Blocos_Sem_Consecutivas()
    {
        var j = Jogo.DasDezenas(new[] { 1, 3, 5, 7, 9, 11 });
        Assert.Equal(0, j.BlocosConsecutivos());
    }

    [Fact]
    public void Contem_E_Dezenas()
    {
        var j = Jogo.DasDezenas(new[] { 1, 10, 20, 30, 40, 60 });
        Assert.True(j.Contem(60));
        Assert.False(j.Contem(59));
        Assert.Equal(new[] { 1, 10, 20, 30, 40, 60 }, j.Dezenas());
        Assert.Equal(6, j.Quantidade);
    }

    [Fact]
    public void DasDezenas_Rejeita_Repetida_E_Fora_De_Faixa()
    {
        Assert.Throws<ArgumentException>(() =>
            Jogo.DasDezenas(new[] { 1, 1, 2, 3, 4, 5 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Jogo.DasDezenas(new[] { 0, 1, 2, 3, 4, 5 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Jogo.DasDezenas(new[] { 1, 2, 3, 4, 5, 61 }));
    }

}