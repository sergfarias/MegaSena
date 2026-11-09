using MegaSena.Domain;
using MegaSena.Application.Models;

namespace MegaSena.Application.Portas;

public interface ILoteRepository
{
    Task SalvarLoteAsync(Guid usuarioId, string hash, string jsonOriginal, IEnumerable<int[]> jogos);
    Task<(IEnumerable<int[]> Jogos, DateTime DataGeracao, string JsonOriginal)?> ObterLotePorHashAsync(string hash);
    Task<(IEnumerable<int[]> Jogos, DateTime DataGeracao, string JsonOriginal)?> ObterLoteDoUsuarioPorHashAsync(Guid usuarioId, string hash);
    Task<IEnumerable<LoteHistoricoModel>> ObterLotesPorUsuarioAsync(Guid usuarioId);
}

