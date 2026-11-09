namespace MegaSena.Application.Models;

public class LoteHistoricoModel
{
    public Guid Id { get; set; }
    public string Hash { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
    public int QuantidadeJogos { get; set; }
    public string JsonOriginal { get; set; } = string.Empty;
}