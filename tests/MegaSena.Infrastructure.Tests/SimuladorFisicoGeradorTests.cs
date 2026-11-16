
using MegaSena.Domain;
using MegaSena.Infrastructure;
using Xunit;

namespace MegaSena.Infrastructure.Tests;

public class SimuladorFisicoGeradorTests
{
    [Fact]
    public void Sortear_DeveGerarJogoValidoEmTempoViavel()
    {
        var config = new ConfiguracaoFisica { Turbulencia = 0.1 }; // Menos caos para ser rápido
        var simulador = new SimuladorFisicoGerador(config, seed: 42); // Seed fixa para teste ser determinístico
        var universo = Universo.Completo();
        var regra = new MaximoParesConsecutivos(3);

        var jogo = simulador.Sortear(universo, regra);

        Assert.Equal(6, jogo.Quantidade);
        Assert.True(regra.Aceita(jogo));
    }

    [Fact]
    public void Sortear_ComMesmaSeed_DaOMesmoJogo()
    {
        var universo = Universo.Completo();
        var regra = new MaximoParesConsecutivos(5);

        var a = new SimuladorFisicoGerador(new ConfiguracaoFisica(), seed: 7).Sortear(universo, regra);
        var b = new SimuladorFisicoGerador(new ConfiguracaoFisica(), seed: 7).Sortear(universo, regra);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Sortear_UniversoReduzido_SoUsaDezenasDoUniverso()
    {
        var universo = new Universo(new[] { 2, 4, 6, 8, 10, 12, 14, 16, 18, 20 });
        var simulador = new SimuladorFisicoGerador(new ConfiguracaoFisica(), seed: 3);

        for (int i = 0; i < 5; i++)
        {
            var jogo = simulador.Sortear(universo, new MaximoParesConsecutivos(5));
            Assert.All(jogo.Dezenas(), d => Assert.True(universo.Contem(d)));
        }
    }

    [Fact]
    public void ConfiguracaoFisica_ChavesAntigasNoJson_SaoIgnoradas()
    {
        var caminho = Path.GetTempFileName();
        try
        {
            File.WriteAllText(caminho, """{ "MassaBola": 0.1, "RotacaoRpm": 12, "JatoAltura": 0.3, "Gravidade": 5.0 }""");

            var config = ConfiguracaoFisica.Carregar(caminho);

            Assert.Equal(5.0, config.Gravidade);
            Assert.Equal(0.090, config.DiametroBola);
        }
        finally
        {
            File.Delete(caminho);
        }
    }
}
