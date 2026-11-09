namespace MegaSena.Domain.Calculadora;

public static class DistribuicaoQuiQuadrado
{
    /// <summary>P(X ≥ x) para qui-quadrado com 'gl' graus de liberdade.</summary>
    public static double PValor(double x, int gl)
    {
        if (gl <= 0) throw new ArgumentOutOfRangeException(nameof(gl));
        return x <= 0 ? 1.0 : GamaSuperiorRegularizada(gl / 2.0, x / 2.0);
    }

    private static double GamaSuperiorRegularizada(double a, double x)
    {
        double lnGama = LogGama(a);
        if (x < a + 1) // série
        {
            double ap = a, soma = 1 / a, termo = soma;
            for (int i = 0; i < 500; i++)
            {
                ap++; termo *= x / ap; soma += termo;
                if (Math.Abs(termo) < Math.Abs(soma) * 1e-14) break;
            }
            return 1 - soma * Math.Exp(-x + a * Math.Log(x) - lnGama);
        }

        const double tiny = 1e-300; // fração contínua
        double b = x + 1 - a, c = 1 / tiny, d = 1 / b, h = d;
        for (int i = 1; i < 500; i++)
        {
            double an = -i * (i - a);
            b += 2;
            d = an * d + b; if (Math.Abs(d) < tiny) d = tiny;
            c = b + an / c; if (Math.Abs(c) < tiny) c = tiny;
            d = 1 / d;
            double delta = d * c; h *= delta;
            if (Math.Abs(delta - 1) < 1e-14) break;
        }
        return Math.Exp(-x + a * Math.Log(x) - lnGama) * h;
    }

    private static readonly double[] G =
    {
        0.99999999999980993, 676.5203681218851, -1259.1392167224028,
        771.32342877765313, -176.61502916214059, 12.507343278686905,
        -0.13857109526572012, 9.9843695780195716e-6, 1.5056327351493116e-7
    };

    private static double LogGama(double z) // Lanczos (g=7)
    {
        if (z < 0.5) return Math.Log(Math.PI / Math.Abs(Math.Sin(Math.PI * z))) - LogGama(1 - z);
        z -= 1;
        double x = G[0];
        for (int i = 1; i < 9; i++) x += G[i] / (z + i);
        double t = z + 7.5;
        return 0.5 * Math.Log(2 * Math.PI) + (z + 0.5) * Math.Log(t) - t + Math.Log(x);
    }
}