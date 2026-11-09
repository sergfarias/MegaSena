namespace MegaSena.Infrastructure.Entities;

public class JogoEntity
{
    public Guid Id { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string JsonOriginal { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    // Relacionamento: Um Jogo tem muitos Itens
    public List<JogoItemEntity> Itens { get; set; } = new();

    public Guid? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
}