namespace MegaSena.Domain.Cobertura;

public sealed record ResultadoCobertura(
    Cobertura Cobertura,
    int TotalJogos,
    long TotalCombinacoesSorteaveis,
    long CombinacoesCobertas,
    bool GarantiaTotal,
    decimal CustoTotal,
    bool MinimoProvado); 