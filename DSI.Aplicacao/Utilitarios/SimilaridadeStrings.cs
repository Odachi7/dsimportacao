namespace DSI.Aplicacao.Utilitarios;

/// <summary>
/// Utilitário para cálculo de similaridade entre strings
/// </summary>
public static class SimilaridadeStrings
{
    /// <summary>
    /// Calcula a distância de Levenshtein entre duas strings
    /// </summary>
    public static int DistanciaLevenshtein(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1)) return s2?.Length ?? 0;
        if (string.IsNullOrEmpty(s2)) return s1.Length;

        var d = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
            d[i, 0] = i;

        for (int j = 0; j <= s2.Length; j++)
            d[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;

                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost
                );
            }
        }

        return d[s1.Length, s2.Length];
    }

    /// <summary>
    /// Calcula percentual de similaridade (0-100)
    /// </summary>
    public static int CalcularSimilaridade(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0;

        // Normaliza para comparação case-insensitive
        s1 = s1.ToLowerInvariant();
        s2 = s2.ToLowerInvariant();

        // Se são iguais, 100%
        if (s1 == s2)
            return 100;

        int distancia = DistanciaLevenshtein(s1, s2);
        int maxLength = Math.Max(s1.Length, s2.Length);
        
        if (maxLength == 0)
            return 100;

        double similaridade = 1.0 - ((double)distancia / maxLength);
        return (int)(similaridade * 100);
    }
}
