namespace MegaSena.Api.Models;

public class FechamentoRequest
{
    /// <summary>Lista de dezenas escolhidas (ex: as 42 dezenas do universo reduzido)</summary>
    public List<int> Dezenas { get; set; } = new();

    /// <summary>Garantia de acerto (4 = Quadra, 5 = Quina, 6 = Sena)</summary>
    public int Garantia { get; set; } = 4;

    /// <summary>Quantidade de dezenas que serão sorteadas (normalmente 6 na Mega-Sena)</summary>
    public int UniversoSorteado { get; set; } = 6;

    /// <summary>Máximo de pares consecutivos permitidos (filtro de padrões)</summary>
    public int MaxPares { get; set; } = 1;
}