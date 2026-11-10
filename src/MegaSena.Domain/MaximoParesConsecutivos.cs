using MegaSena.Domain;

public sealed class MaximoParesConsecutivos : IRegraDeJogo
{
    public int Maximo { get; }
    public MaximoParesConsecutivos(int maximo)
    {
        if (maximo < 0) throw new ArgumentOutOfRangeException(nameof(maximo));
        Maximo = maximo;
    }

    public bool Aceita(Jogo jogo) => jogo.ParesConsecutivos() <= Maximo;
    public string Descricao => $"no máximo {Maximo} par(es) consecutivo(s)";

    //public bool Validar(Jogo jogo)
    //{
    //    // A lógica é a mesma do método Aceita, mas com o nome que a interface exige
    //    return jogo.ParesConsecutivos() <= Maximo;
    //}
}
