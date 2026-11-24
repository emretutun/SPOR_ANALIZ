using SporAnaliz.Web.Setup;

namespace SporAnaliz.Web.Endpoints;

public sealed record EloUpdate(double Elo);

public static class StatusEndpoints
{
    public static void MapStatusEndpoints(this WebApplication app) => app.MapGet("/api/status", (IMatchData data, StartupState state) =>
    {
        try
        {
            return Results.Json(new
            {
                ok = true, source = data.SourceName, teams = data.CountTeams(), testData = data.TestData,
                eloEditable = data.EloEditable, dataUntil = data.DataUntil
            });
        }
        catch (Exception ex)
        {
            return Results.Json(new { ok = false, source = data.SourceName, error = state.Error ?? ex.Message });
        }
    });
}

public static class FixtureEndpoints
{
    public static void MapFixtureEndpoints(this WebApplication app)
    {
        // Maç listesi. Turnuva + aşama + hafta verilirse o haftanın maçları; verilmezse tarih aralığı (varsayılan: bugünden 8 gün).
        app.MapGet("/api/fixtures", (IMatchData data, DateTime? from, DateTime? to, int? days, int? competition, int? stage, string? round) =>
        {
            List<FixtureRow> list;
            if (competition != null && stage != null && !string.IsNullOrEmpty(round))
                list = data.GetRoundMatches(competition.Value, stage.Value, round);
            else
            {
                var start = (from ?? DateTime.Today).Date;
                var end = to?.Date.AddDays(1) ?? start.AddDays(Math.Clamp(days ?? 8, 1, 60));
                if (end <= start) end = start.AddDays(1);
                if ((end - start).TotalDays > 400) end = start.AddDays(400);
                list = data.GetMatches(start, end);
            }
            var teams = data.GetTeams();
            bool Turkish(int id) => teams.TryGetValue(id, out var t) && t.Country == "Türkiye";
            return Results.Json(list.Select(f => new
            {
                f.Id, competition = f.CompetitionName, date = f.MatchDate, status = f.Status,
                home = f.HomeName, homeShort = f.HomeShort, homeColor = f.HomeColor,
                away = f.AwayName, awayShort = f.AwayShort, awayColor = f.AwayColor,
                homeGoals = f.Status == "played" ? f.HomeGoals : null, awayGoals = f.Status == "played" ? f.AwayGoals : null,
                turkish = Turkish(f.HomeTeamId) || Turkish(f.AwayTeamId),
                hasAnalysis = f.HasAnalysis != 0
            }));
        });

        app.MapGet("/api/competitions", (IMatchData data) => Results.Json(data.GetCompetitions()));
        app.MapGet("/api/rounds", (IMatchData data, int competition) => Results.Json(data.GetStages(competition)));
    }
}

public static class AnalysisEndpoints
{
    public static void MapAnalysisEndpoints(this WebApplication app) =>
        app.MapGet("/api/analysis/{matchId:int}", (AnalysisService analysis, int matchId, bool? refresh, int? sims) =>
        {
            var json = analysis.GetOrCompute(matchId, refresh == true, sims);
            return json == null ? Results.NotFound(new { error = "Maç bulunamadı." }) : Results.Content(json, "application/json");
        });
}

public static class TeamEndpoints
{
    public static void MapTeamEndpoints(this WebApplication app)
    {
        // Güç sıralaması. Test verisinde Elo tablosu (düzenlenebilir); maç veritabanında sonuçlardan hesaplanan puan.
        app.MapGet("/api/teams", (AnalysisService analysis) => Results.Json(analysis.GetRatings(DateTime.Today)));

        app.MapPut("/api/teams/{id:int}/elo", (IMatchData data, int id, EloUpdate body) =>
        {
            if (data is not TestDataSource test)
                return Results.BadRequest(new { error = "Bu veri kaynağında güç puanı maç sonuçlarından hesaplanır, elle değiştirilemez." });
            if (body.Elo is < 800 or > 2400) return Results.BadRequest(new { error = "Elo 800 ile 2400 arasında olmalı." });
            return test.UpdateElo(id, body.Elo, approx: false)
                ? Results.Json(new { ok = true })
                : Results.NotFound(new { error = "Takım bulunamadı." });
        });
    }
}

public static class AdminEndpoints
{
    // Yalnızca test kaynağında: tabloları silip varsayımsal veriyi yeniden üretir.
    public static void MapAdminEndpoints(this WebApplication app) => app.MapPost("/api/admin/seed", (IServiceProvider sp, DataOptions options, StartupState state) =>
    {
        if (options.UseFeed) return Results.BadRequest(new { error = "Maç veritabanı salt okunurdur; veri üretilmez." });
        var summary = sp.GetRequiredService<Seeder>().Persist(Seeder.Generate(DateTime.Today));
        state.Error = null;
        return Results.Json(summary);
    });
}
