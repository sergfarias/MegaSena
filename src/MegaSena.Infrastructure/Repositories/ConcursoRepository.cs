using MegaSena.Application.Models;
using MegaSena.Application.Portas;
using MegaSena.Infrastructure.Data;
using MegaSena.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace MegaSena.Infrastructure.Repositories;

public class ConcursoRepository : IConcursoRepository
{
    private readonly AppDbContext _context;

    public ConcursoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ConcursoDto?> ObterPorNumeroAsync(int numero)
    {
        var entidade = await _context.Concursos.FirstOrDefaultAsync(c => c.NumeroConcurso == numero);
        return entidade == null ? null : MapearParaDto(entidade);
    }

    public async Task<bool> ExisteAsync(int numero)
        => await _context.Concursos.AnyAsync(c => c.NumeroConcurso == numero);

    public async Task AdicionarAsync(ConcursoDto concurso)
    {
        var entidade = MapearParaEntidade(concurso);
        _context.Concursos.Add(entidade);
        await _context.SaveChangesAsync();
    }

    public async Task AdicionarVariosAsync(IEnumerable<ConcursoDto> concursos)
    {
        var entidades = concursos.Select(MapearParaEntidade);
        await _context.Concursos.AddRangeAsync(entidades);
        await _context.SaveChangesAsync();
    }

    public async Task<int> ObterUltimoNumeroAsync()
    {
        var ultimo = await _context.Concursos
            .OrderByDescending(c => c.NumeroConcurso)
            .FirstOrDefaultAsync();
        return ultimo?.NumeroConcurso ?? 0;
    }

    public async Task<List<int>> ObterNumerosAsync()
    {
        return await _context.Concursos
            .AsNoTracking()
            .Select(c => c.NumeroConcurso)
            .ToListAsync();
    }

    public async Task<List<ConcursoDto>> ObterUltimosAsync(int quantidade)
    {
        var entidades = await _context.Concursos
            .OrderByDescending(c => c.NumeroConcurso)
            .Take(quantidade)
            .ToListAsync();

        return entidades.Select(MapearParaDto).ToList();
    }

    public async Task<List<ConcursoDto>> ObterTodosAsync()
    {
        var entidades = await _context.Concursos
            .OrderBy(c => c.NumeroConcurso)
            .ToListAsync();

        return entidades.Select(MapearParaDto).ToList();
    }

    public async Task<int> ContarAsync()
        => await _context.Concursos.CountAsync();

    // ============ MÉTODOS PRIVADOS DE MAPEAMENTO ============

    private static ConcursoDto MapearParaDto(Concurso e) => new()
    {
        NumeroConcurso = e.NumeroConcurso,
        DataHoraSorteio = e.DataHoraSorteio,
        CidadeSorteio = e.CidadeSorteio,
        UfSorteio = e.UfSorteio,
        DezenasSorteadas = e.DezenasSorteadas,
        DezenasOrdemSorteio = e.DezenasOrdemSorteio,
        QtdGanhadoresSena = e.QtdGanhadoresSena,
        QtdGanhadoresQuina = e.QtdGanhadoresQuina,
        QtdGanhadoresQuadra = e.QtdGanhadoresQuadra,
        ValorPremioSena = e.ValorPremioSena,
        ValorPremioQuina = e.ValorPremioQuina,
        ValorPremioQuadra = e.ValorPremioQuadra,
        ValorArrecadado = e.ValorArrecadado,
        Acumulado = e.Acumulado,
        ValorAcumulado = e.ValorAcumulado,
        ListaRateioPremioJson = e.ListaRateioPremioJson,
        NumeroConcursoAnterior = e.NumeroConcursoAnterior,
        ProximoConcurso = e.ProximoConcurso,
        DataProximoConcurso = e.DataProximoConcurso
    };

    private static Concurso MapearParaEntidade(ConcursoDto d) => new()
    {
        NumeroConcurso = d.NumeroConcurso,
        DataHoraSorteio = d.DataHoraSorteio,
        CidadeSorteio = d.CidadeSorteio,
        UfSorteio = d.UfSorteio,
        DezenasSorteadas = d.DezenasSorteadas,
        DezenasOrdemSorteio = d.DezenasOrdemSorteio,
        QtdGanhadoresSena = d.QtdGanhadoresSena,
        QtdGanhadoresQuina = d.QtdGanhadoresQuina,
        QtdGanhadoresQuadra = d.QtdGanhadoresQuadra,
        ValorPremioSena = d.ValorPremioSena,
        ValorPremioQuina = d.ValorPremioQuina,
        ValorPremioQuadra = d.ValorPremioQuadra,
        ValorArrecadado = d.ValorArrecadado,
        Acumulado = d.Acumulado,
        ValorAcumulado = d.ValorAcumulado,
        ListaRateioPremioJson = d.ListaRateioPremioJson,
        NumeroConcursoAnterior = d.NumeroConcursoAnterior,
        ProximoConcurso = d.ProximoConcurso,
        DataProximoConcurso = d.DataProximoConcurso,
        DataImportacao = DateTime.UtcNow
    };
}

