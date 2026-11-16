
namespace MegaSena.Application.Models;

public sealed record RefreshTokenModel(
    Guid Id,
    Guid UsuarioId,
    Guid FamiliaId,
    string CarimboSenha,
    DateTime ExpiraEmUtc,
    DateTime? UsadoEmUtc,
    DateTime? RevogadoEmUtc);