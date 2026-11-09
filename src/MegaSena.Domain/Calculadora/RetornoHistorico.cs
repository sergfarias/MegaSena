namespace MegaSena.Domain.Calculadora;

public sealed record ConcursoFinanceiro(decimal Arrecadado, decimal PremiosPagos);

public static class RetornoHistorico
{
    public static decimal Taxa(IEnumerable<ConcursoFinanceiro> concursos)
    {
        var lista = concursos.Where(c => c.Arrecadado > 0).ToList();
        if (lista.Count == 0) return 0m;
        return lista.Sum(c => c.PremiosPagos) / lista.Sum(c => c.Arrecadado);
    }
}