namespace SporAnaliz.Core.Domain;

public sealed class TeamRow
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string ShortName { get; set; } = "";
    public string Country { get; set; } = "";
    public int? CompetitionId { get; set; }
    public string Color { get; set; } = "#888888";
    public double Elo { get; set; }
    public bool EloApprox { get; set; } = true;
}

public sealed class FixtureRow
{
    public int Id { get; set; }
    public int CompetitionId { get; set; }
    public string CompetitionName { get; set; } = "";
    public string Season { get; set; } = "";
    public int RoundNo { get; set; }
    public DateTime MatchDate { get; set; }
    public string Status { get; set; } = "";
    public int? HomeGoals { get; set; }
    public int? AwayGoals { get; set; }
    public int HomeTeamId { get; set; }
    public string HomeName { get; set; } = "";
    public string HomeShort { get; set; } = "";
    public string HomeColor { get; set; } = "";
    public int AwayTeamId { get; set; }
    public string AwayName { get; set; } = "";
    public string AwayShort { get; set; } = "";
    public string AwayColor { get; set; } = "";
    public long HasAnalysis { get; set; }
}

public sealed class TeamStatAvg
{
    public int TeamId { get; set; }
    public double ShotsFor { get; set; } = 12;
    public double ShotsAgainst { get; set; } = 12;
    public double Corners { get; set; } = 5;
    public double Yellows { get; set; } = 2;
    public double Possession { get; set; } = 50;
    public long N { get; set; }
}

public sealed class PlayerGoals
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Pos { get; set; } = "";
    public long Goals { get; set; }
}
