using MegaSena.Application.Models;

namespace MegaSena.Application.Portas;

public interface IConcursoRepository
{
    /// <summary>Busca um concurso pelo número</summary>
    Task<ConcursoDto?> ObterPorNumeroAsync(int numero);

    /// <summary>Verifica se o concurso já existe (evita duplicatas)</summary>
    Task<bool> ExisteAsync(int numero);

    /// <summary>Salva um novo concurso</summary>
    Task AdicionarAsync(ConcursoDto concurso);

    /// <summary>Salva vários concursos em lote (bulk)</summary>
    Task AdicionarVariosAsync(IEnumerable<ConcursoDto> concursos);

    /// <summary>Retorna o número do último concurso salvo</summary>
    Task<int> ObterUltimoNumeroAsync();

    /// <summary>Retorna os números de todos os concursos salvos (para detectar buracos)</summary>
    Task<List<int>> ObterNumerosAsync();

    /// <summary>Retorna os últimos N concursos (mais recentes primeiro)</summary>
    Task<List<ConcursoDto>> ObterUltimosAsync(int quantidade);

    /// <summary>Retorna todos os concursos (para análise)</summary>
    Task<List<ConcursoDto>> ObterTodosAsync();

    /// <summary>Conta quantos concursos existem no banco</summary>
    Task<int> ContarAsync();
}

