namespace MegaSena.Domain.Cobertura;

public sealed record Cobertura(
    IReadOnlySet<int> Dezenas,      // ex.: {1..12}
    int TamanhoJogo,                // 6
    int Garantia,                   // t: 4, 5 ou 6
    int UniversoSorteado,           // k: quantas dezenas são sorteadas (4,5,6)
    IReadOnlyList<Jogo> Jogos,      // solução
    bool MinimoProvado = false);    // <-- NOVO CAMPO (com valor padrão)