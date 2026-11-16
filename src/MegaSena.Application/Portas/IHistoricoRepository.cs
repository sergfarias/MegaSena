
namespace MegaSena.Application.Portas;

public interface IHistoricoRepository
{
    Task<IReadOnlyList<int[]>> ObterUltimosSorteiosAsync(int quantidade, CancellationToken ct = default);
    Task<IReadOnlyList<int>> ObterMenosSorteadasAsync(int quantidade, CancellationToken ct = default);
}
