using SporAnaliz.Core.Model;
using MySqlConnector;

namespace SporAnaliz.Web.Setup;

/// <summary>Açılışta veri kaynağına ulaşılamadıysa hatayı tutar; durum ucu bunu sayfaya bildirir.</summary>
public sealed class StartupState
{
    public string? Error { get; set; }
}

public static class DataSetup
{
    /// <summary>Ayarlara göre veri kaynağını kaydeder: yerel maç veritabanı (varsayılan, salt okunur) ya da üretilmiş test verisi.</summary>
    public static IServiceCollection AddMatchData(this IServiceCollection services, IConfiguration config)
    {
        var options = DataOptions.Create(config.GetConnectionString("MySql"), config["Data:Source"], config["Data:Database"]);
        services.AddSingleton(options);
        // Model ayarları: appsettings.json içinde "Model" bölümü varsa oradan, yoksa koddaki (sınamayla seçilmiş) varsayılanlar
        services.AddSingleton(config.GetSection("Model").Get<ModelOptions>() ?? new ModelOptions());
        services.AddSingleton<StartupState>();
        if (options.UseFeed)
        {
            services.AddSingleton<IMatchData, FeedSource>();
        }
        else
        {
            services.AddSingleton<Db>();
            services.AddSingleton<TestDataSource>();
            services.AddSingleton<IMatchData>(sp => sp.GetRequiredService<TestDataSource>());
            services.AddSingleton<Seeder>();
        }
        return services;
    }

    /// <summary>Veri kaynağını açılışta yükler. Test kaynağında şemayı kurar ve veri yoksa üretir.</summary>
    public static void InitializeMatchData(this WebApplication app)
    {
        var state = app.Services.GetRequiredService<StartupState>();
        try
        {
            var options = app.Services.GetRequiredService<DataOptions>();
            var data = app.Services.GetRequiredService<IMatchData>();
            if (!options.UseFeed)
            {
                app.Services.GetRequiredService<Db>().EnsureSchema();
                if (data.CountTeams() == 0)
                    app.Logger.LogInformation("Varsayımsal veri üretildi: {Summary}",
                        app.Services.GetRequiredService<Seeder>().Persist(Seeder.Generate(DateTime.Today)));
            }
            app.Logger.LogInformation("Veri kaynağı: {Source} · {Teams} takım · son tamamlanan maç: {Until}",
                data.SourceName, data.CountTeams(), data.DataUntil);
        }
        catch (Exception ex)
        {
            state.Error = ex.Message;
            app.Logger.LogError("Veritabanına bağlanılamadı: {Message}. appsettings.json içindeki bağlantı bilgisini kontrol edin.", ex.Message);
        }
    }

    /// <summary>İstek sırasında veritabanı hatası olursa sayfaya anlaşılır bir JSON hatası döner.</summary>
    public static void UseDatabaseErrorHandling(this WebApplication app) => app.Use(async (ctx, next) =>
    {
        try { await next(); }
        catch (MySqlException ex)
        {
            ctx.Response.StatusCode = 503;
            await ctx.Response.WriteAsJsonAsync(new { error = "Veritabanı hatası: " + ex.Message });
        }
    });
}
