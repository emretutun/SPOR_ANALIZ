using SporAnaliz.Tools;

// Geliştirme araçları. Çözüm klasöründen:  dotnet run --project tools/SporAnaliz.Tools -- <komut>
//   backtest [half=.. prior=.. pull=.. euro=.. shot=..]   geçmiş sınaması (verilmeyen ayar varsayılan kalır)
//   tune                                                   ayarları veriyle seç, doğrulama döneminde sına
//   simcheck [macSayisi]                                   formül ile simülasyonu aynı maçlarda karşılaştır
//   explore [veritabani] [tablo] | explore sql "SELECT .." yerel veritabanını yalnızca okuyarak incele
//   selftest                                               veritabanısız motor denemesi
Console.OutputEncoding = System.Text.Encoding.UTF8;
string command = args.FirstOrDefault() ?? "";
var rest = args.Skip(1).ToArray();

switch (command)
{
    case "selftest":
        SelfTest.Run();
        break;
    case "backtest":
        Evaluation.Backtest(LoadOptions(), Evaluation.ParseModel(rest));
        break;
    case "tune":
        Evaluation.Tune(LoadOptions());
        break;
    case "simcheck":
        Evaluation.SimCheck(LoadOptions(), Evaluation.ParseModel(rest), rest.Select(a => int.TryParse(a, out var n) ? n : 0).FirstOrDefault(n => n > 0) is var k && k > 0 ? k : 300);
        break;
    case "explore":
        DbExplore.Run(LoadOptions(), rest);
        break;
    default:
        Console.WriteLine("Kullanım: backtest [ayar=değer ...] | tune | simcheck [maçSayısı] | explore [veritabani] [tablo] | explore sql \"SELECT ...\" | selftest");
        break;
}

// Bağlantı bilgisi web projesinin appsettings.json dosyasından okunur; bulunduğumuz klasörden yukarı doğru aranır.
static DataOptions LoadOptions()
{
    for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir != null; dir = dir.Parent)
    {
        string path = Path.Combine(dir.FullName, "src", "SporAnaliz.Web", "appsettings.json");
        if (File.Exists(path)) return DataOptions.FromJsonFile(path);
    }
    throw new FileNotFoundException("src/SporAnaliz.Web/appsettings.json bulunamadı; komutu çözüm klasörünün içinden çalıştırın.");
}
