
namespace MegaSena.Infrastructure.Entities;

public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }

    /// <summary>Todos os tokens que nasceram de um mesmo login compartilham a família.</summary>
    public Guid FamiliaId { get; set; }

    /// <summary>SHA-256 do token (hex, 64 caracteres). O token em si nunca é guardado.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>Carimbo da senha no momento da emissão: se a senha mudar, o token deixa de valer.</summary>
    public string CarimboSenha { get; set; } = string.Empty;

    public DateTime CriadoEmUtc { get; set; }
    public DateTime ExpiraEmUtc { get; set; }

    /// <summary>Preenchido quando o token foi trocado por um novo (rotação). Reuso depois disso indica roubo.</summary>
    public DateTime? UsadoEmUtc { get; set; }

    public DateTime? RevogadoEmUtc { get; set; }
}
