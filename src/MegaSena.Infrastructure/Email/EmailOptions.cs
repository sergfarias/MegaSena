namespace MegaSena.Infrastructure.Email;

public sealed class EmailOptions
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string? User { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "no-reply@megasena.local";
    public string FromName { get; set; } = "MegaSena SaaS";

    /// <summary>Endereço que recebe o aviso de novas mensagens do formulário de contato (opcional).</summary>
    public string? AdminAddress { get; set; }
}
