using System.ComponentModel.DataAnnotations;

namespace MegaSena.Api.Models;

public class EsqueciSenhaRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetarSenhaRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, MinLength(8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
    public string NovaSenha { get; set; } = string.Empty;
}

// NOVOS DTOs movidos para cá com validação
public class RegistroRequest
{
    [Required, MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Login { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
    public string Senha { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required]
    public string Login { get; set; } = string.Empty;

    [Required]
    public string Senha { get; set; } = string.Empty;
}