using MegaSena.Infrastructure.Seguranca;
using Xunit;

namespace MegaSena.Infrastructure.Tests.Seguranca; // Mude o namespace

public class AuditoriaServiceTests
{
    private readonly AuditoriaService _service;

    public AuditoriaServiceTests()
    {
        _service = new AuditoriaService(); // Instancia o SEU código real
    }

    [Fact]
    public void GerarHashAuditoria_DeveRetornarHashDe64Caracteres()
    {
        // Arrange
        string json = "[{\"dezenas\":[1,2,3,4,5,6]}]";

        // Act
        string hash = _service.GerarHashAuditoria(json);

        // Assert
        Assert.NotNull(hash);
        Assert.Equal(64, hash.Length); // SHA-256 em hexadecimal tem 64 caracteres
    }

    [Fact]
    public void GerarHashAuditoria_MesmoJson_DeveSerIdempotente()
    {
        string json = "{\"jogo\": \"1,2,3,4,5,6\"}";

        string hash1 = _service.GerarHashAuditoria(json);
        string hash2 = _service.GerarHashAuditoria(json);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GerarHashAuditoria_JsonAdulterado_DeveGerarHashDiferente()
    {
        string jsonOriginal = "{\"jogo\": \"1,2,3,4,5,6\"}";
        string jsonAdulterado = "{\"jogo\": \"1,2,3,4,5,7\"}";

        string hash1 = _service.GerarHashAuditoria(jsonOriginal);
        string hash2 = _service.GerarHashAuditoria(jsonAdulterado);

        Assert.NotEqual(hash1, hash2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void GerarHashAuditoria_JsonVazioOuNulo_DeveLancarExcecao(string? jsonInvalido)
    {
        Assert.Throws<ArgumentException>(() => _service.GerarHashAuditoria(jsonInvalido ?? string.Empty));
    }
}