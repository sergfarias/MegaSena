using System.ComponentModel.DataAnnotations;
using System.Text;

namespace MegaSena.Api.Models;

/// <summary>Valida tamanho em caracteres (mín. 8) e em bytes UTF-8 (máx. 72, limite do BCrypt) e bloqueia senhas triviais.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SenhaValidaAttribute : ValidationAttribute
{
    private static readonly HashSet<string> Comuns = new(StringComparer.OrdinalIgnoreCase)
    {
        "12345678", "123456789", "1234567890", "qwertyui", "password",
        "password1", "senha123", "abcd1234", "11111111"
    };

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string s) return ValidationResult.Success; // [Required] cuida do vazio

        if (s.Length < 8)
            return new ValidationResult("A senha deve ter pelo menos 8 caracteres.");

        if (Encoding.UTF8.GetByteCount(s) > 72)
            return new ValidationResult("A senha deve ter no máximo 72 bytes.");

        if (s.Distinct().Count() == 1 || Comuns.Contains(s))
            return new ValidationResult("Escolha uma senha menos previsível.");

        return ValidationResult.Success;
    }
}