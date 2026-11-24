namespace SporAnaliz.Core.Model;

/// <summary>İki takımın golleri bağımsız Poisson kabul edilerek sonuç ve skor olasılıkları (formülle, simülasyonsuz).</summary>
public static class PoissonMath
{
    private const int MaxGoals = 12;

    public static (double Home, double Draw, double Away) Outcome(double lambdaHome, double lambdaAway)
    {
        var ph = Pmf(lambdaHome); var pa = Pmf(lambdaAway);
        double h = 0, d = 0, a = 0;
        for (int i = 0; i <= MaxGoals; i++)
            for (int j = 0; j <= MaxGoals; j++)
            {
                double p = ph[i] * pa[j];
                if (i > j) h += p; else if (i == j) d += p; else a += p;
            }
        double s = h + d + a;
        return (h / s, d / s, a / s);
    }

    /// <summary>Belirli bir skorun olasılığı.</summary>
    public static double Score(double lambdaHome, double lambdaAway, int home, int away) =>
        Pmf(lambdaHome)[Math.Min(home, MaxGoals)] * Pmf(lambdaAway)[Math.Min(away, MaxGoals)];

    private static double[] Pmf(double lambda)
    {
        var p = new double[MaxGoals + 1];
        p[0] = Math.Exp(-lambda);
        for (int k = 1; k <= MaxGoals; k++) p[k] = p[k - 1] * lambda / k;
        return p;
    }
}
