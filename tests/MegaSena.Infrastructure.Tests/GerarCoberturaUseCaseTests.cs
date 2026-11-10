using MegaSena.Application.CasosDeUso;
using MegaSena.Infrastructure.Seguranca;
using MegaSena.Infrastructure.Cobertura;

namespace MegaSena.Infrastructure.Tests;

public class GerarCoberturaUseCaseTests
{
    private static GerarCoberturaUseCase CriarUseCase()
    {
        var guloso = new GulosoCoberturaSolver();
        var exato = new SolverExato();
        var roteador = new SolverRoteador(guloso, exato);
        return new GerarCoberturaUseCase(roteador);
    }

    [Fact]
    public void Executar_ComUniversoPequeno_DeveGarantirTotal()
    {
        // 6 dezenas, garantia 6, universo 6 → só existe 1 jogo e ele cobre tudo.
        var dezenas = new HashSet<int> { 1, 2, 3, 4, 5, 6 };
        var uc = CriarUseCase();

        var resultado = uc.Executar(dezenas, garantia: 6, universoSorteado: 6, maxPares: 5);

        Assert.True(resultado.GarantiaTotal,
            "Com 6 dezenas e garantia 6, o resultado deve garantir 100% de cobertura.");
        Assert.Equal(1, resultado.TotalJogos);
    }

    [Fact]
    public void Executar_DevePreencherCustoTotal()
    {
        var dezenas = new HashSet<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        var uc = CriarUseCase();

        var resultado = uc.Executar(dezenas, garantia: 4, universoSorteado: 4, maxPares: 2);

        Assert.True(resultado.CustoTotal > 0);
        Assert.Equal(resultado.TotalJogos * 6.00m, resultado.CustoTotal);
    }

    [Fact]
    public void Hash_ParaOMesmoFechamento_DeveSerUnico_PorCausaDoNonce()
    {
        // Esse teste é o do PDF (Seção 5): dois INSERTs idênticos
        // não devem colidir por causa do Nonce.
        var service = new AuditoriaService();
        var jogos = new[] { new[] { 1, 2, 3, 4, 5, 6 } };

        var payload1 = new { Jogos = jogos, Nonce = Guid.NewGuid(), CriadoEmUtc = DateTime.UtcNow };
        var payload2 = new { Jogos = jogos, Nonce = Guid.NewGuid(), CriadoEmUtc = DateTime.UtcNow };

        var json1 = System.Text.Json.JsonSerializer.Serialize(payload1);
        var json2 = System.Text.Json.JsonSerializer.Serialize(payload2);

        var h1 = service.GerarHashAuditoria(json1);
        var h2 = service.GerarHashAuditoria(json2);

        Assert.NotEqual(h1, h2);
    }
}