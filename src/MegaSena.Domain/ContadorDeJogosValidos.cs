namespace MegaSena.Domain;

/// <summary>Contagem exata (DP) de jogos válidos segundo uma regra de pares consecutivos.</summary>
public static class ContadorDeJogosValidos
{
    /// <summary>Jogos de 6 dezenas do universo com no máximo <paramref name="maxPares"/> pares consecutivos.</summary>
    public static long Contar(Universo universo, int maxPares)
    {
        if (maxPares < 0 || maxPares > 5)
            throw new ArgumentOutOfRangeException(nameof(maxPares), "maxPares deve estar entre 0 e 5.");

        var dz = universo.Dezenas;
        int n = dz.Count;
        // dp[i, k, p, usouAnterior] — compactamos i em duas camadas.
        var atual = new long[7, 6, 2];
        var proximo = new long[7, 6, 2];
        atual[0, 0, 0] = 1; // estado inicial: 0 escolhidas, 0 pares, anterior não usada

        for (int i = 0; i < n; i++)
        {
            Array.Clear(proximo);
            bool vizinha = i > 0 && dz[i] == dz[i - 1] + 1;

            for (int k = 0; k <= 6; k++)
                for (int p = 0; p <= 5; p++)
                    for (int ua = 0; ua <= 1; ua++)
                    {
                        long ways = atual[k, p, ua];
                        if (ways == 0) continue;

                        // 1) não escolhe a dezena i
                        proximo[k, p, 0] += ways;

                        // 2) escolhe a dezena i (se ainda cabe)
                        if (k < 6)
                        {
                            int np = p + ((ua == 1 && vizinha) ? 1 : 0);
                            if (np <= maxPares)
                                proximo[k + 1, np, 1] += ways;
                        }
                    }

            (atual, proximo) = (proximo, atual);
        }

        long total = 0;
        for (int p = 0; p <= maxPares; p++)
            total += atual[6, p, 0] + atual[6, p, 1];
        return total;
    }

    public static long ContarPorBlocos(Universo universo, int maxBlocos)
    {
        var dz = universo.Dezenas;
        int n = dz.Count;

        // estado: (k, blocos, emCorrida) onde emCorrida:
        //   0 = anterior não foi escolhida
        //   1 = anterior escolhida, formando corrida de tamanho 1 (ainda não contada)
        //   2 = anterior escolhida, dentro de corrida já contada
        var atual = new long[7, 7, 3];
        var proximo = new long[7, 7, 3];
        atual[0, 0, 0] = 1;

        for (int i = 0; i < n; i++)
        {
            Array.Clear(proximo);
            bool vizinha = i > 0 && dz[i] == dz[i - 1] + 1;

            for (int k = 0; k <= 6; k++)
                for (int b = 0; b <= 6; b++)
                    for (int ec = 0; ec <= 2; ec++)
                    {
                        long ways = atual[k, b, ec];
                        if (ways == 0) continue;

                        // 1) NÃO pega a dezena i → estado volta a 0 (anterior não usada)
                        proximo[k, b, 0] += ways;

                        // 2) Pega a dezena i
                        if (k < 6)
                        {
                            if (ec == 0 || !vizinha)
                            {
                                // abre corrida em potencial (tamanho 1, ainda não contada)
                                proximo[k + 1, b, 1] += ways;
                            }
                            else if (ec == 1)
                            {
                                // confirma corrida de tamanho ≥ 2 → conta 1 bloco
                                if (b + 1 <= maxBlocos)
                                    proximo[k + 1, b + 1, 2] += ways;
                            }
                            else // ec == 2
                            {
                                // continua corrida já contada → não conta de novo
                                proximo[k + 1, b, 2] += ways;
                            }
                        }
                    }
            (atual, proximo) = (proximo, atual);
        }

        long total = 0;
        for (int b = 0; b <= maxBlocos; b++)
            for (int ec = 0; ec <= 2; ec++)
                total += atual[6, b, ec];
        return total;
    }


    /// <summary>Distribuição exata por quantidade de pares consecutivos (0..5).</summary>
    public static long[] DistribuicaoPorPares(Universo universo, int maxPares = 5)
    {
        var res = new long[maxPares + 1];
        var dz = universo.Dezenas;
        int n = dz.Count;
        var atual = new long[7, 6, 2];
        var proximo = new long[7, 6, 2];
        atual[0, 0, 0] = 1;

        for (int i = 0; i < n; i++)
        {
            Array.Clear(proximo);
            bool vizinha = i > 0 && dz[i] == dz[i - 1] + 1;
            for (int k = 0; k <= 6; k++)
                for (int p = 0; p <= 5; p++)
                    for (int ua = 0; ua <= 1; ua++)
                    {
                        long ways = atual[k, p, ua];
                        if (ways == 0) continue;
                        proximo[k, p, 0] += ways;
                        if (k < 6)
                        {
                            int np = p + ((ua == 1 && vizinha) ? 1 : 0);
                            if (np <= 5)
                                proximo[k + 1, np, 1] += ways;
                        }
                    }
            (atual, proximo) = (proximo, atual);
        }

        for (int p = 0; p <= maxPares; p++)
            res[p] = atual[6, p, 0] + atual[6, p, 1];
        return res;
    }
}