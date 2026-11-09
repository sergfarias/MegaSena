using MegaSena.Application.Models;
using MegaSena.Application.Portas;
using MegaSena.Infrastructure.Data;
using MegaSena.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace MegaSena.Infrastructure.Repositories;

public class MensagemContatoRepository : IMensagemContatoRepository
{
    private readonly AppDbContext _context;

    public MensagemContatoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AdicionarAsync(MensagemContatoDto mensagem)
    {
        var entidade = MapearParaEntidade(mensagem);
        _context.MensagensContato.Add(entidade);
        await _context.SaveChangesAsync();
    }

    public async Task<List<MensagemContatoDto>> ObterNaoLidasAsync()
    {
        var entidades = await _context.MensagensContato
            .Where(m => !m.Lida)
            .OrderByDescending(m => m.DataEnvio)
            .ToListAsync();

        return entidades.Select(MapearParaDto).ToList();
    }

    public async Task<List<MensagemContatoDto>> ObterTodasAsync()
    {
        var entidades = await _context.MensagensContato
            .OrderByDescending(m => m.DataEnvio)
            .ToListAsync();

        return entidades.Select(MapearParaDto).ToList();
    }

    public async Task MarcarComoLidaAsync(Guid id)
    {
        var entidade = await _context.MensagensContato.FindAsync(id);
        if (entidade != null)
        {
            entidade.Lida = true;
            await _context.SaveChangesAsync();
        }
    }

    // ============ MAPEAMENTO ============
    private static MensagemContatoDto MapearParaDto(MensagemContato e) => new()
    {
        Id = e.Id,
        Nome = e.Nome,
        Email = e.Email,
        Mensagem = e.Mensagem,
        DataEnvio = e.DataEnvio,
        Lida = e.Lida
    };

    private static MensagemContato MapearParaEntidade(MensagemContatoDto d) => new()
    {
        Id = d.Id == Guid.Empty ? Guid.NewGuid() : d.Id,
        Nome = d.Nome,
        Email = d.Email,
        Mensagem = d.Mensagem,
        DataEnvio = d.DataEnvio,
        Lida = d.Lida
    };
}