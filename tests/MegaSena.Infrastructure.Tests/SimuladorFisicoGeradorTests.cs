using MegaSena.Domain;
using MegaSena.Infrastructure;
using Xunit;

namespace MegaSena.Infrastructure.Tests;

public class SimuladorFisicoGeradorTests
{
    [Fact]
    public void Sortear_DeveGerarJogoValidoEmTempoViavel()
    {
        // Arrange
        var config = new ConfiguracaoFisica { RotacaoRpm = 10, Turbulencia = 0.1 }; // Menos caos para ser rápido
        var simulador = new SimuladorFisicoGerador(config, seed: 42); // Seed fixa para teste ser determinístico
        var universo = Universo.Completo();
        var regra = new MaximoParesConsecutivos(3);

        // Act
        var jogo = simulador.Sortear(universo, regra);

        // Assert
        Assert.Equal(6, jogo.Quantidade);
        Assert.True(regra.Aceita(jogo));
    }
}