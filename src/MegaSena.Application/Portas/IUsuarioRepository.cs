using MegaSena.Application.Models;

namespace MegaSena.Application.Portas;

public interface IUsuarioRepository
{
    Task<bool> LoginExisteAsync(string login);
    Task<bool> EmailExisteAsync(string email);
    Task<UsuarioModel?> ObterPorLoginAsync(string login);
    Task<UsuarioModel?> ObterPorEmailAsync(string email);
    Task<UsuarioModel?> ObterPorIdAsync(Guid id);

    /// <summary>Cria o usuário. Devolve false se o banco recusar (por exemplo, corrida em login/e-mail únicos).</summary>
    Task<bool> CriarAsync(string nome, string login, string email, string senhaHash);

    Task DefinirTokenResetAsync(Guid usuarioId, string tokenHash, DateTime expiraEmUtc);

    /// <summary>Troca a senha se o token existir e não estiver expirado; invalida o token. Devolve false caso contrário.</summary>
    Task<bool> RedefinirSenhaAsync(string tokenHash, string novaSenhaHash, DateTime agoraUtc);
}
