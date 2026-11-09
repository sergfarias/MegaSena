using MegaSena.Domain;

namespace MegaSena.Application.Portas;

public interface IGeradorDeSorteio
{
    Jogo Sortear(Universo universo, IRegraDeJogo regra);
    string Nome { get; }
}