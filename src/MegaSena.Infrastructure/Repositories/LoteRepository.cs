using MegaSena.Application.Models;
using MegaSena.Application.Portas;
using MegaSena.Infrastructure.Data;
using MegaSena.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MegaSena.Infrastructure.Repositories;

public class LoteRepository : ILoteRepository
{
    private readonly AppDbContext _context;

    public LoteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task SalvarLoteAsync(Guid usuarioId, string hash, string jsonOriginal, IEnumerable<int[]> jogos)
    {
        var novoJogo = new JogoEntity
        {
            Id = Guid.NewGuid(),
            Hash = hash,
            JsonOriginal = jsonOriginal,
            DataCriacao = DateTime.UtcNow,
            UsuarioId = usuarioId
        };

        foreach (var jogoArray in jogos)
        {
            novoJogo.Itens.Add(new JogoItemEntity
            {
                Id = Guid.NewGuid(),
                Dezenas = string.Join(",", jogoArray.Select(d => d.ToString("00"))),
                DataCriacao = DateTime.UtcNow
            });
        }

        _context.Jogos.Add(novoJogo);
        await _context.SaveChangesAsync();
    }

    public async Task<(IEnumerable<int[]> Jogos, DateTime DataGeracao, string JsonOriginal)?> ObterLotePorHashAsync(string hash)
    {
        var lote = await _context.Jogos.Include(j => j.Itens).FirstOrDefaultAsync(j => j.Hash == hash);
        if (lote == null) return null;

        var jogos = lote.Itens.Select(item => item.Dezenas.Split(',').Select(int.Parse).ToArray()).ToList();

        // Retornamos o JsonOriginal salvo no banco
        return (jogos, lote.DataCriacao, lote.JsonOriginal);
    }

    public async Task<(IEnumerable<int[]> Jogos, DateTime DataGeracao, string JsonOriginal)?> ObterLoteDoUsuarioPorHashAsync(Guid usuarioId, string hash)
    {
        // Uma única query: o filtro por UsuarioId garante a posse
        var lote = await _context.Jogos
            .AsNoTracking()
            .Include(j => j.Itens)
            .FirstOrDefaultAsync(j => j.Hash == hash && j.UsuarioId == usuarioId);
        if (lote == null) return null;

        var jogos = lote.Itens.Select(item => item.Dezenas.Split(',').Select(int.Parse).ToArray()).ToList();
        return (jogos, lote.DataCriacao, lote.JsonOriginal);
    }

    public async Task<IEnumerable<LoteHistoricoModel>> ObterLotesPorUsuarioAsync(Guid usuarioId)
    {
        // 1. Busca a Entidade no banco
        var lotesDoBanco = await _context.Jogos
            .Include(j => j.Itens)
            .Where(j => j.UsuarioId == usuarioId)
            .OrderByDescending(j => j.DataCriacao)
            .ToListAsync();

        // 2. Converte (Mapeia) de Entidade para o Model da Application
        return lotesDoBanco.Select(l => new LoteHistoricoModel
        {
            Id = l.Id,
            Hash = l.Hash,
            DataCriacao = l.DataCriacao,
            QuantidadeJogos = l.Itens.Count,
            JsonOriginal = l.JsonOriginal
        }).ToList();
    }
}

