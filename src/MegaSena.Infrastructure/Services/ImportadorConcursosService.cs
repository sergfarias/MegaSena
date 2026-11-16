using MegaSena.Application.Models;
using MegaSena.Application.Portas;
using MegaSena.Domain;
using MegaSena.Domain.Calculadora;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using System.Text.RegularExpressions;

namespace MegaSena.Infrastructure.Services;

public class ImportadorConcursosService : BackgroundService
{
    private const string UrlBase = "https://api.guidi.dev.br/loteria/megasena";
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ImportadorConcursosService> _log;
    private readonly IMemoryCache _cache;

    public ImportadorConcursosService(
        IServiceScopeFactory scopes,
        ILogger<ImportadorConcursosService> log,
        IMemoryCache cache)
    {
        _scopes = scopes;
        _log = log;
        _cache = cache;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Espera 10s na inicialização
        await Task.Delay(TimeSpan.FromSeconds(10), ct);

        while (!ct.IsCancellationRequested)
        {
            bool houveErro = false;
            try
            {
                await ImportarConcursosAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                houveErro = true;
                _log.LogWarning(ex, "Falha ao importar concursos.");
            }

            // Se houve erro, tenta de novo em 15 minutos. Se deu tudo certo, espera 6 horas.
            var delay = houveErro ? TimeSpan.FromMinutes(15) : TimeSpan.FromHours(6);
            _log.LogInformation("Próxima verificação em {Delay}.", delay);
            await Task.Delay(delay, ct);
        }
    }

    internal async Task ImportarConcursosAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IConcursoRepository>();
        var http = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("ApiCaixa");

        var existentes = (await repo.ObterNumerosAsync()).ToHashSet();

        using var resp = await http.GetAsync($"{UrlBase}/ultimo", ct);
        if (!resp.IsSuccessStatusCode)
        {
            _log.LogWarning("API externa indisponível.");
            return;
        }

        var json = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var numeroUltimo = doc.RootElement.GetProperty("numero").GetInt32();

        // Números que faltam entre 1 e o último concurso (inclui buracos deixados por falhas anteriores)
        var faltantes = Enumerable.Range(1, numeroUltimo).Where(n => !existentes.Contains(n)).ToList();

        if (faltantes.Count == 0)
        {
            _log.LogInformation("Já temos todos os concursos até #{Numero}.", numeroUltimo);
            return;
        }

        _log.LogInformation("Importando {Qtd} concursos faltantes (de {Min} até {Max}).",
            faltantes.Count, faltantes[0], faltantes[^1]);

        var lote = new List<ConcursoDto>();
        const int tamanhoLote = 50;
        int falhas = 0;
        int falhasSeguidas = 0;
        const int maxFalhasSeguidas = 10;

        foreach (var num in faltantes)
        {
            ct.ThrowIfCancellationRequested();

            ConcursoDto? c = null;
            try
            {
                c = await BuscarConcursoAsync(http, num, ct);
                await Task.Delay(150, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.LogWarning("Falha no concurso #{Num}: {Msg}", num, ex.Message);
            }

            if (c == null)
            {
                falhas++; // o buraco será refeito na próxima rodada
                falhasSeguidas++;
                if (falhasSeguidas >= maxFalhasSeguidas)
                {
                    _log.LogWarning("{N} falhas seguidas; interrompendo a rodada.", falhasSeguidas);
                    break;
                }
                continue;
            }

            falhasSeguidas = 0;
            lote.Add(c);

            // Salva a cada 50 concursos. Erro de banco sobe para o ExecuteAsync (retry curto).
            if (lote.Count >= tamanhoLote)
            {
                await repo.AdicionarVariosAsync(lote);
                _cache.Remove("freq-dezenas");
                _log.LogInformation("Salvos {Qtd} concursos no banco.", lote.Count);
                lote.Clear();
            }
        }

        if (lote.Count > 0)
        {
            await repo.AdicionarVariosAsync(lote);
            _cache.Remove("freq-dezenas");
            _log.LogInformation("Salvos os últimos {Qtd} concursos no banco.", lote.Count);
        }

        await RodarQuiQuadradoAsync(repo, ct);

        // Sinaliza falha parcial para o retry curto (15 min) em vez de esperar 6 horas
        if (falhas > 0)
            throw new InvalidOperationException($"{falhas} concurso(s) não puderam ser importados.");
    }

    private async Task RodarQuiQuadradoAsync(IConcursoRepository repo, CancellationToken ct)
    {
        try
        {
            var todos = await repo.ObterTodosAsync();
            if (todos.Count < 50)
            {
                _log.LogInformation("Ainda não há concursos suficientes para o Qui-Quadrado ({Count}/50).", todos.Count);
                return;
            }

            var freq = new int[61];
            foreach (var c in todos)
            {
                foreach (var dezenaStr in c.DezenasSorteadas.Split(','))
                {
                    if (int.TryParse(dezenaStr, out int d) && d >= 1 && d <= 60)
                        freq[d]++;
                }
            }

            var universo = Universo.Completo();
            var n = todos.Count;

            // Usa a calculadora unificada (Seção 2.1)
            double chi2 = CalculadoraQuiQuadrado.Calcular(freq, n, universo.Dezenas);
            int gl = universo.Tamanho - 1;
            double p = DistribuicaoQuiQuadrado.PValor(chi2, gl);

            _log.LogInformation(
                "?? Qui-Quadrado com {N} concursos: ?²={Chi2:F2}, gl={Gl}, p-valor={P:F4} ({Resultado})",
                n, chi2, gl, p,
                p >= 0.05 ? "? Sem evidência de viés" : "?? Possível viés detectado");
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Falha no pós-processamento (Qui-Quadrado).");
        }
    }

