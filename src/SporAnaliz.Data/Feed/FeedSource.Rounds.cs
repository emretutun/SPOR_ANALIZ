using System.Text.Json;
using Dapper;
using MySqlConnector;

namespace SporAnaliz.Data.Feed;

// Turnuva, sezon/aşama ve hafta listeleri.
public sealed partial class FeedSource
{
    private static readonly int[] CompetitionOrder = { 1, 3, 351, 698, 4, 71, 81, 2, 7, 6, 5, 8, 91 };

    public List<CompetitionInfo> GetCompetitions()
    {
        Load();
        return CompetitionOrder.Where(_tournaments.ContainsKey).Select(id => new CompetitionInfo(id, _tournaments[id])).ToList();
    }

    public List<StageInfo> GetStages(int competitionId)
    {
        Load();
        var today = DateTime.Today;
        var stages = _matches.Values.Where(m => m.TournamentId == competitionId && (m.StatusId == ScheduledStatus || m.Finished))
            .GroupBy(m => m.StageId).Select(g =>
            {
                var first = g.Min(m => m.Date);
                int year = first.Month >= 7 ? first.Year : first.Year - 1;
                string name = g.First().StageName ?? "Aşama";
                if (!name.Contains("Sezon", StringComparison.OrdinalIgnoreCase)) name += $" · {year}/{(year + 1) % 100:00}";
                var rounds = g.GroupBy(m => m.RoundName ?? "-").Select(r => (Name: r.Key, First: r.Min(m => m.Date), Last: r.Max(m => m.Date), N: r.Count(), P: r.Count(m => m.Finished)))
                    .OrderBy(r => r.First).ToList();
                // Güncel hafta: bugünü içeren ya da bugünden sonraki ilk hafta; hepsi geçmişse sonuncusu
                int cur = rounds.FindIndex(r => r.Last.Date >= today);
                if (cur < 0) cur = rounds.Count - 1;
                return (Id: g.Key, Name: name, First: first, Last: g.Max(m => m.Date),
                    Rounds: rounds.Select((r, i) => new RoundInfo(r.Name, r.First, r.Last, r.N, r.P, i == cur)).ToList());
            }).OrderByDescending(s => s.First).ToList();
        // Güncel aşama: bugünden sonra maçı olan en erken başlayan; yoksa en son biten
        var upcoming = stages.Where(s => s.Last.Date >= today).OrderBy(s => s.First).FirstOrDefault();
        int currentId = upcoming.Rounds != null ? upcoming.Id : stages.OrderByDescending(s => s.Last).Select(s => s.Id).FirstOrDefault();
        return stages.Select(s => new StageInfo(s.Id, s.Name, s.Id == currentId, s.Rounds)).ToList();
    }

    public List<FixtureRow> GetRoundMatches(int competitionId, int stageId, string round)
    {
        Load();
        return _matches.Values.Where(m => m.TournamentId == competitionId && m.StageId == stageId && (m.RoundName ?? "-") == round && (m.StatusId == ScheduledStatus || m.Finished))
            .OrderBy(m => m.Date).ThenBy(m => m.MatchId).Select(ToFixture).ToList();
    }
}
