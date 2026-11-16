
using MegaSena.Application.CasosDeUso;
using MegaSena.Application.Portas;
using MegaSena.Domain;
using Xunit;

namespace MegaSena.Application.Tests.CasosDeUso;

public class UniformidadeComRegraTests
{
    /// <summary>Sorteia uniformemente entre os jogos que a regra aceita (amostragem por rejeição).</summary>
    private sealed class GeradorUniformeValido : IGeradorDeSorteio
    {
        private readonly Random _rnd;
        public GeradorUniformeValido(int seed) => _rnd = new Random(seed);
        public string Nome => "Uniforme entre válidos";

        public Jogo Sortear(Universo universo, IRegraDeJogo regra)
        {
            var arr = universo.Dezenas.ToArray();
            while (true)
            {
                for (int i = 0; i < 6; i++)
                {
                    int j = _rnd.Next(i, arr.Length);
                    (arr[i], arr[j]) = (arr[j], arr[i]);
                }

                var jogo = Jogo.DasDezenas(arr.Take(6));
                if (regra.Aceita(jogo)) return jogo;
            }
        }
    }

    [Fact]
    public void Executar_SemParesEmUniversoReduzido_GeradorCorreto_NaoReprova()
    {
        // 38 dezenas consecutivas e nenhum par consecutivo permitido: as dezenas das pontas entram em bem mais jogos
        // válidos que as do meio (18,2% contra 15,7% de chance de estar num jogo, contra 15,8% se fosse uniforme).
        // Com a frequência esperada uniforme (cálculo antigo) este teste daria p-valor da ordem de 1e-20 mesmo com um
        // gerador correto; com as probabilidades exatas (N10) o p-valor fica normal. Por isso o teste protege o N10.
        var r = new TestarUniformidadeUseCase().Executar(
            new GeradorUniformeValido(seed: 12345),
            new Universo(Enumerable.Range(1, 38)),
            n: 20_000,
            new MaximoParesConsecutivos(0));

        Assert.Equal(37, r.GrausDeLiberdade);
        Assert.True(r.PValor >= 0.0001, $"p-valor {r.PValor} baixo demais para um gerador correto.");
    }

    [Fact]
    public void Executar_ComRegraImpossivel_LancaArgumentException()
    {
        var universo = new Universo(Enumerable.Range(1, 8));

        Assert.Throws<ArgumentException>(() => new TestarUniformidadeUseCase().Executar(
            new GeradorUniformeValido(1), universo, n: 100, new MaximoParesConsecutivos(0)));
    }

    [Fact]
    public void Executar_ComMaximoCincoOuMais_UsaOTesteSemRestricao()
    {
        var r = new TestarUniformidadeUseCase().Executar(
            new GeradorUniformeValido(99), Universo.Completo(), n: 3_000, new MaximoParesConsecutivos(5));

        Assert.Equal(59, r.GrausDeLiberdade);
    }
}
