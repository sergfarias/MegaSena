using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace MegaSena.Api.Tests;

/// <summary>
/// Helper: registra um usuário novo e devolve o JWT (2 chamadas ao limite "auth" de 5 por 5 min).
/// Cada classe de teste usa o próprio CustomWebApplicationFactory, então os limites não se somam entre classes.
/// </summary>
internal static class AuthHelper
{
    public static async Task<(string Token, string Email)> RegistrarELogarAsync(HttpClient client)
    {
        var sufixo = Guid.NewGuid().ToString("N");
        var login = $"user{sufixo}"[..20];
        var email = $"user{sufixo}@teste.com";
        const string senha = "senha123456";

        var reg = await client.PostAsJsonAsync("/api/Auth/registrar",
            new { nome = "Teste", login, email, senha });
        Assert.Equal(HttpStatusCode.OK, reg.StatusCode);

        var resp = await client.PostAsJsonAsync("/api/Auth/login", new { login, senha });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var token = doc.RootElement.GetProperty("token").GetString()!;
        return (token, email);
    }

    public static void Autenticar(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
}

// ---------------------------------------------------------------------------
// Recuperação de senha, contato e lotes (limites: auth 5/5min, senha 3/15min)
// ---------------------------------------------------------------------------
public class SegurancaContaTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SegurancaContaTests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task EsqueciSenha_ForaDeDevelopment_NaoDevolveToken()
    {
        var (_, email) = await AuthHelper.RegistrarELogarAsync(_client);

        var resp = await _client.PostAsJsonAsync("/api/Auth/esqueci-senha", new { email });
        var body = await resp.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.DoesNotContain("tokendebug", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Contato_LeituraDeMensagens_SemLogin_RetornaUnauthorized()
    {
        var resp = await _client.GetAsync("/api/Contato/mensagens");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Contato_LeituraDeMensagens_UsuarioComum_RetornaForbidden()
    {
        var (token, _) = await AuthHelper.RegistrarELogarAsync(_client);
        AuthHelper.Autenticar(_client, token);

        var resp = await _client.GetAsync("/api/Contato/mensagens");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }
}

public class SegurancaLoteTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SegurancaLoteTests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task MeuLoteDetalhado_HashDeOutroUsuarioOuInexistente_RetornaNotFound()
    {
        var (token, _) = await AuthHelper.RegistrarELogarAsync(_client);
        AuthHelper.Autenticar(_client, token);

        var resp = await _client.GetAsync("/api/Auditoria/meus-lotes/HASHQUENAOEXISTE");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}

// ---------------------------------------------------------------------------
// /gerar: validações de entrada (limite "fechamento": 5/min por usuário -> 4 chamadas aqui)
// ---------------------------------------------------------------------------
public class SegurancaGerarTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private bool _autenticado;

    public SegurancaGerarTests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    private async Task<HttpResponseMessage> GerarAsync(object corpo)
    {
        if (!_autenticado)
        {
            var (token, _) = await AuthHelper.RegistrarELogarAsync(_client);
            AuthHelper.Autenticar(_client, token);
            _autenticado = true;
        }
        return await _client.PostAsJsonAsync("/api/Cobertura/gerar", corpo);
    }

    [Fact]
    public async Task Gerar_DezenaForaDe1a60_RetornaBadRequest()
    {
        var resp = await GerarAsync(new { dezenas = new[] { 1, 2, 3, 4, 5, 61 }, garantia = 4, universoSorteado = 6, maxPares = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Gerar_DezenasRepetidas_RetornaBadRequest()
    {
        var resp = await GerarAsync(new { dezenas = new[] { 1, 1, 2, 3, 4, 5, 6 }, garantia = 4, universoSorteado = 6, maxPares = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Gerar_MaxParesForaDaFaixa_RetornaBadRequest()
    {
        var resp = await GerarAsync(new { dezenas = new[] { 1, 5, 9, 14, 20, 31, 44 }, garantia = 4, universoSorteado = 6, maxPares = 9 });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Gerar_MenosDeSeisDezenas_RetornaBadRequest()
    {
        var resp = await GerarAsync(new { dezenas = new[] { 1, 2, 3 }, garantia = 4, universoSorteado = 6, maxPares = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}

// ---------------------------------------------------------------------------
// Backtest: limites (mesmo limite "fechamento" -> 4 chamadas aqui)
// ---------------------------------------------------------------------------
public class SegurancaBacktestTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private bool _autenticado;

    public SegurancaBacktestTests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    private async Task<HttpResponseMessage> BacktestAsync(object corpo)
    {
        if (!_autenticado)
        {
            var (token, _) = await AuthHelper.RegistrarELogarAsync(_client);
            AuthHelper.Autenticar(_client, token);
            _autenticado = true;
        }
        return await _client.PostAsJsonAsync("/api/Cobertura/backtest?janela=100", corpo);
    }

    [Fact]
    public async Task Backtest_MaisDe1000Jogos_RetornaBadRequest()
    {
        var jogos = Enumerable.Range(0, 1001).Select(_ => new[] { 1, 2, 3, 4, 5, 6 }).ToArray();
        var resp = await BacktestAsync(jogos);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Backtest_JogoComMenosDeSeisDezenas_RetornaBadRequest()
    {
        var resp = await BacktestAsync(new[] { new[] { 1, 2, 3, 4, 5 } });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Backtest_SemConcursosSuficientes_RetornaUnprocessableEntity()
    {
        // Banco InMemory do ambiente Testing não tem concursos importados
        var resp = await BacktestAsync(new[] { new[] { 1, 2, 3, 4, 5, 6 } });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
    }
}

// ---------------------------------------------------------------------------
// Endpoints protegidos sem token
// ---------------------------------------------------------------------------
public class SegurancaSemLoginTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SegurancaSemLoginTests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    [Theory]
    [InlineData("/api/Cobertura/gerar")]
    [InlineData("/api/Cobertura/backtest")]
    public async Task EndpointsDeFechamento_SemToken_RetornamUnauthorized(string rota)
    {
        var resp = await _client.PostAsJsonAsync(rota, new { });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task MeusLotes_SemToken_RetornaUnauthorized()
    {
        var resp = await _client.GetAsync("/api/Auditoria/meus-lotes");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
