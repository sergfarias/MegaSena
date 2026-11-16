
using MegaSena.Api.Services;
using Xunit;

namespace MegaSena.Api.Tests;

public class EstimadorDeMemoriaTests
{
    [Fact]
    public void VinteDezenas_Garantia4De6_FicaPertoDosTrezentosMb()
    {
        var bytes = EstimadorDeMemoria.BytesDoFechamento(dezenas: 20, garantia: 4, universoSorteado: 6);

        // 38.760 candidatos x 606 palavras x 8 bytes ≈ 190 MB, com margem de 1,5 ≈ 285 MB
        Assert.InRange(bytes, 250_000_000L, 320_000_000L);
    }

    [Fact]
    public void FechamentoPequeno_UsaPoucaMemoria()
    {
        Assert.True(EstimadorDeMemoria.BytesDoFechamento(dezenas: 6, garantia: 4, universoSorteado: 4) < 1_000);
    }

    [Fact]
    public void Sena6De6_NaoMontaMatrizDeCobertura()
    {
        var sena = EstimadorDeMemoria.BytesDoFechamento(dezenas: 20, garantia: 6, universoSorteado: 6);
        var quadra = EstimadorDeMemoria.BytesDoFechamento(dezenas: 20, garantia: 4, universoSorteado: 6);

        Assert.True(sena < quadra / 10);
    }

    [Fact]
    public void MaisDezenas_NuncaUsaMenosMemoria()
    {
        long anterior = 0;
        for (int n = 7; n <= 20; n++)
        {
            var atual = EstimadorDeMemoria.BytesDoFechamento(n, garantia: 4, universoSorteado: 6);
            Assert.True(atual >= anterior);
            anterior = atual;
        }
    }
}
