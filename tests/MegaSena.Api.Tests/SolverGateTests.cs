using MegaSena.Api.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace MegaSena.Api.Tests;

public class SolverGateTests
{
    [Fact]
    public void TryEnter_ComMaxConcorrentes1_DeveBloquearSegundaChamada()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Solver:MaxConcorrentes"] = "1"
            }).Build();

        var gate = new SolverGate(config);

        // Act
        var vaga1 = gate.TryEnter(); // Deve pegar a única vaga
        var vaga2 = gate.TryEnter(); // Deve falhar, pois a vaga está ocupada

        // Assert
        Assert.NotNull(vaga1);
        Assert.Null(vaga2);

        // Libera a vaga e tenta de novo
        vaga1.Dispose();
        var vaga3 = gate.TryEnter();
        Assert.NotNull(vaga3);

        vaga3?.Dispose();
        gate.Dispose();
    }
}