    private async Task<ConcursoDto?> BuscarConcursoAsync(HttpClient http, int numero, CancellationToken ct)
    {
        using var resp = await http.GetAsync($"{UrlBase}/{numero}", ct);
        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadAsStringAsync(ct);
        try
        {
            return MapearConcurso(json);
        }
        catch (Exception ex)
        {
            _log.LogWarning("Erro ao mapear concurso #{Num}: {Msg}", numero, ex.Message);
            return null;
        }
    }

    /// <summary>Converte o JSON de um concurso da API externa. Lança exceção se faltar campo obrigatório.</summary>
    public static ConcursoDto MapearConcurso(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var dataString = root.GetProperty("dataApuracao").GetString()!;
        var dataSorteio = DateTime.ParseExact(dataString, "dd/MM/yyyy", CultureInfo.InvariantCulture);

        DateTime? dataProximo = null;
        if (root.TryGetProperty("dataProximoConcurso", out var dp) && dp.ValueKind == JsonValueKind.String)
        {
            var dpString = dp.GetString()!;
            dataProximo = DateTime.ParseExact(dpString, "dd/MM/yyyy", CultureInfo.InvariantCulture);
        }

        var concurso = new ConcursoDto
        {
            NumeroConcurso = root.GetProperty("numero").GetInt32(),
            DataHoraSorteio = dataSorteio,
            CidadeSorteio = root.TryGetProperty("nomeMunicipioSorteio", out var cidade) ? cidade.GetString() : null,
            UfSorteio = root.TryGetProperty("ufSorteio", out var uf) ? uf.GetString() : null,
            DezenasSorteadas = string.Join(",", root.GetProperty("listaDezenas").EnumerateArray().Select(x => x.GetString()!)),
            DezenasOrdemSorteio = root.TryGetProperty("dezenasSorteadasOrdemSorteio", out var ordem)
                ? string.Join(",", ordem.EnumerateArray().Select(x => x.GetString()!))
                : null,

            ValorArrecadado = LerDecimal(root, "valorArrecadado"),
            ValorAcumulado = LerDecimal(root, "valorAcumuladoConcursoEspecial"),
            NumeroConcursoAnterior = LerInt(root, "numeroConcursoAnterior"),
            ProximoConcurso = LerInt(root, "proximoConcurso"),

            Acumulado = root.TryGetProperty("acumulado", out var ac) && ac.ValueKind == JsonValueKind.True,
            DataProximoConcurso = dataProximo,
            ListaRateioPremioJson = root.TryGetProperty("listaRateioPremio", out var rateio)
                ? rateio.GetRawText()
                : null
        };

        // Ganhadores e prêmios por faixa
        if (root.TryGetProperty("listaRateioPremio", out var rateioList) && rateioList.ValueKind == JsonValueKind.Array)
        {
            foreach (var faixa in rateioList.EnumerateArray())
            {
                if (!faixa.TryGetProperty("descricaoFaixa", out var descProp) ||
                    !faixa.TryGetProperty("numeroDeGanhadores", out var ganhadoresProp))
                    continue;

                string descricao = descProp.GetString() ?? "";
                int ganhadores = ganhadoresProp.ValueKind == JsonValueKind.Number ? ganhadoresProp.GetInt32() : 0;

                decimal valorPremio = 0;
                if (faixa.TryGetProperty("valorPremio", out var premioProp) && premioProp.ValueKind == JsonValueKind.Number)
                    valorPremio = premioProp.GetDecimal();

                switch (FaixaDeAcertos(descricao))
                {
                    case 6:
                        concurso.QtdGanhadoresSena = ganhadores;
                        concurso.ValorPremioSena = valorPremio;
                        break;
                    case 5:
                        concurso.QtdGanhadoresQuina = ganhadores;
                        concurso.ValorPremioQuina = valorPremio;
                        break;
                    case 4:
                        concurso.QtdGanhadoresQuadra = ganhadores;
                        concurso.ValorPremioQuadra = valorPremio;
                        break;
                }
            }
        }

        return concurso;
    }

    private static decimal? LerDecimal(JsonElement el, string nome) =>
    el.TryGetProperty(nome, out var p) && p.ValueKind == JsonValueKind.Number
        ? p.GetDecimal() : null;

    private static int? LerInt(JsonElement el, string nome) =>
        el.TryGetProperty(nome, out var p) && p.ValueKind == JsonValueKind.Number
            ? p.GetInt32() : null;

    private static int? FaixaDeAcertos(string descricao)
    {
        var m = Regex.Match(descricao, @"(?<!\d)([456])(?!\d)");
        return m.Success ? int.Parse(m.Groups[1].Value) : null;
    }
}

