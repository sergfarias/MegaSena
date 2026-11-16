namespace MegaSena.Application.Models;

public sealed record UsuarioModel(
    Guid Id,
    string Nome,
    string Login,
    string? Email,
    string SenhaHash,
    bool Ativo,
    string Perfil);
