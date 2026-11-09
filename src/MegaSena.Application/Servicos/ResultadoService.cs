using System.Net.Http.Json;
using System.Text.Json;

namespace MegaSena.Application.Servicos;

public class ResultadoService
{
    private readonly HttpClient _httpClient;

    public ResultadoService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<object> ObterUltimoSorteioAsync()
    {
        try
        {
            // Faz a chamada para a API externa
            var response = await _httpClient.GetAsync("https://api.guidi.dev.br/loteria/megasena/ultimo");

            if (!response.IsSuccessStatusCode)
                throw new Exception("API externa indisponível.");

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Extrai as informações relevantes
            var numero = root.GetProperty("numero").GetInt32();
            var dataApuracao = root.GetProperty("dataApuracao").GetString();

            var dezenas = new List<int>();
            foreach (var d in root.GetProperty("listaDezenas").EnumerateArray())
            {
                dezenas.Add(int.Parse(d.GetString()!));
            }

            return new
            {
                Concurso = numero,
                Data = dataApuracao,
                Dezenas = dezenas,
                Sucesso = true
            };
        }
        catch (Exception)
        {
            return new
            {
                Sucesso = false,
                Erro = "Não foi possível obter o último sorteio no momento."
            };
        }
    }
}

