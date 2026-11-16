namespace MegaSena.Api.Contracts;

public class ParametrosAuditoria
{
    public int[] Dezenas { get; set; } = Array.Empty<int>();
    public int Garantia { get; set; }
    public int UniversoSorteado { get; set; }
    public int MaxPares { get; set; }
}