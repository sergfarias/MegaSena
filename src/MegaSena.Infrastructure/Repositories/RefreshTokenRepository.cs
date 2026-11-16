
using MegaSena.Application.Models;
using MegaSena.Application.Portas;
using MegaSena.Infrastructure.Data;
using MegaSena.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace MegaSena.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;

    public RefreshTokenRepository(AppDbContext context) => _context = context;

    public async Task AdicionarAsync(Guid usuarioId, Guid familiaId, string tokenHash, string carimboSenha, DateTime expiraEmUtc)
    {
        var agora = DateTime.UtcNow;

        // Faxina barata: apaga os tokens deste usuário vencidos há mais de 7 dias
        var limite = agora.AddDays(-7);
        var velhos = await _context.RefreshTokens
            .Where(t => t.UsuarioId == usuarioId && t.ExpiraEmUtc < limite)
            .ToListAsync();
        _context.RefreshTokens.RemoveRange(velhos);

        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            FamiliaId = familiaId,
            TokenHash = tokenHash,
            CarimboSenha = carimboSenha,
            CriadoEmUtc = agora,
            ExpiraEmUtc = expiraEmUtc
        });

        await _context.SaveChangesAsync();
    }

    public async Task<RefreshTokenModel?> ObterPorHashAsync(string tokenHash)
    {
        var t = await _context.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(x => x.TokenHash == tokenHash);
        return t == null
            ? null
            : new RefreshTokenModel(t.Id, t.UsuarioId, t.FamiliaId, t.CarimboSenha, t.ExpiraEmUtc, t.UsadoEmUtc, t.RevogadoEmUtc);
    }

    public async Task<bool> TentarConsumirAsync(Guid id, DateTime agoraUtc)
    {
        var t = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.Id == id);
        if (t == null || t.UsadoEmUtc != null || t.RevogadoEmUtc != null) return false;

        t.UsadoEmUtc = agoraUtc;
        try
        {
            // UsadoEmUtc é token de concorrência: se dois pedidos tentarem ao mesmo tempo, só um grava.
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    public async Task RevogarFamiliaAsync(Guid familiaId, DateTime agoraUtc)
    {
        var tokens = await _context.RefreshTokens
            .Where(t => t.FamiliaId == familiaId && t.RevogadoEmUtc == null)
            .ToListAsync();

        foreach (var t in tokens)
            t.RevogadoEmUtc = agoraUtc;

        if (tokens.Count > 0)
            await _context.SaveChangesAsync();
    }
}

