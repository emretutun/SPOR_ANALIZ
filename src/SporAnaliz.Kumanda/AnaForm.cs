using System.Diagnostics;
using System.Globalization;
using SporAnaliz.Core.Abstractions;
using SporAnaliz.Core.Analysis;
using SporAnaliz.Core.Domain;
using SporAnaliz.Core.Model;
using SporAnaliz.Core.Playback;
using SporAnaliz.Data.Atak;
using SporAnaliz.Data.Feed;
using SporAnaliz.Kumanda.Viz;

namespace SporAnaliz.Kumanda;

/// <summary>
/// Viz kumandası. Akış: maçı seç → ANALİZ ET → VİZ'E YÜKLE → OYNAT. Her adım kendi düğmesiyle (ve kısayoluyla) yapılır.
/// Yerleşim AnaForm.Designer.cs'tedir (Visual Studio tasarım ekranında düzenlenebilir); burada yalnızca davranış var.
/// Sahneye yalnızca paylaşımlı bellek üzerinden veri gider (bkz. viz/MacSim.vbs).
/// </summary>
public sealed partial class AnaForm : Form
{
    // ---- renkler (göz yormaması için düşük karşıtlıklı koyu tema; tasarım dosyasındakilerle aynı)
    // ---- renkler: tasarım ekranındaki "gorunum" bileşeninden (Properties penceresinde değiştirilir)
    Color Zemin => gorunum.ZeminRengi;
    Color Yuzey => gorunum.YuzeyRengi;
    Color Satir => gorunum.SatirRengi;
    Color Satir2 => gorunum.Satir2Rengi;
    Color Dugme => gorunum.DugmeRengi;
    Color DugmeUstu => gorunum.DugmeUstuRengi;
    Color Yazi => gorunum.YaziRengi;
    Color Soluk => gorunum.SolukYaziRengi;
    Color Mavi => gorunum.VurguRengi;          // sıradaki adım, seçili satır, seçili hız, seçili sekme
    Color MaviUstu => gorunum.VurguUstuRengi;
    Color Yesil => gorunum.OynatRengi;         // oynat
    Color YesilUstu => gorunum.OynatUstuRengi;
    Color YaziYesil => gorunum.BasariRengi;
    Color YaziSari => gorunum.UyariRengi;
    Color YaziKirmizi => gorunum.HataRengi;
    static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    readonly KumandaAyarlari _ayar;
    readonly IMatchData _veri;
    readonly AnalysisService _analiz;
    readonly VizBaglanti _viz = new();
    readonly VizYayin _yayin;

    FixtureRow? _secili;
    MatchPlayer? _oynatici;
    bool _yuklendi, _oynuyor, _analizde;
    readonly Stopwatch _saat = new();
    double _sonGonderim, _sonAdim, _hiz = 1;

    List<FixtureRow> _maclar = new();
    List<StageInfo> _asamalar = new();

    public AnaForm(KumandaAyarlari ayar)
    {
        _ayar = ayar;
        _veri = new FeedSource(ayar.Veri);
        _analiz = new AnalysisService(_veri, new ModelOptions());
        _yayin = new VizYayin(_viz);

        InitializeComponent();

        // Sekmeler tasarım ekranında normal sekme olarak görünür (tıklayıp içine Toolbox'tan kontrol bırakılabilir).
        // Çalışırken başlıkları gizlenir; geçiş üstteki SİMÜLASYON / CANLI MAÇ düğmeleriyle yapılır.
        tabSekmeler.Appearance = TabAppearance.FlatButtons;
        tabSekmeler.ItemSize = new Size(0, 1);
        tabSekmeler.SizeMode = TabSizeMode.Fixed;
        tabSekmeler.SelectedIndexChanged += (_, _) => SekmeleriBoya();

        txtHost.Text = ayar.VizHost;
        txtPort.Text = ayar.VizPort.ToString(CultureInfo.InvariantCulture);
        dtBas.Value = DateTime.Today;
        dtBit.Value = DateTime.Today.AddDays(7);
        _viz.DurumDegisti += bagli => BeginInvoke(() =>
        {
            VizDurumu(bagli ? "● Bağlı" : "● Bağlı değil", bagli ? YaziSari : Soluk);
            Durum();
        });

        // Canlı maç sekmesi: gol atakları yalnızca yerel htspor_tahmin veritabanına kaydedilir
        AtakDeposu? depo = null;
        string? depoHata = null;
        try { depo = new AtakDeposu(ayar.Veri); }
        catch (Exception ex) { depoHata = ex.Message; }
        CanliBaslat(depo, depoHata);

        HizSec(1);
        SekmeSec(false);
        Durum();
    }

