namespace MegaSena.Domain;

public interface IRegraDeJogo
{
    bool Aceita(Jogo jogo);
    string Descricao { get; }

    bool Validar(Jogo jogo);
}