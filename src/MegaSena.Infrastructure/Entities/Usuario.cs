namespace MegaSena.Infrastructure.Entities;

public class Usuario
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? CPF { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string Login { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public string? Salt { get; set; }
    public string? SenhaInicial { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    // Chave Estrangeira
    public Guid? PerfilId { get; set; }
    public Perfil? Perfil { get; set; }

    // Relacionamento (Um usuário tem vários jogos)
    public ICollection<JogoEntity> Jogos { get; set; } = new List<JogoEntity>();

    // Recuperação de senha (temporários, apagados após uso)
    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpiraEm { get; set; }
}