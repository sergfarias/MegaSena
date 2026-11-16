
using MegaSena.Application.Portas;
using MegaSena.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace MegaSena.Infrastructure.Repositories;

public class HistoricoDbRepository : IHistoricoRepository
{
    // O ImportadorConcursosService remove esta chave quando entram concursos novos.
    private const string ChaveCacheFrequencias = "freq-dezenas";

    private readonly AppDbContext _context;
    private readonly IMemoryCache _cache;

    public HistoricoDbRepository(AppDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<IReadOnlyList<int[]>> ObterUltimosSorteiosAsync(int quantidade, CancellationToken ct = default)
    {
        if (quantidade <= 0) return Array.Empty<int[]>();

        // Puxa os últimos N concursos do banco (mais recentes primeiro)
        var concursos = await _context.Concursos
            .AsNoTracking()
            .OrderByDescending(c => c.NumeroConcurso)
            .Take(quantidade)
            .Select(c => c.DezenasSorteadas)
            .ToListAsync(ct);

        var resultado = new List<int[]>();

        foreach (var dezenasStr in concursos)
        {
            if (string.IsNullOrWhiteSpace(dezenasStr)) continue;

            var dezenas = dezenasStr
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.Parse(s.Trim()))
                .ToArray();

            resultado.Add(dezenas);
        }

        return resultado;
    }

    public async Task<IReadOnlyList<int>> ObterMenosSorteadasAsync(int quantidade, CancellationToken ct = default)
    {
        if (quantidade <= 0) return Array.Empty<int>();

        // Cache de 1 hora; o importador invalida a chave quando entram concursos novos
        var frequencias = await _cache.GetOrCreateAsync(ChaveCacheFrequencias, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

            var todas = await _context.Concursos
                .AsNoTracking()
                .Select(c => c.DezenasSorteadas)
                .ToListAsync(ct);

            var f = new int[61];
            foreach (var dezenasStr in todas)
            {
                if (string.IsNullOrWhiteSpace(dezenasStr)) continue;
                foreach (var dStr in dezenasStr.Split(','))
                {
                    if (int.TryParse(dStr.Trim(), out int d) && d >= 1 && d <= 60)
                        f[d]++;
                }
            }
            return f;
        });

        return Enumerable.Range(1, 60)
            .OrderBy(d => frequencias![d])
            .Take(quantidade)
            .ToList();
    }
}
