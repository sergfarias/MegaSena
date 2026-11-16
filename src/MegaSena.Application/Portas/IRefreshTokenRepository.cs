
using MegaSena.Application.Models;

namespace MegaSena.Application.Portas;

public interface IRefreshTokenRepository
{
    /// <summary>Guarda o hash (nunca o token) de um refresh token novo.</summary>
    Task AdicionarAsync(Guid usuarioId, Guid familiaId, string tokenHash, string carimboSenha, DateTime expiraEmUtc);

    Task<RefreshTokenModel?> ObterPorHashAsync(string tokenHash);

    /// <summary>Marca o token como usado. Devolve false se já estava usado/revogado ou se outro pedido o usou antes.</summary>
    Task<bool> TentarConsumirAsync(Guid id, DateTime agoraUtc);

    /// <summary>Revoga todos os tokens da família (cadeia de renovações de um mesmo login).</summary>
    Task RevogarFamiliaAsync(Guid familiaId, DateTime agoraUtc);
}
