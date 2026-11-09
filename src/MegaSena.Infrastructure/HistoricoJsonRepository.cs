using System.Text.Json;
using MegaSena.Application.Portas;

namespace MegaSena.Infrastructure;

public sealed class HistoricoJsonRepository : IHistoricoRepository
{
    private readonly List<int[]> _ultimos = new();
    private readonly List<int> _menos = new();

    public HistoricoJsonRepository(string caminhoArquivo)
    {
        if (!File.Exists(caminhoArquivo)) return;

        var json = File.ReadAllText(caminhoArquivo);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("ultimosSorteios", out var propUltimos))
        {
            foreach (var arr in propUltimos.EnumerateArray())
                _ultimos.Add(arr.EnumerateArray().Select(x => x.GetInt32()).ToArray());
        }

        if (root.TryGetProperty("menosSorteadas", out var propMenos))
        {
            foreach (var el in propMenos.EnumerateArray())
                _menos.Add(el.GetInt32());
        }
    }

    public IReadOnlyList<int[]> ObterUltimosSorteios(int quantidade)
        => _ultimos.Take(quantidade).ToList();

    public IReadOnlyList<int> ObterMenosSorteadas(int quantidade)
        => _menos.Take(quantidade).ToList();
}