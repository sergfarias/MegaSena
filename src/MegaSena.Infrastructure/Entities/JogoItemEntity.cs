namespace MegaSena.Infrastructure.Entities;

public class JogoItemEntity
{
    public Guid Id { get; set; }
    public Guid JogoId { get; set; }
    public string Dezenas { get; set; } = string.Empty; // Ex: "01,05,12,25,35,48"
    public bool Ativo { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    // Propriedade de navegação para o Jogo "pai"
    public JogoEntity? Jogo { get; set; }
}