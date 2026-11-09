using MegaSena.Application.Portas;
using MegaSena.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MegaSena.Infrastructure.Repositories;

public class HistoricoDbRepository : IHistoricoRepository
{
    private readonly AppDbContext _context;

    public HistoricoDbRepository(AppDbContext context)
    {
        _context = context;
    }

    public IReadOnlyList<int[]> ObterUltimosSorteios(int quantidade)
    {
        if (quantidade <= 0) return Array.Empty<int[]>();

        // Puxa os últimos N concursos do banco (mais recentes primeiro)
        var concursos = _context.Concursos
            .OrderByDescending(c => c.NumeroConcurso)
            .Take(quantidade)
            .Select(c => c.DezenasSorteadas)
            .ToList(); // Executa a query no banco aqui

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

    public IReadOnlyList<int> ObterMenosSorteadas(int quantidade)
    {
        if (quantidade <= 0) return Array.Empty<int>();

        // Busca todas as dezenas de todos os concursos salvos
        var todasDezenas = _context.Concursos
            .Select(c => c.DezenasSorteadas)
            .ToList();

        // Conta a frequência de cada dezena (1 a 60)
        var frequencias = new int[61];
        foreach (var dezenasStr in todasDezenas)
        {
            if (string.IsNullOrWhiteSpace(dezenasStr)) continue;
            foreach (var dStr in dezenasStr.Split(','))
            {
                if (int.TryParse(dStr.Trim(), out int d) && d >= 1 && d <= 60)
                    frequencias[d]++;
            }
        }

        // Retorna as N dezenas que menos saíram (ordenadas pela frequência)
        return Enumerable.Range(1, 60)
            .OrderBy(d => frequencias[d])
            .Take(quantidade)
            .ToList();
    }
}