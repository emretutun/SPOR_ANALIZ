
namespace SporAnaliz.Core.Abstractions;

/// <summary>Analizin ihtiyaç duyduğu veriyi sağlayan kaynak (yerel maç veritabanı ya da üretilmiş test verisi).</summary>
public interface IMatchData
{
    string SourceName { get; }
    bool TestData { get; }
    bool EloEditable { get; }
    /// <summary>Kaynaktaki son tamamlanmış maçın tarihi (verinin güncelliğini göstermek için).</summary>
    DateTime? DataUntil { get; }

    int CountTeams();
    FixtureRow? GetFixture(int id);
    /// <summary>Tarih aralığındaki maçlar: hem oynanmamış hem sonuçlanmış olanlar.</summary>
    List<FixtureRow> GetMatches(DateTime from, DateTime to);
    List<MatchRow> GetHistory(DateTime before);
    Dictionary<int, TeamRow> GetTeams();
    Dictionary<int, TeamStatAvg> GetStats(DateTime before);
    List<PlayerGoals> GetPlayerGoals(int teamId, DateTime before);
    /// <summary>Turnuvalar (hafta seçimi için).</summary>
    List<CompetitionInfo> GetCompetitions() => new();
    /// <summary>Bir turnuvanın sezonları/aşamaları ve her birinin haftaları.</summary>
    List<StageInfo> GetStages(int competitionId) => new();
    List<FixtureRow> GetRoundMatches(int competitionId, int stageId, string round) => new();
    /// <summary>Canlı ekran için tarih aralığındaki BÜTÜN maçlar (oynanmış, oynanıyor, oynanacak), anlık skorla; önbelleğe bakmadan okunur.</summary>
    List<FixtureRow> GetDayMatches(DateTime from, DateTime to) => GetMatches(from, to);
    /// <summary>Maç kadrosu: açıklandıysa gerçek ilk 11, açıklanmadıysa son maçlardan muhtemel 11. Veri yoksa null.</summary>
    MatchLineups? GetLineups(FixtureRow match) => null;
    /// <summary>Takım → lig/ülke grubu (güç hesabında seviye tespiti için). Yoksa null.</summary>
    IReadOnlyDictionary<int, int>? GetTeamGroups() => null;
    string? GetAnalysisJson(int matchId);
    void SaveAnalysis(int matchId, int sims, string json);
}
