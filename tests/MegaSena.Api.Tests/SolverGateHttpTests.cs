
using System.Net;
using System.Net.Http.Json;
using MegaSena.Api.Services;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace MegaSena.Api.Tests;

/// <summary>
/// T9: 429 via HTTP quando o gate está sem vaga. Os gates são trocados por falsos que nunca dão vaga,
/// então o teste é determinístico (não depende de duas requisições simultâneas).
/// N3: fechamento grande demais para o orçamento de memória é recusado antes de rodar o solver.
/// </summary>
public class SolverGateHttpTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SolverGateHttpTests(CustomWebApplicationFactory factory) => _factory = factory;

    private sealed class GateSemVaga : ISolverGate
    {
        public IDisposable? TryEnter() => null;
    }

    private sealed class SimulacaoSemVaga : ISimulacaoGate
    {
        public IDisposable? TryEnter() => null;
    }

    private HttpClient CriarClienteComGatesOcupados() =>
        _factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.RemoveAll<ISolverGate>();
            s.AddSingleton<ISolverGate>(new GateSemVaga());
            s.RemoveAll<ISimulacaoGate>();
            s.AddSingleton<ISimulacaoGate>(new SimulacaoSemVaga());
        })).CreateClient();

    private static object FechamentoPequeno() => new
    {
        dezenas = new[] { 2, 5, 9, 14, 20, 31, 44 },
        garantia = 6,
        universoSorteado = 6,
        maxPares = 1
    };

    [Fact]
    public async Task Fechamento_ComGateOcupado_Retorna429ComRetryAfter()
    {
        var client = CriarClienteComGatesOcupados();
        var (token, _) = await AuthHelper.RegistrarELogarAsync(client);
        AuthHelper.Autenticar(client, token);

        var resp = await client.PostAsJsonAsync("/api/Cobertura/gerar", FechamentoPequeno());

        Assert.Equal(HttpStatusCode.TooManyRequests, resp.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(5), resp.Headers.RetryAfter?.Delta);
    }

    [Fact]
    public async Task UniformidadeFisica_ComSimulacaoOcupada_Retorna429ComRetryAfter()
    {
        var client = CriarClienteComGatesOcupados();

        var resp = await client.GetAsync("/api/MegaSena/uniformidade?fisico=true&n=50");

        Assert.Equal(HttpStatusCode.TooManyRequests, resp.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(5), resp.Headers.RetryAfter?.Delta);
    }

    [Fact]
    public async Task UniformidadeRapida_ComGatesOcupados_NaoUsaGate()
    {
        var client = CriarClienteComGatesOcupados();

        var resp = await client.GetAsync("/api/MegaSena/uniformidade?fisico=false&n=50");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task UniformidadeFisica_NaoDisputaVagaComOsFechamentos()
    {
        // Só o gate de fechamento está ocupado: a simulação física tem o próprio gate e deve passar
        var client = _factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.RemoveAll<ISolverGate>();
            s.AddSingleton<ISolverGate>(new GateSemVaga());
        })).CreateClient();

        var resp = await client.GetAsync("/api/MegaSena/uniformidade?fisico=true&n=50");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Fechamento_MaiorQueOOrcamentoDeMemoria_Retorna400SemRodarOSolver()
    {
        var client = _factory.WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?> { ["Solver:MemoriaMaximaMb"] = "1" })))
            .CreateClient();
        var (token, _) = await AuthHelper.RegistrarELogarAsync(client);
        AuthHelper.Autenticar(client, token);

        var resp = await client.PostAsJsonAsync("/api/Cobertura/gerar", new
        {
            dezenas = Enumerable.Range(1, 20).ToArray(),   // 20 dezenas, garantia 4 de 6: ~285 MB estimados
            garantia = 4,
            universoSorteado = 6,
            maxPares = 5
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Contains("grande demais", await resp.Content.ReadAsStringAsync());
    }
}
