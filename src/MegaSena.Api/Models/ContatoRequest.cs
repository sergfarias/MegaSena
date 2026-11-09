using System.ComponentModel.DataAnnotations;

namespace MegaSena.Api.Models;

public class ContatoRequest
{
    [Required, MaxLength(200)]
    public string Nome { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Mensagem { get; set; } = string.Empty;
}