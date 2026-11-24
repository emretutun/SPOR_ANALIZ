namespace SporAnaliz.Core.Analysis;

// Analiz çıktısı: sayfaya JSON olarak giden yapı.
public sealed record TeamInfo(int Id, string Name, string Short, string Color, double Elo, bool EloApprox);
public sealed record MatchInfo(int Id, string Competition, string Season, DateTime Date, TeamInfo Home, TeamInfo Away, int? ActualHome, int? ActualAway);
public sealed record ModelInfo(double ExpectedGoalsHome, double ExpectedGoalsAway, double HomeAdvantage,
    double HomeAttack, double HomeDefense, double AwayAttack, double AwayDefense);
public sealed record Outcome(double Home, double Draw, double Away);
public sealed record ScoreProb(int Home, int Away, double Pct);
public sealed record PairStat(string Label, double Home, double Away);
public sealed record FirstGoal(double Home, double Away, double None);
public sealed record ScorerProb(string Side, string Player, string Pos, long RecentGoals, double Pct);
public sealed record FormItem(DateTime Date, string Opponent, string Venue, int GoalsFor, int GoalsAgainst, string Result);
public sealed record H2HItem(DateTime Date, string Home, string Away, int HomeGoals, int AwayGoals);
/// <summary>Oyuncu değişikliğinde (Type = "sub") PlayerId/Player giren, OutPlayerId/OutPlayer çıkan oyuncudur.</summary>
public sealed record PlayEvent(int Slot, int Minute, int Added, string Side, string Type, int? PlayerId, string? Player, int HomeGoals, int AwayGoals,
    int? OutPlayerId = null, string? OutPlayer = null);
public sealed record Playback(int HomeGoals, int AwayGoals, List<PlayEvent> Events, double[] Momentum, List<PairStat> Stats);

public sealed class AnalysisResult
{
    public bool TestData { get; init; } = true;
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    public int Simulations { get; init; }
    public required MatchInfo Match { get; init; }
    public required ModelInfo Model { get; init; }
    public required Outcome Probabilities { get; init; }
    public required List<ScoreProb> TopScores { get; init; }
    /// <summary>Simülasyonlarda çıkan bütün skorlar (tahmin–gerçek karşılaştırması için).</summary>
    public required List<ScoreProb> AllScores { get; init; }
    public required List<PairStat> Expected { get; init; }
    public required List<PairStat> GoalTiming { get; init; }
    public required FirstGoal FirstGoal { get; init; }
    public required List<ScorerProb> Scorers { get; init; }
    public required List<PairStat> Comparison { get; init; }
    public required List<FormItem> HomeForm { get; init; }
    public required List<FormItem> AwayForm { get; init; }
    public required List<H2HItem> HeadToHead { get; init; }
    public required List<string> Comments { get; init; }
    public MatchLineups? Lineups { get; init; }
    public required Playback Simulation { get; init; }
}
