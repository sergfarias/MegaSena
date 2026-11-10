
namespace MegaSena.Application.Portas;

public interface IGeradorDeSorteioFactory
{
    IGeradorDeSorteio Criar(bool fisico);
}