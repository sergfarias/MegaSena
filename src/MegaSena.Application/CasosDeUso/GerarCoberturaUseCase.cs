using MegaSena.Domain;
using MegaSena.Domain.Cobertura;
using MegaSena.Domain.Calculadora;
using System.Numerics; 

namespace MegaSena.Application.CasosDeUso;

public sealed class GerarCoberturaUseCase
{
    private readonly ICoberturaSolver _solver;

    public GerarCoberturaUseCase(ICoberturaSolver solver)
    {
        _solver = solver ?? throw new ArgumentNullException(nameof(solver));
    }

    public ResultadoCobertura Executar(
        IReadOnlySet<int> dezenas,
        int garantia,
        int universoSorteado,
        int maxPares = 1,
        CancellationToken ct = default)
    {
        if (dezenas.Count < 6)
            throw new ArgumentException("Mínimo de 6 dezenas.", nameof(dezenas));
        if (garantia < 1 || garantia > 6)
            throw new ArgumentOutOfRangeException(nameof(garantia), "Garantia deve ser entre 1 e 6.");
        if (universoSorteado < garantia || universoSorteado > 6)
            throw new ArgumentOutOfRangeException(nameof(universoSorteado), "Universo sorteado inválido.");

        var cobertura = _solver.Resolver(dezenas, garantia, universoSorteado, maxPares, ct);

        // Otimizado com Bitmasks e Respeitando o CancellationToken
        var totalAlvos = Combinatoria.C(dezenas.Count, universoSorteado);
        var cobertos = ContarAlvosCobertos(cobertura, universoSorteado, ct);

        // Lógica para saber se o motor exato provou o mínimo
        bool minimoProvado = (garantia == 6 && universoSorteado == 6) || (dezenas.Count <= 9);

        return new ResultadoCobertura(
            cobertura,
            cobertura.Jogos.Count,
            totalAlvos,
            cobertos,
            GarantiaTotal: cobertos == totalAlvos,
            CustoTotal: PrecoAposta.Calcular(6) * cobertura.Jogos.Count,
            MinimoProvado: minimoProvado 
        );
    }

    private static long ContarAlvosCobertos(Cobertura c, int k, CancellationToken ct)
    {
        // 1. Converte as dezenas do lote em máscaras de bits (bitmask)
        var dezenasArr = c.Dezenas.OrderBy(x => x).ToArray();
        var jogosMascaras = c.Jogos.Select(j => GerarMascara(j.Dezenas())).ToArray();

        long cobertos = 0;
        long iter = 0;

        // 2. Gera todas as combinações possíveis (alvos) e conta
        foreach (var alvo in Combinatoria.Combinacoes(dezenasArr, k))
        {
            // A cada 1024 alvos verificados (cobertos ou não), checa o cancelamento
            if ((++iter & 1023) == 0)
                ct.ThrowIfCancellationRequested();

            var alvoMascara = GerarMascara(alvo);

            foreach (var jogoMascara in jogosMascaras)
            {
                // PopCount conta quantos bits '1' existem na interseção entre o alvo e o jogo
                if (BitOperations.PopCount(jogoMascara & alvoMascara) >= c.Garantia)
                {
                    cobertos++;
                    break; // Se já achou um jogo que cobre, não precisa testar os outros
                }
            }
        }

        return cobertos;
    }

    private static ulong GerarMascara(IEnumerable<int> dezenas)
    {
        ulong mascara = 0;
        foreach (var d in dezenas)
            mascara |= (1UL << (d - 1));
        return mascara;
    }
}