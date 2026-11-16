
using MegaSena.Domain;

namespace MegaSena.Api.Services;

/// <summary>
/// Estima o pico de memória de um fechamento ANTES de rodar o solver, para o SolverGate poder
/// reservar memória e para recusar combinações grandes demais para o servidor.
/// </summary>
public static class EstimadorDeMemoria
{
    private const double MargemDeSeguranca = 1.5; // GC, listas intermediárias e fragmentação

    public static long BytesDoFechamento(int dezenas, int garantia, int universoSorteado)
    {
        long candidatos = Combinatoria.C(dezenas, 6); // limite superior: o filtro de maxPares só reduz

        // Garantia 6/6: o roteador só lista os jogos válidos, sem matriz de cobertura
        if (garantia == 6 && universoSorteado == 6)
            return candidatos * 64;

        // Guloso: uma linha de bits (ulong[]) por candidato, uma coluna por alvo
        long alvos = Combinatoria.C(dezenas, universoSorteado);
        long palavras = (alvos + 63) / 64;
        double bruto = candidatos * (palavras * 8.0 + 24) + (candidatos + alvos) * 8.0;

        return (long)(bruto * MargemDeSeguranca);
    }
}
