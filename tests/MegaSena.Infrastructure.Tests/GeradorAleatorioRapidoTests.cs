using MegaSena.Domain;
using MegaSena.Infrastructure;
using Xunit;

namespace MegaSena.Infrastructure.Tests;

public class GeradorAleatorioRapidoTests
{
    [Fact]
    public void Sortear_DeveRespeitarARegraDeJogo()
    {
        var gerador = new GeradorAleatorioRapido();
        var universo = Universo.Completo();
        var regra = new MaximoParesConsecutivos(0); // Regra rígida: zero pares

        for (int i = 0; i < 50; i++)
        {
            var jogo = gerador.Sortear(universo, regra);
            Assert.True(regra.Aceita(jogo), $"O gerador devolveu um jogo inválido: {jogo}");
        }
    }
}