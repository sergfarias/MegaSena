namespace MegaSena.Application.Models;

public sealed record ResultadoUltimoSorteio(
    bool Sucesso,
    int? Concurso = null,
    string? Data = null,
    IReadOnlyList<int>? Dezenas = null,
    string? Erro = null);
