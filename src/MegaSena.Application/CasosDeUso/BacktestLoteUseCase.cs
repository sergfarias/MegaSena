using MegaSena.Application.Models;
using MegaSena.Domain;
using MegaSena.Domain.Calculadora; // <-- Adicionado

namespace MegaSena.Application.CasosDeUso;

public sealed record ResultadoBacktest(
    int Janela,
    int TotalJogosLote,
    decimal CustoTotal,
    int Quadras,
    int Quinas,
    int Senas,
    List<DetalheConcursoBacktest> Detalhes);

public sealed record DetalheConcursoBacktest(
    int NumeroConcurso,
    DateTime DataSorteio,
    int[] DezenasSorteadas,
    int MelhorAcerto,
    int QuadrasNoConcurso,
    int QuinasNoConcurso,
    int SenasNoConcurso);

public sealed class BacktestLoteUseCase
{
    //private const decimal PrecoJogoSimples = 6.00m;

    public ResultadoBacktest Executar(
        IReadOnlyList<Jogo> lote,
        IReadOnlyList<ConcursoDto> concursos,
        int janela = 500,
        CancellationToken ct = default)
    {
        if (lote is null || lote.Count == 0)
            throw new ArgumentException("O lote não pode estar vazio.", nameof(lote));

        // Pega os últimos N concursos (mais recentes primeiro)
        var ultimos = concursos
            .OrderByDescending(c => c.NumeroConcurso)
            .Take(janela)
            .ToList();

        int totalQuadras = 0, totalQuinas = 0, totalSenas = 0;
        var detalhes = new List<DetalheConcursoBacktest>();

        foreach (var concurso in ultimos)
        {
            ct.ThrowIfCancellationRequested();

            var dezenasSorteadas = concurso.DezenasSorteadas
                .Split(',')
                .Select(d => int.Parse(d.Trim()))
                .ToHashSet();

            int melhorAcerto = 0;
            int quadras = 0, quinas = 0, senas = 0;

            foreach (var jogo in lote)
            {
                var acertos = jogo.Dezenas().Count(dezenasSorteadas.Contains);
                if (acertos > melhorAcerto) melhorAcerto = acertos;

                if (acertos == 4) quadras++;
                else if (acertos == 5) quinas++;
                else if (acertos == 6) senas++;
            }

            totalQuadras += quadras;
            totalQuinas += quinas;
            totalSenas += senas;

            detalhes.Add(new DetalheConcursoBacktest(
                concurso.NumeroConcurso,
                concurso.DataHoraSorteio,
                dezenasSorteadas.OrderBy(x => x).ToArray(),
                melhorAcerto,
                quadras,
                quinas,
                senas));
        }

        return new ResultadoBacktest(
            Janela: ultimos.Count,
            TotalJogosLote: lote.Count,
            //CustoTotal: lote.Count * PrecoJogoSimples * ultimos.Count,
            CustoTotal: PrecoAposta.Calcular(6) * lote.Count * ultimos.Count, // <-- Alterado
            Quadras: totalQuadras,
            Quinas: totalQuinas,
            Senas: totalSenas,
            Detalhes: detalhes);
    }
}

