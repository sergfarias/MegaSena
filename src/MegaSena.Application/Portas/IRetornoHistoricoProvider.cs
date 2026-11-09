// Portas/IRetornoHistoricoProvider.cs
namespace MegaSena.Application.Portas;

public sealed record TaxaRetorno(decimal Taxa, int Concursos, DateTime AtualizadoEmUtc);

public interface IRetornoHistoricoProvider
{
    TaxaRetorno? Atual { get; }          // null enquanto o job não rodou ou se a API falhou
    void Atualizar(TaxaRetorno taxa);
}