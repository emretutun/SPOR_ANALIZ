namespace SporAnaliz.Core.Model;

public sealed class Strengths
{
    public double Mu;        // deplasman takımının ortalama beklenen golü
    public double HomeAdv;   // iç saha çarpanı
    public Dictionary<int, double> Att = new();  // hücum gücü (1 = ortalama, yüksek = iyi)
    public Dictionary<int, double> Def = new();  // yenen gol çarpanı (1 = ortalama, düşük = iyi)

    public double AttOf(int t) => Att.GetValueOrDefault(t, 1.0);
    public double DefOf(int t) => Def.GetValueOrDefault(t, 1.0);

    public (double Home, double Away) Lambdas(int home, int away) =>
        (Mu * HomeAdv * AttOf(home) * DefOf(away), Mu * AttOf(away) * DefOf(home));
}

/// <summary>
/// Poisson tabanlı hücum/savunma gücü tahmini. Tüm maçlar (lig + Avrupa) birlikte kullanılır;
/// farklı ligler Avrupa maçları üzerinden aynı ölçeğe bağlanır. Eski maçların ağırlığı zamanla azalır,
/// az maçı olan takımlar ortalamaya doğru çekilir.
/// </summary>
public static class StrengthModel
{
    /// <summary>Elo puanını log-ölçekli genel güce çevirir (100 Elo farkı ≈ gol beklentisinde 1,5 kat oran).</summary>
    public static double EloToStrength(double elo) => (elo - 1700.0) / 560.0;
    public static double StrengthToElo(double s) => 1700.0 + 560.0 * s;

    /// <summary>Bir birim güç farkının top hakimiyetine etkisi (yüzde puan).</summary>
    public const double PossessionPerStrength = 14.0;

    /// <param name="groups">Takım → lig/ülke grubu. Verilirse az maçlı takım genel ortalamaya değil kendi grubunun seviyesine çekilir.</param>
    public static Strengths Fit(IReadOnlyList<MatchRow> matches, DateTime asOf, IReadOnlyDictionary<int, int>? groups = null, ModelOptions? options = null)
    {
        options ??= new ModelOptions();
        double halfLifeDays = options.HalfLifeDays, priorMatches = options.PriorMatches, groupPull = options.GroupPull;
        int iterations = options.Iterations;
        var s = new Strengths { Mu = 1.25, HomeAdv = 1.2 };
        if (matches.Count == 0) return s;

        var w = new double[matches.Count];
        var ids = new HashSet<int>();
        for (int i = 0; i < matches.Count; i++)
        {
            double days = Math.Max(0, (asOf - matches[i].MatchDate).TotalDays);
            w[i] = Math.Pow(0.5, days / halfLifeDays) * matches[i].Weight * (matches[i].European ? options.EuropeanWeight : 1);
            ids.Add(matches[i].HomeTeamId);
            ids.Add(matches[i].AwayTeamId);
        }
        foreach (var id in ids) { s.Att[id] = 1; s.Def[id] = 1; }

        // Maç başına "etkili gol": gerçek gol ile şutlardan beklenen golün karışımı (ShotWeight = 0 ise yalnızca gerçek gol)
        var (gh, gaw) = EffectiveGoals(matches, w, options.ShotWeight);

        var gf = ids.ToDictionary(i => i, _ => 0.0);
        var ga = ids.ToDictionary(i => i, _ => 0.0);
        double totW = 0, totGoals = 0;
        for (int i = 0; i < matches.Count; i++)
        {
            var m = matches[i];
            gf[m.HomeTeamId] += w[i] * gh[i]; ga[m.HomeTeamId] += w[i] * gaw[i];
            gf[m.AwayTeamId] += w[i] * gaw[i]; ga[m.AwayTeamId] += w[i] * gh[i];
            totW += w[i]; totGoals += w[i] * (gh[i] + gaw[i]);
        }
        double prior = priorMatches * totGoals / (2 * totW); // sözde maçların gol karşılığı

        // Takım başına ağırlıklı maç sayısı: ölçek sabitlenirken az maçlı takımlar ortalamayı bozmasın
        var teamW = ids.ToDictionary(i => i, _ => 0.0);
        for (int i = 0; i < matches.Count; i++) { teamW[matches[i].HomeTeamId] += w[i]; teamW[matches[i].AwayTeamId] += w[i]; }
        double sumTeamW = teamW.Values.Sum();

        var exp = new Dictionary<int, double>();
        var centerAtt = ids.ToDictionary(i => i, _ => 1.0);
        var centerDef = ids.ToDictionary(i => i, _ => 1.0);
        var gSum = new Dictionary<int, (double W, double A, double D)>();
        for (int it = 0; it < iterations; it++)
        {
            if (groups != null)
            {
                // Her grubun (lig/ülke) o anki ortalama seviyesi: zayıf ligin az maçlı takımı "ortalama" sayılmaz
                gSum.Clear();
                foreach (var id in ids)
                {
                    if (!groups.TryGetValue(id, out int g)) continue;
                    var x = gSum.GetValueOrDefault(g);
                    gSum[g] = (x.W + teamW[id], x.A + teamW[id] * s.Att[id], x.D + teamW[id] * s.Def[id]);
                }
                foreach (var id in ids)
                {
                    if (!groups.TryGetValue(id, out int g) || gSum[g].W <= 0) { centerAtt[id] = 1; centerDef[id] = 1; continue; }
                    centerAtt[id] = groupPull * gSum[g].A / gSum[g].W + (1 - groupPull);
                    centerDef[id] = groupPull * gSum[g].D / gSum[g].W + (1 - groupPull);
                }
            }
            // Hücum ve savunma SIRAYLA güncellenir (önce hücum, sonra yeni hücumla savunma).
            // İkisi aynı anda güncellenirse az maçlı çok takım varken hesap salınıma girip dağılıyor.
            foreach (var id in ids) exp[id] = 0;
            for (int i = 0; i < matches.Count; i++)
            {
                var m = matches[i];
                exp[m.HomeTeamId] += w[i] * s.Mu * s.HomeAdv * s.Def[m.AwayTeamId];
                exp[m.AwayTeamId] += w[i] * s.Mu * s.Def[m.HomeTeamId];
            }
            foreach (var id in ids) s.Att[id] = (gf[id] + prior * centerAtt[id]) / (exp[id] + prior);

            foreach (var id in ids) exp[id] = 0;
            for (int i = 0; i < matches.Count; i++)
            {
                var m = matches[i];
                exp[m.AwayTeamId] += w[i] * s.Mu * s.HomeAdv * s.Att[m.HomeTeamId];
                exp[m.HomeTeamId] += w[i] * s.Mu * s.Att[m.AwayTeamId];
            }
            foreach (var id in ids) s.Def[id] = (ga[id] + prior * centerDef[id]) / (exp[id] + prior);

            // Ölçek: maç ağırlıklı ortalama 1 olacak şekilde; fark Mu'ya aktarılır, beklenen goller değişmez
            double ma = 0, md = 0;
            foreach (var id in ids) { ma += teamW[id] * s.Att[id]; md += teamW[id] * s.Def[id]; }
            ma /= sumTeamW; md /= sumTeamW;
            foreach (var id in ids) { s.Att[id] /= ma; s.Def[id] /= md; }
            s.Mu *= ma * md;

            double homeG = 0, homeExp = 0, allG = 0, allExp = 0;
            for (int i = 0; i < matches.Count; i++)
            {
                var m = matches[i];
                double eh = s.Att[m.HomeTeamId] * s.Def[m.AwayTeamId], ea = s.Att[m.AwayTeamId] * s.Def[m.HomeTeamId];
                homeG += w[i] * gh[i]; homeExp += w[i] * s.Mu * eh;
                allG += w[i] * (gh[i] + gaw[i]); allExp += w[i] * (s.HomeAdv * eh + ea);
            }
            if (homeExp > 0) s.HomeAdv = Math.Clamp(homeG / homeExp, 1.0, 1.6);
            if (allExp > 0) s.Mu = allG / allExp;
        }
        return s;
    }

