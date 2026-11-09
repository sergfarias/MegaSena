namespace MegaSena.Infrastructure.Entities;

public class Perfil
{
    public Guid Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? Sigla { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    // Relacionamento
    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}