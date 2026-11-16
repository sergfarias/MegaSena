using MegaSena.Application.Models;
using MegaSena.Application.Portas;
using MegaSena.Infrastructure.Data;
using MegaSena.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace MegaSena.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AppDbContext _context;

    public UsuarioRepository(AppDbContext context) => _context = context;

    public Task<bool> LoginExisteAsync(string login) =>
        _context.Usuarios.AnyAsync(u => u.Login == login);

    public Task<bool> EmailExisteAsync(string email) =>
        _context.Usuarios.AnyAsync(u => u.Email == email);

    public async Task<UsuarioModel?> ObterPorLoginAsync(string login)
    {
        var u = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(x => x.Login == login);
        return u == null ? null : await ParaModeloAsync(u);
    }

    public async Task<UsuarioModel?> ObterPorEmailAsync(string email)
    {
        var u = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email);
        return u == null ? null : await ParaModeloAsync(u);
    }

    public async Task<bool> CriarAsync(string nome, string login, string email, string senhaHash)
    {
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Login = login,
            Email = email,
            SenhaHash = senhaHash,
            Ativo = true,
            DataCriacao = DateTime.UtcNow
        };

        try
        {
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    public async Task DefinirTokenResetAsync(Guid usuarioId, string tokenHash, DateTime expiraEmUtc)
    {
        var usuario = await _context.Usuarios.FindAsync(usuarioId);
        if (usuario == null) return;

        usuario.ResetToken = tokenHash;
        usuario.ResetTokenExpiraEm = expiraEmUtc;
        await _context.SaveChangesAsync();
    }

    public async Task<bool> RedefinirSenhaAsync(string tokenHash, string novaSenhaHash, DateTime agoraUtc)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.ResetToken == tokenHash);

        if (usuario == null || usuario.ResetTokenExpiraEm == null || usuario.ResetTokenExpiraEm < agoraUtc)
            return false;

        usuario.SenhaHash = novaSenhaHash;
        usuario.ResetToken = null;
        usuario.ResetTokenExpiraEm = null;
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<UsuarioModel> ParaModeloAsync(Usuario u)
    {
        var perfil = "Cliente";
        if (u.PerfilId != null)
        {
            var p = await _context.Perfis.FindAsync(u.PerfilId);
            if (p != null) perfil = p.Sigla ?? "Cliente";
        }

        return new UsuarioModel(u.Id, u.Nome, u.Login, u.Email, u.SenhaHash, u.Ativo, perfil);
    }

    public async Task<UsuarioModel?> ObterPorIdAsync(Guid id)
    {
        var u = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return u == null ? null : await ParaModeloAsync(u);
    }
}
