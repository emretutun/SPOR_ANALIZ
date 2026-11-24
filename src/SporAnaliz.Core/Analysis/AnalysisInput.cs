namespace SporAnaliz.Core.Analysis;

/// <summary>Bir maçın analizi için gereken bütün girdiler.</summary>
public sealed class AnalysisInput
{
    public required FixtureRow Match { get; init; }
    public required IReadOnlyList<MatchRow> History { get; init; }
    public required IReadOnlyDictionary<int, TeamRow> Teams { get; init; }
    public required IReadOnlyDictionary<int, TeamStatAvg> Stats { get; init; }
    public required IReadOnlyList<PlayerGoals> HomePlayers { get; init; }
    public required IReadOnlyList<PlayerGoals> AwayPlayers { get; init; }
    public int Simulations { get; init; } = 10000;
    public bool TestData { get; init; } = true;
    public IReadOnlyDictionary<int, int>? Groups { get; init; }
    public MatchLineups? Lineups { get; init; }
    public ModelOptions Model { get; init; } = new();
}
