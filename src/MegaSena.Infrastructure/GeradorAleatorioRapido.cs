using System.Security.Cryptography;
using MegaSena.Domain;
using MegaSena.Application.Portas;

namespace MegaSena.Infrastructure;

public sealed class GeradorAleatorioRapido : IGeradorDeSorteio
{
    public string Nome => "Rápido";

    public Jogo Sortear(Universo universo, IRegraDeJogo regra)
    {
        var dezenas = universo.Dezenas.ToArray();
        var bolas = new int[dezenas.Length];

        // TRAVA DE SEGURANÇA (Item 4.3 do PDF)
        for (int tentativa = 0; tentativa < 10_000; tentativa++)
        {
            Array.Copy(dezenas, bolas, dezenas.Length);

            // Fisher-Yates parcial (Sorteia as 6 dezenas)
            for (int i = 0; i < 6; i++)
            {
                int j = RandomNumberGenerator.GetInt32(i, bolas.Length);
                (bolas[i], bolas[j]) = (bolas[j], bolas[i]);
            }

            var jogo = Jogo.DasDezenas(bolas.Take(6));
            if (regra.Aceita(jogo))
                return jogo;
        }

        // Se falhar 10.000 vezes, a regra é impossível para este universo
        throw new InvalidOperationException("Nenhum jogo válido possível neste universo/regra.");
    }
}