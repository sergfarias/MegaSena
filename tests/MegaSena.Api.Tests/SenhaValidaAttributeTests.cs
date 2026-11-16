using System.ComponentModel.DataAnnotations;
using MegaSena.Api.Models;
using Xunit;

namespace MegaSena.Api.Tests;

public class SenhaValidaAttributeTests
{
    private static ValidationResult? Validar(string senha) =>
        new SenhaValidaAttribute().GetValidationResult(senha, new ValidationContext(new object()));

    [Theory]
    [InlineData("curta")]
    [InlineData("12345678")]
    [InlineData("PASSWORD")]
    [InlineData("aaaaaaaa")]
    public void SenhaFracaOuCurta_EhRejeitada(string senha) => Assert.NotNull(Validar(senha));

    [Fact]
    public void Senha_AcimaDe72Bytes_EhRejeitada()
    {
        // 40 caracteres, mas 80 bytes em UTF-8
        var senha = new string('é', 40);
        Assert.Equal(40, senha.Length);
        Assert.NotNull(Validar(senha));
    }

    [Theory]
    [InlineData("senha123456")]
    [InlineData("novaSenha98765")]
    public void SenhaRazoavel_EhAceita(string senha) => Assert.Null(Validar(senha));
}