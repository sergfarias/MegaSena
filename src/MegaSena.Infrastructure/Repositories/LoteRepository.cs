
using MegaSena.Application.Models;
using MegaSena.Application.Portas;
using MegaSena.Infrastructure.Data;
using MegaSena.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace MegaSena.Infrastructure.Repositories;

public class LoteRepository : ILoteRepository
{
    private readonly AppDbContext _context;

    public LoteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task SalvarLoteAsync(
        Guid usuarioId,
        string hash,
        string jsonOriginal,
        IEnumerable<int[]> jogos)
    {
        var novoJogo = new JogoEntity
        {
            Id = Guid.NewGuid(),
            Hash = hash,
            JsonOriginal = jsonOriginal,
            DataCriacao = DateTime.UtcNow,
            UsuarioId = usuarioId,
            Ativo = true
        };

        foreach (var jogoArray in jogos)
        {
            novoJogo.Itens.Add(new JogoItemEntity
            {
                Id = Guid.NewGuid(),
                Dezenas = string.Join(",", jogoArray.Select(d => d.ToString("00"))),
                DataCriacao = DateTime.UtcNow,
                Ativo = true
            });
        }

        _context.Jogos.Add(novoJogo);
        await _context.SaveChangesAsync();
    }

    public async Task<(IEnumerable<int[]> Jogos, DateTime DataGeracao, string JsonOriginal)?> ObterLotePorHashAsync(string hash)
    {
        var lote = await _context.Jogos
            .AsNoTracking()
            .Where(j => j.Hash == hash && j.Ativo)
            .Select(j => new
            {
                j.DataCriacao,
                j.JsonOriginal,
                Itens = j.Itens
                    .Where(i => i.Ativo)
                    .Select(i => i.Dezenas)
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (lote == null)
            return null;

        var jogos = lote.Itens
            .Select(dezenas => dezenas.Split(',').Select(int.Parse).ToArray())
            .ToList();

        return (jogos, lote.DataCriacao, lote.JsonOriginal);
    }

    public async Task<(IEnumerable<int[]> Jogos, DateTime DataGeracao, string JsonOriginal)?> ObterLoteDoUsuarioPorHashAsync(
        Guid usuarioId,
        string hash)
    {
        var lote = await _context.Jogos
            .AsNoTracking()
            .Where(j => j.Hash == hash
                     && j.UsuarioId == usuarioId
                     && j.Ativo)
            .Select(j => new
            {
                j.DataCriacao,
                j.JsonOriginal,
                Itens = j.Itens
                    .Where(i => i.Ativo)
                    .Select(i => i.Dezenas)
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (lote == null)
            return null;

        var jogos = lote.Itens
            .Select(dezenas => dezenas.Split(',').Select(int.Parse).ToArray())
            .ToList();

        return (jogos, lote.DataCriacao, lote.JsonOriginal);
    }

    public async Task<IEnumerable<LoteHistoricoModel>> ObterLotesPorUsuarioAsync(Guid usuarioId)
    {
        return await _context.Jogos
            .AsNoTracking()
            .Where(j => j.UsuarioId == usuarioId && j.Ativo)
            .OrderByDescending(j => j.DataCriacao)
            .Select(j => new LoteHistoricoModel
            {
                Id = j.Id,
                Hash = j.Hash,
                DataCriacao = j.DataCriacao,
                Ativo = j.Ativo,
                QuantidadeJogos = j.Itens.Count(i => i.Ativo),
                JsonOriginal = string.Empty
            })
            .ToListAsync();
    }
}