    // ---------------------------------------------------------------- sekmeler

    void btnSekmeSim_Click(object? sender, EventArgs e) => SekmeSec(false);
    void btnSekmeCanli_Click(object? sender, EventArgs e) => SekmeSec(true);

    void SekmeSec(bool canliMac)
    {
        if (canliMac && _oynuyor) Duraklat();
        tabSekmeler.SelectedTab = canliMac ? tabCanli : tabSim;
        SekmeleriBoya();
    }

    void SekmeleriBoya()
    {
        bool canliMac = tabSekmeler.SelectedTab == tabCanli;
        Vurgula(btnSekmeSim, canliMac ? null : Mavi);
        Vurgula(btnSekmeCanli, canliMac ? Mavi : null);
    }

    // ---------------------------------------------------------------- tasarım ekranının bağladığı olaylar

    async void AnaForm_Shown(object? sender, EventArgs e) => await Baslat();
    void AnaForm_FormClosing(object? sender, FormClosingEventArgs e) { tik.Stop(); durumTik.Stop(); _viz.Dispose(); }
    async void btnBaglan_Click(object? sender, EventArgs e) => await Baglan();
    async void btnAnaliz_Click(object? sender, EventArgs e) => await AnalizEt();
    void btnYukle_Click(object? sender, EventArgs e) => VizeYukle();
    void btnOynat_Click(object? sender, EventArgs e) => Oynat();
    void btnDuraklat_Click(object? sender, EventArgs e) => Duraklat();
    void btnBastan_Click(object? sender, EventArgs e) => Bastan();
    void btnTemizle_Click(object? sender, EventArgs e) => Temizle();
    void btnHiz1_Click(object? sender, EventArgs e) => HizSec(1);
    void btnHiz2_Click(object? sender, EventArgs e) => HizSec(2);
    void btnHiz4_Click(object? sender, EventArgs e) => HizSec(4);
    async void cmbTurnuva_SelectedIndexChanged(object? sender, EventArgs e) => await TurnuvaDegisti();
    void cmbAsama_SelectedIndexChanged(object? sender, EventArgs e) => HaftalariDoldur();
    async void cmbHafta_SelectedIndexChanged(object? sender, EventArgs e) => await MaclariYukle();
    async void tarih_ValueChanged(object? sender, EventArgs e) => await MaclariYukle();
    void txtAra_TextChanged(object? sender, EventArgs e) => ListeyiCiz();
    void lstMac_SelectedIndexChanged(object? sender, EventArgs e) => MacSecildi();
    async void lstMac_DoubleClick(object? sender, EventArgs e) => await AnalizEt();
    void tik_Tick(object? sender, EventArgs e) => Tik();
    async void durumTik_Tick(object? sender, EventArgs e) => await SahneDurumu();

