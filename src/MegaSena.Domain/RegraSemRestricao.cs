namespace MegaSena.Domain;

public sealed class RegraSemRestricao : IRegraDeJogo
{
    public bool Aceita(Jogo jogo) => true;
    //public bool Validar(Jogo jogo) => true;
    public string Descricao => "sem restrição";
}