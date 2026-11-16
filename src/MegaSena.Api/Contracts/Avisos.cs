namespace MegaSena.Api.Contracts;

public static class Avisos
{
    public const string Filtros =
        "Excluir dezenas dos últimos sorteios não altera a chance de acerto: os sorteios são independentes. " +
        "O filtro reduz o universo (e o custo), mas reduz na mesma proporção a chance de as 6 dezenas caírem nele.";

    public const string GloboFisico =
        "No modo físico, as dezenas saem de uma simulação de física (bolas num globo com jato de ar e colisões). " +
        "É uma simulação, não o sorteio oficial, e não altera a chance de acerto.";
}
