namespace MegaSena.Api.Contracts;

public class AuditoriaPayload
{
    public ParametrosAuditoria Parametros { get; set; } = new();
    public List<int[]> Jogos { get; set; } = new();
    public Guid Nonce { get; set; }
    public DateTime CriadoEmUtc { get; set; }
}
