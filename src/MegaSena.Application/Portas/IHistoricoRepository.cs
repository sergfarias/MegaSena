namespace MegaSena.Application.Portas;

public interface IHistoricoRepository
{
    IReadOnlyList<int[]> ObterUltimosSorteios(int quantidade);
    IReadOnlyList<int> ObterMenosSorteadas(int quantidade);

}