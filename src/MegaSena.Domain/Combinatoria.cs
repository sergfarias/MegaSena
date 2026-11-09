namespace MegaSena.Domain;

public static class Combinatoria
{
    /// <summary>C(n, k) exato em long — seguro para n ≤ 60, k ≤ 6.</summary>
    public static long C(int n, int k)
    {
        if (k < 0 || k > n) return 0;
        k = Math.Min(k, n - k);
        long r = 1;
        for (int i = 1; i <= k; i++)
            r = r * (n - k + i) / i;
        return r;
    }

    /// <summary>
    /// Gera todas as combinações de tamanho k do array. Lazy (yield return).
    /// </summary>
    public static IEnumerable<int[]> Combinacoes(int[] arr, int k)
    {
        var indices = Enumerable.Range(0, k).ToArray();
        while (true)
        {
            yield return indices.Select(i => arr[i]).ToArray();
            int pos = k - 1;
            while (pos >= 0 && indices[pos] == arr.Length - k + pos) pos--;
            if (pos < 0) yield break;
            indices[pos]++;
            for (int j = pos + 1; j < k; j++) indices[j] = indices[j - 1] + 1;
        }
    }
}