using MegaSena.Domain;
using Xunit;

namespace MegaSena.Domain.Tests;

public class CombinatoriaTests
{
    [Theory]
    [InlineData(60, 6, 50_063_860)]
    [InlineData(42, 6, 5_245_786)]
    [InlineData(38, 6, 2_760_681)]
    public void C_Bate_Com_PDF(int n, int k, long esperado)
        => Assert.Equal(esperado, Combinatoria.C(n, k));
}