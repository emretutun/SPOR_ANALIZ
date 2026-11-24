namespace SporAnaliz.Core.Domain;

public sealed record CompetitionInfo(int Id, string Name);
public sealed record RoundInfo(string Name, DateTime First, DateTime Last, int Matches, int Played, bool Current);
public sealed record StageInfo(int Id, string Name, bool Current, List<RoundInfo> Rounds);

public sealed record LineupPlayer(int Id, string Name, string ShortName, int Number, string Pos, string Tactical);

/// <summary>Bir takımın maç kadrosu. Confirmed=false ise kadro açıklanmamıştır; son maçlardan çıkarılmış muhtemel 11'dir.</summary>
public sealed record TeamLineup(string Formation, string? Manager, List<LineupPlayer> Starting, List<LineupPlayer> Bench, bool Confirmed);

public sealed record MatchLineups(TeamLineup Home, TeamLineup Away);
