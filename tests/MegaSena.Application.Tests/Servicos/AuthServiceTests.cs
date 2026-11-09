using MegaSena.Application.Servicos;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace MegaSena.Application.Tests.Servicos;

public class AuthServiceTests
{
    private static AuthService CriarServico()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "chave-de-teste-com-mais-de-32-bytes-aqui-123456",
                ["Jwt:Issuer"] = "MegaSenaApi",
                ["Jwt:Audience"] = "MegaSenaFront"
            })
            .Build();
        return new AuthService(config);
    }

    [Fact]
    public void GerarHashSenha_DeveGerarHashDiferenteDoOriginal()
    {
        var service = CriarServico();
        var senha = "minhasenha123";
        var hash = service.GerarHashSenha(senha);

        Assert.NotEqual(senha, hash);
        Assert.True(hash.Length > 20); // BCrypt tem ~60 chars
    }

    [Fact]
    public void VerificarSenha_ComSenhaCorreta_DeveRetornarTrue()
    {
        var service = CriarServico();
        var senha = "minhasenha123";
        var hash = service.GerarHashSenha(senha);

        Assert.True(service.VerificarSenha(senha, hash));
    }

    [Fact]
    public void VerificarSenha_ComSenhaErrada_DeveRetornarFalse()
    {
        var service = CriarServico();
        var hash = service.GerarHashSenha("senhaCorreta123");

        Assert.False(service.VerificarSenha("senhaErrada456", hash));
    }

    [Fact]
    public void GerarTokenJwt_DeveGerarTokenNaoVazio()
    {
        var service = CriarServico();
        var token = service.GerarTokenJwt(
            Guid.NewGuid(), "Fulano", "fulano@teste.com", "Cliente");

        Assert.NotNull(token);
        Assert.NotEmpty(token);
        // JWT tem 3 partes separadas por ponto
        Assert.Equal(3, token.Split('.').Length);
    }

    [Fact]
    public void AuthService_SemJwtKey_DeveLancarExcecao()
    {
        var config = new ConfigurationBuilder().Build();
        var ex = Assert.Throws<InvalidOperationException>(() => new AuthService(config));
        Assert.Contains("Jwt:Key", ex.Message);
    }
}
