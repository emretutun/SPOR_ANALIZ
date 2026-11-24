using System.Text.Json;

namespace SporAnaliz.Core.Analysis;

public sealed record TeamRating(int Id, string Name, string Country, string Color, double Elo, bool EloApprox, int Matches);

/// <summary>
/// Veri kaynağı ile analiz motorunu birleştirir: bir maçın girdilerini toplar, analizi üretir ve saklar.
/// Web uçları ve araçlar motoru doğrudan değil bu sınıf üzerinden kullanır.
/// </summary>
public sealed class AnalysisService(IMatchData data, ModelOptions model)
{
    public const int DefaultSimulations = 10000;
    private static readonly JsonSerializerOptions Json = JsonSerializerOptions.Web;

    /// <summary>Maçın analizini JSON olarak döndürür; saklanmış analiz varsa onu kullanır. Maç yoksa null.</summary>
    public string? GetOrCompute(int matchId, bool refresh = false, int? simulations = null)
    {
        if (!refresh)
        {
            var cached = data.GetAnalysisJson(matchId);
            if (cached != null) return cached;
        }
        int n = Math.Clamp(simulations ?? DefaultSimulations, 100, 100000);
        var result = Compute(matchId, n);
        if (result == null) return null;
        var text = JsonSerializer.Serialize(result, Json);
        data.SaveAnalysis(matchId, n, text);
        return text;
    }

    public AnalysisResult? Compute(int matchId, int simulations = DefaultSimulations)
    {
        var m = data.GetFixture(matchId);
        if (m == null) return null;
        // Bütün girdiler maç tarihinden ÖNCEKİ veriye göre toplanır; geçmiş bir maç o günün bilgisiyle tahmin edilir.
        return Analyzer.Compute(new AnalysisInput
        {
            Match = m,
            History = data.GetHistory(m.MatchDate),
            Teams = data.GetTeams(),
            Stats = data.GetStats(m.MatchDate),
            HomePlayers = data.GetPlayerGoals(m.HomeTeamId, m.MatchDate),
            AwayPlayers = data.GetPlayerGoals(m.AwayTeamId, m.MatchDate),
            Simulations = simulations,
            TestData = data.TestData,
            Groups = data.GetTeamGroups(),
            Lineups = data.GetLineups(m),
            Model = model,
        });
    }

    /// <summary>
    /// Güç sıralaması. Elo tablosu olan kaynakta tablo; diğerlerinde bugüne kadarki maç sonuçlarından hesaplanan puan
    /// (son iki yılda en az <paramref name="minMatches"/> maçı olan takımlar).
    /// </summary>
    public List<TeamRating> GetRatings(DateTime asOf, int minMatches = 12)
    {
        var teams = data.GetTeams();
        if (data.EloEditable)
            return teams.Values.OrderByDescending(t => t.Elo)
                .Select(t => new TeamRating(t.Id, t.Name, t.Country, t.Color, t.Elo, t.EloApprox, 0)).ToList();

        var history = data.GetHistory(asOf);
        var fit = StrengthModel.Fit(history, asOf, data.GetTeamGroups(), model);
        var recent = asOf.AddYears(-2);
        var counts = new Dictionary<int, int>();
        foreach (var h in history.Where(h => h.MatchDate >= recent))
        {
            counts[h.HomeTeamId] = counts.GetValueOrDefault(h.HomeTeamId) + 1;
            counts[h.AwayTeamId] = counts.GetValueOrDefault(h.AwayTeamId) + 1;
        }
        return counts.Where(c => c.Value >= minMatches && teams.ContainsKey(c.Key)).Select(c =>
        {
            var t = teams[c.Key];
            double elo = Math.Round(StrengthModel.StrengthToElo(Math.Log(fit.AttOf(t.Id) / fit.DefOf(t.Id)) / 2));
            return new TeamRating(t.Id, t.Name, t.Country, t.Color, elo, false, c.Value);
        }).OrderByDescending(x => x.Elo).ToList();
    }
}