    // ---------------------------------------------------------------- kısayollar

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (tabSekmeler.SelectedTab == tabCanli)
        {
            if (CanliTusIsle(e)) { e.Handled = true; e.SuppressKeyPress = true; }
            return;
        }
        switch (e.KeyCode)
        {
            case Keys.F2: _ = AnalizEt(); break;
            case Keys.F3: VizeYukle(); break;
            case Keys.F5: Oynat(); break;
            case Keys.F6: Duraklat(); break;
            case Keys.F7: Bastan(); break;
            case Keys.F8: Temizle(); break;
            default: return;
        }
        e.Handled = true;
    }

    // ---------------------------------------------------------------- başlangıç ve maç listesi

    async Task Baslat()
    {
        try
        {
            var (turnuvalar, sonVeri) = await Task.Run(() => (_veri.GetCompetitions(), _veri.DataUntil));
            lblVeri.Text = sonVeri != null ? $"Veri {sonVeri:dd.MM.yyyy} tarihine kadar" : "Veri: test";
            cmbTurnuva.Items.Add(new Secenek(0, "Tüm turnuvalar (tarih aralığı)"));
            foreach (var t in turnuvalar) cmbTurnuva.Items.Add(new Secenek(t.Id, t.Name));
            cmbTurnuva.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            lblVeri.Text = "Veritabanı hatası";
            lblVeri.ForeColor = YaziKirmizi;
            Gunluk("HATA: veritabanına bağlanılamadı: " + ex.Message);
        }
        await Baglan();
        durumTik.Start();
    }

    async Task TurnuvaDegisti()
    {
        if (cmbTurnuva.SelectedItem is not Secenek t) return;
        bool hepsi = t.Id == 0;
        pnlTarih.Visible = hepsi;
        pnlHafta.Visible = !hepsi;
        if (hepsi) { await MaclariYukle(); return; }
        _asamalar = await Task.Run(() => _veri.GetStages(t.Id));
        cmbAsama.Items.Clear();
        foreach (var s in _asamalar) cmbAsama.Items.Add(new Secenek(s.Id, s.Name));
        int cur = _asamalar.FindIndex(s => s.Current);
        cmbAsama.SelectedIndex = _asamalar.Count == 0 ? -1 : Math.Max(0, cur);
    }

    void HaftalariDoldur()
    {
        cmbHafta.Items.Clear();
        if (cmbAsama.SelectedIndex < 0) return;
        var asama = _asamalar[cmbAsama.SelectedIndex];
        foreach (var r in asama.Rounds)
            cmbHafta.Items.Add(new Secenek(0, r.Name, $"{r.Name}  ·  {r.First:dd.MM.yy}" + (r.Played == r.Matches ? "" : r.Played > 0 ? $"  ·  {r.Played}/{r.Matches}" : "  ·  oynanmadı")));
        int cur = asama.Rounds.FindIndex(r => r.Current);
        cmbHafta.SelectedIndex = asama.Rounds.Count == 0 ? -1 : Math.Max(0, cur);
    }

    async Task MaclariYukle()
    {
        if (cmbTurnuva.SelectedItem is not Secenek t) return;
        try
        {
            if (t.Id == 0)
            {
                DateTime bas = dtBas.Value.Date, bit = dtBit.Value.Date.AddDays(1);
                if (bit <= bas) bit = bas.AddDays(1);
                _maclar = await Task.Run(() => _veri.GetMatches(bas, bit));
            }
            else
            {
                if (cmbAsama.SelectedIndex < 0 || cmbHafta.SelectedItem is not Secenek h) { _maclar = new(); ListeyiCiz(); return; }
                int asama = _asamalar[cmbAsama.SelectedIndex].Id;
                _maclar = await Task.Run(() => _veri.GetRoundMatches(t.Id, asama, h.Ad));
            }
        }
        catch (Exception ex)
        {
            Gunluk("HATA: maçlar okunamadı: " + ex.Message);
            _maclar = new();
        }
        ListeyiCiz();
    }

    void ListeyiCiz()
    {
        var q = txtAra.Text.Trim().ToLower(Tr);
        lstMac.BeginUpdate();
        lstMac.Items.Clear();
        foreach (var f in _maclar)
        {
            var ad = f.HomeName + " – " + f.AwayName;
            if (q.Length > 0 && !ad.ToLower(Tr).Contains(q)) continue;
            var it = new ListViewItem(f.MatchDate.ToString("dd.MM HH:mm")) { Tag = f, ToolTipText = f.CompetitionName };
            it.SubItems.Add(ad);
            it.SubItems.Add(f.Status == "played" ? $"{f.HomeGoals}-{f.AwayGoals}" : "");
            if (_secili != null && f.Id == _secili.Id) it.Selected = true;
            lstMac.Items.Add(it);
        }
        lstMac.EndUpdate();
    }

    void MacSecildi()
    {
        if (lstMac.SelectedItems.Count == 0) return;
        var f = (FixtureRow)lstMac.SelectedItems[0].Tag!;
        if (_secili?.Id == f.Id) return;
        if (_oynuyor) Duraklat();
        _secili = f;
        _oynatici = null;
        _yuklendi = false;
        saha.Oynatici = null;
        saha.Invalidate();
        lblBilgi.Text = $"{f.HomeName} – {f.AwayName}\n{f.CompetitionName}\n{f.MatchDate:dd.MM.yyyy HH:mm}\n\nANALİZ ET'e basın (F2).";
        lblTabela.Text = $"{f.HomeName}   –   {f.AwayName}";
        Durum();
    }

    // ---------------------------------------------------------------- adımlar

    async Task AnalizEt()
    {
        if (_secili == null) { Gunluk("Önce soldan bir maç seçin."); return; }
        if (_analizde) return;
        var f = _secili;
        _analizde = true;
        Durum();
        lblBilgi.Text = "10.000 simülasyon çalışıyor…";
        try
        {
            var sonuc = await Task.Run(() => _analiz.Compute(f.Id));
            if (_secili?.Id != f.Id) return;
            if (sonuc == null) { Gunluk("HATA: maç bulunamadı."); return; }
            if (_oynuyor) Duraklat();
            _oynatici = new MatchPlayer(sonuc) { Speed = _hiz };
            _yuklendi = false;
            var (ev, dep) = VizYayin.TakimRenkleri(sonuc.Match.Home.Color, sonuc.Match.Away.Color);
            saha.EvRengi = Color.FromArgb(ev.R, ev.G, ev.B);
            saha.DepRengi = Color.FromArgb(dep.R, dep.G, dep.B);
            saha.Oynatici = _oynatici;
            saha.Invalidate();
            var p = sonuc.Probabilities;
            var kadro = sonuc.Lineups == null ? "yok" : sonuc.Lineups.Home.Confirmed ? "açıklanan 11" : "muhtemel 11";
            lblBilgi.Text =
                $"{sonuc.Match.Home.Name}   %{p.Home:0.#}\nBeraberlik   %{p.Draw:0.#}\n{sonuc.Match.Away.Name}   %{p.Away:0.#}\n\n" +
                $"Beklenen gol   {sonuc.Model.ExpectedGoalsHome:0.00} - {sonuc.Model.ExpectedGoalsAway:0.00}\n" +
                $"Oynatılacak maç   {sonuc.Simulation.HomeGoals} - {sonuc.Simulation.AwayGoals}\n" +
                $"Kadro   {kadro}\nSüre (1x)   {_oynatici.DurationSeconds:0} sn";
            TabelaYaz();
            Gunluk($"Analiz hazır: {sonuc.Match.Home.Name} – {sonuc.Match.Away.Name}, oynatılacak skor {sonuc.Simulation.HomeGoals}-{sonuc.Simulation.AwayGoals}.");
        }
        catch (Exception ex)
        {
            Gunluk("HATA: analiz yapılamadı: " + ex.Message);
            lblBilgi.Text = "Analiz yapılamadı.";
        }
        finally
        {
            _analizde = false;
            Durum();
        }
    }

    void VizeYukle()
    {
        if (_oynatici == null) { Gunluk("Önce ANALİZ ET (F2)."); return; }
        if (!_viz.Bagli) { Gunluk("Viz bağlı değil. Önce BAĞLAN."); return; }
        if (_oynuyor) Duraklat();
        _oynatici.Reset();
        _yayin.Yukle(_oynatici);
        _yuklendi = true;
        saha.Invalidate();
        TabelaYaz();
        Gunluk("Viz'e yüklendi: kadro, renkler, tabela ve başlama dizilişi gönderildi.");
        Durum();
    }

    void Oynat()
    {
        if (_oynatici == null) { Gunluk("Önce ANALİZ ET (F2)."); return; }
        if (_oynuyor) return;
        if (_viz.Bagli && !_yuklendi) { Gunluk("Önce VİZ'E YÜKLE (F3)."); return; }
        if (_oynatici.Done)
        {
            _oynatici.Reset();
            if (_viz.Bagli) _yayin.Yukle(_oynatici);
        }
        if (!_viz.Bagli) Gunluk("Viz bağlı değil: yalnızca önizleme oynatılıyor.");
        _oynuyor = true;
        _saat.Restart();
        _sonAdim = 0;
        _sonGonderim = -1;
        tik.Start();
        Durum();
    }

    void Duraklat()
    {
        _oynuyor = false;
        tik.Stop();
        Durum();
    }

    void Bastan()
    {
        if (_oynatici == null) return;
        Duraklat();
        _oynatici.Reset();
        if (_viz.Bagli && _yuklendi) _yayin.Yukle(_oynatici);
        saha.Invalidate();
        TabelaYaz();
        Gunluk("Baştan alındı.");
    }

    void Temizle()
    {
        if (!_viz.Bagli) return;
        _yayin.Temizle();
        Gunluk("Bant ve etiket kapatıldı.");
    }

    void HizSec(double hiz)
    {
        _hiz = hiz;
        if (_oynatici != null) _oynatici.Speed = hiz;
        Vurgula(btnHiz1, hiz == 1 ? Mavi : null);
        Vurgula(btnHiz2, hiz == 2 ? Mavi : null);
        Vurgula(btnHiz4, hiz == 4 ? Mavi : null);
    }

    // ---------------------------------------------------------------- oynatma döngüsü

    void Tik()
    {
        var p = _oynatici;
        if (!_oynuyor || p == null) return;
        p.Speed = _hiz;
        double simdi = _saat.Elapsed.TotalSeconds, dt = simdi - _sonAdim;
        _sonAdim = simdi;
        bool viz = _viz.Bagli && _yuklendi;
        foreach (var e in p.Step(dt))
        {
            if (viz) _yayin.Olay(p, e);
            OlayYaz(p, e);
        }
        if (viz && simdi - _sonGonderim >= 1.0 / _ayar.GonderimHz)
        {
            _yayin.Kare(p);
            _sonGonderim = simdi;
        }
        if (p.Done)
        {
            if (viz) { _yayin.Kare(p); _yayin.MacSonu(p); }
            Gunluk($"Maç sonu: {p.HomeGoals} - {p.AwayGoals}");
            Duraklat();
        }
        TabelaYaz();
        saha.Invalidate();
    }

    void OlayYaz(MatchPlayer p, PlayEvent e)
    {
        var m = p.Analysis.Match;
        string takim = e.Side == "home" ? m.Home.Short : m.Away.Short;
        string ne = e.Type switch
        {
            "goal" => $"GOL!  {e.HomeGoals}-{e.AwayGoals}  {e.Player}",
            "shot_on" => $"İsabetli şut  {e.Player}",
            "shot_off" => $"Şut dışarıda  {e.Player}",
            "corner" => "Korner",
            "yellow" => $"Sarı kart  {e.Player}",
            "red" => $"KIRMIZI KART  {e.Player}",
            "sub" => $"Değişiklik  giren {e.Player}  çıkan {e.OutPlayer}",
            _ => e.Type,
        };
        Gunluk($"{MatchPlayer.SlotLabel(e.Slot),-7}{takim,-5}{ne}");
    }

    void TabelaYaz()
    {
        var p = _oynatici;
        if (p == null) return;
        var m = p.Analysis.Match;
        string dk = p.Done ? "MS" : p.T <= 0 ? "0'" : MatchPlayer.SlotLabel(p.Slot);
        lblTabela.Text = $"{m.Home.Name}    {p.HomeGoals} - {p.AwayGoals}    {m.Away.Name}        {dk}";
    }

    // ---------------------------------------------------------------- Viz

    async Task Baglan()
    {
        if (!int.TryParse(txtPort.Text.Trim(), out int port)) { Gunluk("Port sayı olmalı."); return; }
        var host = txtHost.Text.Trim();
        VizDurumu("● Bağlanıyor…", Soluk);
        bool ok = await _viz.BaglanAsync(host, port);
        if (!ok)
        {
            VizDurumu("● Bağlanamadı", YaziKirmizi);
            Gunluk($"HATA: Viz'e bağlanılamadı ({host}:{port}). Viz Engine açık mı?");
        }
        else
        {
            Gunluk($"Viz'e bağlandı ({host}:{port}).");
            _yuklendi = false;
            await SahneDurumu();
        }
        Durum();
    }

    // Sahnedeki betik her saniye durumunu "sim_durum" anahtarına yazar; okuyup sahnenin hazır olduğunu anlarız
    bool _sahneEski;

    async Task SahneDurumu()
    {
        if (!_viz.Bagli) { if (!lblViz.Text.Contains("Bağlanamadı")) VizDurumu("● Bağlı değil", Soluk); Durum(); return; }
        var yanit = await _viz.SorAsync("VIZ_COMMUNICATION*MAP GET_STRING_ELEMENT \"sim_durum\"", 1500);
        if (yanit == null) VizDurumu("● Bağlı · yanıt yok", YaziSari);
        else if (yanit.Contains("bagli=1") && !yanit.Contains("atak=1"))
        {
            VizDurumu("● Bağlı · sahne ESKİ: betiği güncelleyip 'Sahneyi kur'a basın", YaziSari);
            _sahneEski = true;
        }
        else if (yanit.Contains("bagli=1")) { VizDurumu("● Bağlı · mac_sim sahnesi hazır", YaziYesil); _sahneEski = false; }
        else VizDurumu("● Bağlı · mac_sim sahnesi yanıt vermiyor", YaziSari);
        Durum();
    }

    void VizDurumu(string yazi, Color renk) { lblViz.Text = yazi; lblViz.ForeColor = renk; }

    // ---------------------------------------------------------------- düğme durumları

    void Durum()
    {
        bool analiz = _oynatici != null;
        bool hazir = analiz && (!_viz.Bagli || _yuklendi);
        btnAnaliz.Enabled = _secili != null && !_analizde;
        btnYukle.Enabled = analiz && _viz.Bagli;
        btnOynat.Enabled = analiz && !_oynuyor && (!_viz.Bagli || _yuklendi);
        btnDuraklat.Enabled = _oynuyor;
        btnBastan.Enabled = analiz;
        btnTemizle.Enabled = _viz.Bagli;
        btnBaglan.Text = _viz.Bagli ? "YENİDEN" : "BAĞLAN";

        // Yalnızca sıradaki adım renklenir: göz neye basacağını arar, her şey parlarsa yorulur
        Vurgula(btnAnaliz, _secili != null && !analiz ? Mavi : null);
        Vurgula(btnYukle, analiz && _viz.Bagli && !_yuklendi ? Mavi : null);
        Vurgula(btnOynat, hazir && !_oynuyor ? Yesil : null);
    }

    void Vurgula(Button b, Color? renk)
    {
        b.BackColor = renk ?? Dugme;
        b.FlatAppearance.MouseOverBackColor = renk == null ? DugmeUstu : renk == Yesil ? YesilUstu : MaviUstu;
        b.FlatAppearance.BorderColor = renk ?? gorunum.KenarRengi;
        b.ForeColor = Yazi;
    }

    void Gunluk(string satir)
    {
        lstAkis.Items.Insert(0, $"{DateTime.Now:HH:mm:ss}   {satir}");
        while (lstAkis.Items.Count > 300) lstAkis.Items.RemoveAt(lstAkis.Items.Count - 1);
    }

    // ---------------------------------------------------------------- liste ve akışın çizimi (Windows'un açık renkli başlığı yerine)

    void lstMac_DrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
    {
        using (var b = new SolidBrush(Yuzey)) e.Graphics.FillRectangle(b, e.Bounds);
        var r = Rectangle.Inflate(e.Bounds, -6, 0);
        using var f = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
        TextRenderer.DrawText(e.Graphics, e.Header?.Text ?? "", f, r, Soluk, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
    }

    void lstMac_DrawItem(object? sender, DrawListViewItemEventArgs e) => e.DrawDefault = false;

    void lstMac_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        if (e.Item == null || e.SubItem == null) return;
        var arka = e.Item.Selected ? Mavi : e.ItemIndex % 2 == 0 ? Satir : Satir2;
        using (var b = new SolidBrush(arka)) e.Graphics.FillRectangle(b, e.Bounds);
        var renk = e.ColumnIndex == 1 || e.Item.Selected ? Yazi : Soluk;
        var r = Rectangle.Inflate(e.Bounds, -6, 0);
        TextRenderer.DrawText(e.Graphics, e.SubItem.Text, lstMac.Font, r, renk,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    void lstAkis_DrawItem(object? sender, DrawItemEventArgs e)
    {
        using (var b = new SolidBrush(Yuzey)) e.Graphics.FillRectangle(b, e.Bounds);
        if (e.Index < 0) return;
        var s = lstAkis.Items[e.Index]?.ToString() ?? "";
        var renk = s.Contains("HATA") ? YaziKirmizi
            : s.Contains("GOL!") ? YaziYesil
            : s.Contains("KIRMIZI") || s.Contains("Sarı kart") ? YaziSari
            : e.Index == 0 ? Yazi : Soluk;
        var r = new Rectangle(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 10, e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, s, lstAkis.Font, r, renk, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
    }

    sealed record Secenek(int Id, string Ad, string? Gorunen = null)
    {
        public override string ToString() => Gorunen ?? Ad;
    }
}
