namespace MegaSena.Infrastructure.Entities;

public class MensagemContato
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public DateTime DataEnvio { get; set; } = DateTime.UtcNow;
    public bool Lida { get; set; } = false;
}