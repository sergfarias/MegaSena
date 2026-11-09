namespace MegaSena.Domain;

public sealed record ResultadoSorteio(Jogo Jogo, Universo Universo, bool Fisico)
{
    public override string ToString() =>
        $"{Jogo}  (universo: {Universo.Tamanho} dezenas, {(Fisico ? "físico" : "rápido")})";
}