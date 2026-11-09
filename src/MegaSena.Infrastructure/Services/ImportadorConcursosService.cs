using MegaSena.Application.Models;
using MegaSena.Application.Portas;
using MegaSena.Domain;
using MegaSena.Domain.Calculadora;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Globalization; // <-- Necessário para o ParseExact
using System.Text.Json;

namespace MegaSena.Infrastructure.Services;

public class ImportadorConcursosService : BackgroundService
{
    private const string UrlBase = "https://api.guidi.dev.br/loteria/megasena";
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ImportadorConcursosService> _log;

    public ImportadorConcursosService(
        IServiceScopeFactory scopes,
        ILogger<ImportadorConcursosService> log)
    {
        _scopes = scopes;
        _log = log;
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

    private async Task ImportarConcursosAsync(CancellationToken ct)
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
                continue;
            }

            lote.Add(c);

            // Salva a cada 50 concursos. Erro de banco sobe para o ExecuteAsync (retry curto).
            if (lote.Count >= tamanhoLote)
            {
                await repo.AdicionarVariosAsync(lote);
                _log.LogInformation("Salvos {Qtd} concursos no banco.", lote.Count);
                lote.Clear();
            }
        }

        if (lote.Count > 0)
        {
            await repo.AdicionarVariosAsync(lote);
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
            double chi2 = CalculadoraQuiQuadrado.Calcular(freq, n, universo.Tamanho);
            int gl = universo.Tamanho - 1;
            double p = DistribuicaoQuiQuadrado.PValor(chi2, gl);

            _log.LogInformation(
                "📊 Qui-Quadrado com {N} concursos: χ²={Chi2:F2}, gl={Gl}, p-valor={P:F4} ({Resultado})",
                n, chi2, gl, p,
                p >= 0.05 ? "✅ Sem evidência de viés" : "⚠️ Possível viés detectado");
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
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        try
        {
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
                ValorArrecadado = root.TryGetProperty("valorArrecadado", out var arr) ? arr.GetDecimal() : null,
                Acumulado = root.TryGetProperty("acumulado", out var ac) && ac.GetBoolean(),
                ValorAcumulado = root.TryGetProperty("valorAcumuladoConcursoEspecial", out var va) ? va.GetDecimal() : null,
                DataProximoConcurso = dataProximo,
                NumeroConcursoAnterior = root.TryGetProperty("numeroConcursoAnterior", out var ant) ? ant.GetInt32() : null,
                ProximoConcurso = root.TryGetProperty("proximoConcurso", out var prox) ? prox.GetInt32() : null,
                ListaRateioPremioJson = root.TryGetProperty("listaRateioPremio", out var rateio)
                    ? rateio.GetRawText()
                    : null
            };

            // =========================================================
            // NOVO: Extrair os ganhadores e prêmios do JSON de rateio
            // =========================================================
            if (root.TryGetProperty("listaRateioPremio", out var rateioList) && rateioList.ValueKind == JsonValueKind.Array)
            {
                foreach (var faixa in rateioList.EnumerateArray())
                {
                    if (!faixa.TryGetProperty("descricaoFaixa", out var descProp) || !faixa.TryGetProperty("numeroDeGanhadores", out var ganhadoresProp))
                        continue;

                    string descricao = descProp.GetString() ?? "";
                    int ganhadores = ganhadoresProp.GetInt32();

                    decimal valorPremio = 0;
                    if (faixa.TryGetProperty("valorPremio", out var premioProp))
                        valorPremio = premioProp.GetDecimal();

                    // Mapeia para a faixa correta (baseado no padrão da API da Caixa)
                    if (descricao.Contains("6", StringComparison.OrdinalIgnoreCase))
                    {
                        concurso.QtdGanhadoresSena = ganhadores;
                        concurso.ValorPremioSena = valorPremio;
                    }
                    else if (descricao.Contains("5", StringComparison.OrdinalIgnoreCase))
                    {
                        concurso.QtdGanhadoresQuina = ganhadores;
                        concurso.ValorPremioQuina = valorPremio;
                    }
                    else if (descricao.Contains("4", StringComparison.OrdinalIgnoreCase))
                    {
                        concurso.QtdGanhadoresQuadra = ganhadores;
                        concurso.ValorPremioQuadra = valorPremio;
                    }
                }
            }

            return concurso;
        }
        catch (Exception ex)
        {
            _log.LogWarning("Erro ao mapear concurso #{Num}: {Msg}", numero, ex.Message);
            return null;
        }
    }
}