    /// <summary>
    /// Her maç için güç hesabına girecek gol değerleri. Şut verisi olan maçlarda gerçek gol, şutlardan beklenen golle
    /// <paramref name="shotWeight"/> oranında harmanlanır. "Şutlardan beklenen gol" = a × isabetli şut + b × isabetsiz şut;
    /// a ve b, eldeki maçlarda gollere en iyi uyan değerler olarak veriden bulunur (toplam gol korunur).
    /// </summary>
    private static (double[] Home, double[] Away) EffectiveGoals(IReadOnlyList<MatchRow> matches, double[] w, double shotWeight)
    {
        int n = matches.Count;
        var home = new double[n]; var away = new double[n];
        for (int i = 0; i < n; i++) { home[i] = matches[i].HomeGoals; away[i] = matches[i].AwayGoals; }
        if (shotWeight <= 0) return (home, away);

        // En küçük kareler: gol ≈ a·isabetli + b·isabetsiz (sabit terimsiz, maç ağırlıklı)
        double sxx = 0, sxy = 0, syy = 0, sxg = 0, syg = 0;
        void Acc(double wt, int? on, int? off, int goals)
        {
            if (on == null || off == null) return;
            double x = on.Value, y = off.Value;
            sxx += wt * x * x; sxy += wt * x * y; syy += wt * y * y; sxg += wt * x * goals; syg += wt * y * goals;
        }
        for (int i = 0; i < n; i++)
        {
            Acc(w[i], matches[i].HomeShotsOn, matches[i].HomeShotsOff, matches[i].HomeGoals);
            Acc(w[i], matches[i].AwayShotsOn, matches[i].AwayShotsOff, matches[i].AwayGoals);
        }
        double det = sxx * syy - sxy * sxy;
        if (sxx <= 0 || Math.Abs(det) < 1e-9) return (home, away);
        double a = (sxg * syy - syg * sxy) / det, b = (syg * sxx - sxg * sxy) / det;
        if (b < 0) { b = 0; a = sxg / sxx; }   // isabetsiz şut golü azaltamaz
        if (a <= 0) return (home, away);

        double k = Math.Min(1, shotWeight);
        for (int i = 0; i < n; i++)
        {
            var m = matches[i];
            if (m.HomeShotsOn != null && m.HomeShotsOff != null) home[i] = (1 - k) * m.HomeGoals + k * (a * m.HomeShotsOn.Value + b * m.HomeShotsOff.Value);
            if (m.AwayShotsOn != null && m.AwayShotsOff != null) away[i] = (1 - k) * m.AwayGoals + k * (a * m.AwayShotsOn.Value + b * m.AwayShotsOff.Value);
        }
        return (home, away);
    }
}
