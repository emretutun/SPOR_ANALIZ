using System.Globalization;
using SporAnaliz.Core.Abstractions;
using SporAnaliz.Core.Domain;
using SporAnaliz.Core.Playback;
using SporAnaliz.Data.Atak;
using SporAnaliz.Kumanda.Viz;

namespace SporAnaliz.Kumanda;

/// <summary>
/// Ana pencerenin CANLI MAÇ sekmesi: operatör golün atağını sahaya çizer, program kaydeder ve Viz'e gönderir.
/// Akış: maçı seç → golü atan takımı ve dakikayı gir → atağı çiz → VİZ'E GÖNDER (F5).
/// Yerleşim AnaForm.Designer.cs'te (tabCanli sekmesi); burada yalnızca davranış var.
/// </summary>
public sealed partial class AnaForm
{
    AtakDeposu? _depo;

    List<FixtureRow> _cMaclar = new();
    FixtureRow? _mac;
    MatchLineups? _kadro;
    bool _evAtti = true;
    int _kayitId;
    bool _yukleniyor;
    string _numara = "";
    readonly System.Windows.Forms.Timer _numaraTik = new() { Interval = 900 };
    // Fikstür (skor, dakika, durum) 30 saniyede bir veritabanından yenilenir
    readonly System.Windows.Forms.Timer _fiksturTik = new() { Interval = 30000 };
    bool _fiksturOkunuyor;

    /// <summary>Ana pencere açılınca çağrılır. depoHata doluysa kayıt kapalıdır (çizim ve gönderim çalışır).</summary>
    void CanliBaslat(AtakDeposu? depo, string? depoHata)
    {
        _numaraTik.Tick += (_, _) => NumarayiUygula();
        _fiksturTik.Tick += async (_, _) => await CanliMaclariYukle();
        Disposed += (_, _) => { _numaraTik.Dispose(); _fiksturTik.Dispose(); };
        _depo = depo;
        if (depoHata != null) CanliDurum("Kayıt kapalı: " + depoHata, YaziKirmizi);
        // Bugünden itibaren bütün fikstür: o an oynanan maçlar en üstte
        _yukleniyor = true;
        dtGun.Value = DateTime.Today;
        _yukleniyor = false;
        _ = CanliMaclariYukle();
        _fiksturTik.Start();
        TakimDugmeleri();
        CanliGuncelle();
    }

    // ---------------------------------------------------------------- tasarım ekranının bağladığı olaylar

    async void dtGun_ValueChanged(object? sender, EventArgs e) { if (!_yukleniyor) await CanliMaclariYukle(); }
    void cTxtAra_TextChanged(object? sender, EventArgs e) => CanliListeyiCiz();
    async void lstMaclar_SelectedIndexChanged(object? sender, EventArgs e) => await MacSec();
    void btnEv_Click(object? sender, EventArgs e) => TakimSec(true);
    void btnDep_Click(object? sender, EventArgs e) => TakimSec(false);
    void bilgi_Changed(object? sender, EventArgs e) => CanliGuncelle();
    void btnGeriAl_Click(object? sender, EventArgs e) => cizim.GeriAl();
    void cBtnTemizle_Click(object? sender, EventArgs e) => cizim.Temizle();
    void btnOnizle_Click(object? sender, EventArgs e) => cizim.Onizle();
    void btnYeni_Click(object? sender, EventArgs e) => YeniGol();
    void btnGonder_Click(object? sender, EventArgs e) => Gonder();
    void btnKaldir_Click(object? sender, EventArgs e) => Kaldir();
    void btnSil_Click(object? sender, EventArgs e) => KaydiSil();
    void lstKayitlar_SelectedIndexChanged(object? sender, EventArgs e) => KaydiAc();
    void cizim_Degisti(object? sender, EventArgs e) => CanliGuncelle();

    // ---------------------------------------------------------------- klavye (ana pencere iletir)

    /// <summary>F4 önizle, F5 gönder, F8 kaldır, Ctrl+Z geri al, rakamlar + Enter: seçili noktaya o numaralı oyuncu.</summary>
    bool CanliTusIsle(KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.F4: cizim.Onizle(); return true;
            case Keys.F5: Gonder(); return true;
            case Keys.F8: Kaldir(); return true;
        }
        if (YaziAlanindaMi()) return false;
        if (e.Control && e.KeyCode == Keys.Z) { cizim.GeriAl(); return true; }
        if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back) { if (_numara.Length > 0) _numara = _numara[..^1]; else cizim.GeriAl(); CanliGuncelle(); return true; }
        int rakam = e.KeyCode is >= Keys.D0 and <= Keys.D9 ? e.KeyCode - Keys.D0 : e.KeyCode is >= Keys.NumPad0 and <= Keys.NumPad9 ? e.KeyCode - Keys.NumPad0 : -1;
        if (rakam >= 0 && !e.Control && !e.Alt)
        {
            if (_numara.Length >= 2) _numara = "";
            _numara += rakam;
            _numaraTik.Stop();
            _numaraTik.Start();
            CanliGuncelle();
            return true;
        }
        if (e.KeyCode == Keys.Enter && _numara.Length > 0) { NumarayiUygula(); return true; }
        if (e.KeyCode == Keys.Escape) { _numara = ""; CanliGuncelle(); return true; }
        return false;
    }

    bool YaziAlanindaMi()
    {
        Control? c = FindForm()?.ActiveControl;
        while (c is ContainerControl cc && cc.ActiveControl != null) c = cc.ActiveControl;
        return c is TextBoxBase or NumericUpDown or DateTimePicker || c?.Parent is NumericUpDown;
    }

    void NumarayiUygula()
    {
        _numaraTik.Stop();
        if (_numara.Length == 0) return;
        int no = int.Parse(_numara, CultureInfo.InvariantCulture);
        _numara = "";
        if (cizim.Dokunuslar.Count == 0) { CanliDurum("Önce sahaya tıklayın, sonra numarayı yazın.", YaziKirmizi); return; }
        var o = TakimOyunculari().FirstOrDefault(p => p.Number == no);
        if (o != null) cizim.OyuncuAta(o.Id, o.Number, o.ShortName);
        else if (_kadro == null) cizim.OyuncuAta(-no, no, "");
        else CanliDurum($"{no} numara kadroda yok.", YaziKirmizi);
    }

    // ---------------------------------------------------------------- maç seçimi

    async Task CanliMaclariYukle()
    {
        if (_veri == null || _fiksturOkunuyor) return;
        var gun = dtGun.Value.Date;
        _fiksturOkunuyor = true;
        try
        {
            // Seçilen günden itibaren bir yıllık fikstür (canlı, biten, oynanacak)
            _cMaclar = await Task.Run(() => _veri.GetDayMatches(gun, gun.AddYears(1)));
        }
        catch (Exception ex)
        {
            CanliDurum("Maçlar okunamadı: " + ex.Message, YaziKirmizi);
        }
        finally { _fiksturOkunuyor = false; }
        CanliListeyiCiz();
    }

    void CanliListeyiCiz()
    {
        var q = cTxtAra.Text.Trim().ToLower(Tr);
        int ust = lstMaclar.TopIndex;               // yenilemede liste kaydırılmış yerinde kalsın
        lstMaclar.BeginUpdate();
        lstMaclar.Items.Clear();
        foreach (var f in _cMaclar)
            if (q.Length == 0 || (f.HomeName + " " + f.AwayName).ToLower(Tr).Contains(q)) lstMaclar.Items.Add(f);
        if (_mac != null)
            for (int i = 0; i < lstMaclar.Items.Count; i++)
                if (((FixtureRow)lstMaclar.Items[i]).Id == _mac.Id) { _yukleniyor = true; lstMaclar.SelectedIndex = i; _yukleniyor = false; }
        if (ust > 0 && ust < lstMaclar.Items.Count) lstMaclar.TopIndex = ust;
        lstMaclar.EndUpdate();
        if (_cMaclar.Count == 0 && _mac == null) CanliDurum("Bu tarihten sonra maç yok. Soldan başka bir gün seçin.", Soluk);
    }

    async Task MacSec()
    {
        if (_yukleniyor || _veri == null || lstMaclar.SelectedItem is not FixtureRow f || f.Id == _mac?.Id) return;
        _mac = f;
        _kadro = null;
        _yukleniyor = true;
        numEv.Value = Math.Min(numEv.Maximum, f.HomeGoals ?? 0);
        numDep.Value = Math.Min(numDep.Maximum, f.AwayGoals ?? 0);
        _yukleniyor = false;
        _evAtti = true;
        YeniGol();
        TakimDugmeleri();
        CanliDurum("Kadro okunuyor…", Soluk);
        try
        {
            var kadro = await Task.Run(() => _veri.GetLineups(f));
            if (_mac?.Id != f.Id) return;
            _kadro = kadro;
            CanliDurum(kadro == null ? "Bu maçın kadrosu yok: oyuncu numarasını klavyeden yazabilirsiniz." : "Golü atan takımı seçip atağı çizin.", kadro == null ? YaziKirmizi : Soluk);
        }
        catch (Exception ex)
        {
            CanliDurum("Kadro okunamadı: " + ex.Message, YaziKirmizi);
        }
        OyuncuDugmeleri();
        KayitlariYukle();
        CanliGuncelle();
    }

    void TakimSec(bool ev)
    {
        if (_mac == null || ev == _evAtti) return;
        _evAtti = ev;
        // Çizilen noktalar kalır, oyuncuları diğer takımdan olduğu için silinir
        cizim.Yukle(cizim.Dokunuslar.Select(d => d with { OyuncuId = 0, Numara = 0, Ad = "" }).ToList());
        TakimDugmeleri();
        OyuncuDugmeleri();
        CanliGuncelle();
    }

    (VizYayin.Rgb Ev, VizYayin.Rgb Dep) Renkler() => VizYayin.TakimRenkleri(_mac?.HomeColor ?? "", _mac?.AwayColor ?? "");

    static Color C(VizYayin.Rgb r) => Color.FromArgb(r.R, r.G, r.B);

    void TakimDugmeleri()
    {
        btnEv.Text = _mac?.HomeName ?? "EV SAHİBİ";
        btnDep.Text = _mac?.AwayName ?? "DEPLASMAN";
        var (ev, dep) = Renkler();
        Boya(btnEv, _mac != null && _evAtti ? C(ev) : null);
        Boya(btnDep, _mac != null && !_evAtti ? C(dep) : null);
        cizim.TakimRengi = C(_evAtti ? ev : dep);
        cizim.Invalidate();
    }

    void Boya(Button b, Color? renk)
    {
        b.BackColor = renk ?? Dugme;
        b.FlatAppearance.BorderColor = renk ?? gorunum.KenarRengi;
        b.ForeColor = renk is Color r && SahaCizer.Acik(r) ? Color.FromArgb(17, 17, 17) : Yazi;
    }

    IEnumerable<LineupPlayer> TakimOyunculari()
    {
        var t = _kadro == null ? null : _evAtti ? _kadro.Home : _kadro.Away;
        return t == null ? Enumerable.Empty<LineupPlayer>() : t.Starting.Concat(t.Bench);
    }

    void OyuncuDugmeleri()
    {
        flowOyuncular.SuspendLayout();
        foreach (Control c in flowOyuncular.Controls) c.Dispose();
        flowOyuncular.Controls.Clear();
        var t = _kadro == null ? null : _evAtti ? _kadro.Home : _kadro.Away;
        if (t != null)
        {
            foreach (var o in t.Starting) flowOyuncular.Controls.Add(OyuncuDugmesi(o, false));
            if (t.Bench.Count > 0)
                flowOyuncular.Controls.Add(new Label { Text = "YEDEKLER", ForeColor = Soluk, Font = lblOyuncuBaslik.Font, Size = new Size(270, 22), TextAlign = ContentAlignment.BottomLeft, Margin = new Padding(2, 6, 2, 2) });
            foreach (var o in t.Bench) flowOyuncular.Controls.Add(OyuncuDugmesi(o, true));
        }
        flowOyuncular.ResumeLayout();
        OyuncuVurgula();
    }

    Button OyuncuDugmesi(LineupPlayer o, bool yedek)
    {
        var b = new Button
        {
            Text = $"{o.Number,2}   {o.ShortName}", Tag = o, Size = gorunum.OyuncuDugmeBoyutu, Margin = new Padding(2),
            FlatStyle = FlatStyle.Flat, BackColor = Dugme, ForeColor = yedek ? gorunum.YedekYaziRengi : Yazi, TextAlign = ContentAlignment.MiddleLeft,
            Font = yedek ? gorunum.YedekFontu : gorunum.OyuncuFontu, Cursor = Cursors.Hand, AutoEllipsis = true,
        };
        b.FlatAppearance.BorderColor = gorunum.KenarRengi;
        b.FlatAppearance.MouseOverBackColor = gorunum.DugmeUstuRengi;
        b.Click += (_, _) => cizim.OyuncuAta(o.Id, o.Number, o.ShortName);
        return b;
    }

    // Seçili noktanın oyuncusunun düğmesi mavi yanar
    void OyuncuVurgula()
    {
        var d = cizim.Secili >= 0 && cizim.Secili < cizim.Dokunuslar.Count ? cizim.Dokunuslar[cizim.Secili] : null;
        foreach (Control c in flowOyuncular.Controls)
            if (c is Button b && b.Tag is LineupPlayer o)
            {
                bool secili = d != null && d.Numara == o.Number && d.OyuncuId == o.Id;
                b.BackColor = secili ? gorunum.SeciliOyuncuRengi : Dugme;
                b.FlatAppearance.BorderColor = secili ? gorunum.SeciliOyuncuRengi : gorunum.KenarRengi;
            }
    }

    // ---------------------------------------------------------------- kayıtlar

    void KayitlariYukle()
    {
        lstKayitlar.Items.Clear();
        if (_depo == null || _mac == null) return;
        try
        {
            _yukleniyor = true;
            foreach (var k in _depo.Listele(_mac.Id)) lstKayitlar.Items.Add(k);
            for (int i = 0; i < lstKayitlar.Items.Count; i++)
                if (((GolAtakKaydi)lstKayitlar.Items[i]).Id == _kayitId) lstKayitlar.SelectedIndex = i;
        }
        catch (Exception ex)
        {
            CanliDurum("Kayıtlar okunamadı: " + ex.Message, YaziKirmizi);
        }
        finally { _yukleniyor = false; }
    }

    void KaydiAc()
    {
        if (_yukleniyor || lstKayitlar.SelectedItem is not GolAtakKaydi k) return;
        _kayitId = k.Id;
        _yukleniyor = true;
        _evAtti = k.Takim != "away";
        txtDakika.Text = k.Dakika;
        numEv.Value = Math.Min(numEv.Maximum, k.SkorEv);
        numDep.Value = Math.Min(numDep.Maximum, k.SkorDep);
        _yukleniyor = false;
        TakimDugmeleri();
        OyuncuDugmeleri();
        cizim.Yukle(k.Dokunuslar);
        CanliDurum($"Kayıt açıldı: {k}. Değiştirip yeniden gönderebilirsiniz.", Soluk);
    }

    void KaydiSil()
    {
        if (_depo == null || lstKayitlar.SelectedItem is not GolAtakKaydi k) return;
        if (MessageBox.Show($"{k} kaydı silinsin mi?", "Kaydı sil", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            _depo.Sil(k.Id);
            if (_kayitId == k.Id) YeniGol();
            KayitlariYukle();
            CanliDurum("Kayıt silindi.", Soluk);
        }
        catch (Exception ex) { CanliDurum("Silinemedi: " + ex.Message, YaziKirmizi); }
    }

    void YeniGol()
    {
        _kayitId = 0;
        _yukleniyor = true;
        txtDakika.Text = "";
        lstKayitlar.ClearSelected();
        _yukleniyor = false;
        cizim.Temizle();
    }

    // ---------------------------------------------------------------- Viz

    string Dakika()
    {
        var d = txtDakika.Text.Trim().TrimEnd('\'', '’');
        return d.Length == 0 ? "" : d + "'";
    }

    string Baslik(AtakPlani p)
    {
        var golcu = (p.Golcu?.Ad ?? "").ToUpper(Tr);
        var dk = Dakika();
        var parca = new[] { "GOL", dk, golcu }.Where(s => s.Length > 0);
        return string.Join("  ·  ", parca);
    }

    static string OyuncuSatiri(AtakPlani p) =>
        string.Join("   ·   ", p.Oyuncular.Select(o => $"{o.Numara} {o.Ad}".Trim())) + $"          {p.PasSayisi} PAS";

    void Gonder()
    {
        if (_mac == null) { CanliDurum("Önce soldan maçı seçin.", YaziKirmizi); return; }
        if (cizim.Dokunuslar.Count == 0) { CanliDurum("Önce atağı çizin.", YaziKirmizi); return; }
        if (cizim.OyuncusuEksik) { CanliDurum("Kırmızı '?' noktalara oyuncu seçin.", YaziKirmizi); return; }
        var plan = AtakPlani.Kur(cizim.Dokunuslar);

        // Önce kaydet: yayında bir şey ters giderse çizim kaybolmasın
        string kayit = "";
        if (_depo != null)
        {
            try
            {
                _kayitId = _depo.Kaydet(new GolAtakKaydi
                {
                    Id = _kayitId, MacId = _mac.Id, Takim = _evAtti ? "home" : "away", Dakika = txtDakika.Text.Trim().TrimEnd('\'', '’'),
                    Golcu = plan.Golcu?.Ad ?? "", SkorEv = (int)numEv.Value, SkorDep = (int)numDep.Value, Dokunuslar = cizim.Dokunuslar.ToList(),
                });
                KayitlariYukle();
                kayit = "Kaydedildi. ";
            }
            catch (Exception ex) { kayit = "KAYDEDİLEMEDİ (" + ex.Message + "). "; }
        }

        if (_viz == null || _yayin == null || !_viz.Bagli)
        {
            CanliDurum(kayit + "Viz bağlı değil; yalnızca önizleme oynatılıyor.", YaziKirmizi);
            cizim.Onizle();
            return;
        }
        var (ev, dep) = Renkler();
        _yayin.CanliHazirla(_mac.HomeName, _mac.AwayName, (int)numEv.Value, (int)numDep.Value, Dakika(), ev, dep);
        _yayin.Atak(plan, _evAtti ? ev : dep, Baslik(plan), OyuncuSatiri(plan));
        cizim.Onizle();
        if (_sahneEski)
            CanliDurum(kayit + "Gönderildi ama Viz sahnesi ESKİ: viz/MacSim.vbs'i SIM'e yapıştırıp 'Sahneyi kur'a basın, sonra sahneyi kaydedin.", YaziKirmizi);
        else
            CanliDurum(kayit + $"Viz'de oynuyor ({plan.Toplam:0.0} sn). Kapatmak için VİZ'DEN KALDIR (F8).", YaziYesil);
    }

    void Kaldir()
    {
        if (_yayin == null || _viz == null || !_viz.Bagli) { CanliDurum("Viz bağlı değil.", YaziKirmizi); return; }
        _yayin.AtakKapat();
        cizim.OnizlemeyiDurdur();
        CanliDurum("Atak Viz'den kaldırıldı.", Soluk);
    }

    // ---------------------------------------------------------------- ekran

    void CanliGuncelle()
    {
        if (_yukleniyor) return;
        cLblBaslik.Text = _mac == null ? "—" : $"{_mac.HomeName}    {numEv.Value} - {numDep.Value}    {_mac.AwayName}" + (Dakika().Length > 0 ? $"        {Dakika()}" : "");
        bool hazir = _mac != null && cizim.Dokunuslar.Count > 0 && !cizim.OyuncusuEksik;
        btnGonder.Enabled = hazir;
        btnOnizle.Enabled = cizim.Dokunuslar.Count > 0;
        btnGeriAl.Enabled = cBtnTemizle.Enabled = cizim.Dokunuslar.Count > 0;
        btnSil.Enabled = _depo != null && lstKayitlar.SelectedItem != null;
        btnEv.Enabled = btnDep.Enabled = _mac != null;
        OyuncuVurgula();

        if (_numara.Length > 0) { CanliDurum($"Numara: {_numara}   (Enter ya da kısa bir bekleme)", Yazi); return; }
        if (_mac == null || cLblDurum.ForeColor == YaziKirmizi && cizim.Dokunuslar.Count == 0) return;
        if (cizim.Dokunuslar.Count == 0) CanliDurum("Atağın başladığı yere tıklayın.", Soluk);
        else if (cizim.OyuncusuEksik) CanliDurum("Kırmızı '?' noktaya oyuncu seçin: sağdaki formaya tıklayın ya da numarayı yazın.", Soluk);
        else
        {
            var p = AtakPlani.Kur(cizim.Dokunuslar);
            CanliDurum($"{cizim.Dokunuslar.Count} dokunuş  ·  {p.PasSayisi} pas  ·  golü atan: {p.Golcu?.Ad}.   Hazır: F5 ile Viz'e gönderin.", Soluk);
        }
    }

    void CanliDurum(string yazi, Color renk) { cLblDurum.Text = yazi; cLblDurum.ForeColor = renk; }

    void lstMaclar_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        var f = (FixtureRow)lstMaclar.Items[e.Index];
        bool secili = (e.State & DrawItemState.Selected) != 0;
        using (var b = new SolidBrush(secili ? Mavi : e.Index % 2 == 0 ? Satir : Satir2)) e.Graphics.FillRectangle(b, e.Bounds);
        using var kucuk = new Font("Segoe UI", 8.5f);
        var r = e.Bounds;
        string skor = f.HomeGoals != null && f.AwayGoals != null ? $"   {f.HomeGoals}-{f.AwayGoals}" : "";
        // Üst satır: canlıysa durum ve dakika (yeşil), değilse tarih ve saat; yanında turnuva
        string ust, durum = "";
        Color ustRenk = secili ? Yazi : Soluk;
        if (f.Live)
        {
            durum = f.Minute is int dk and > 0 ? $"● CANLI  {dk}'" + (f.Stoppage is int ek and > 0 ? $"+{ek}" : "") : $"● {f.StatusName.ToUpper(Tr)}";
            ust = $"{durum}  ·  {f.CompetitionName}";
            ustRenk = YaziYesil;
        }
        else
        {
            ust = $"{f.MatchDate.ToString("dd.MM ddd  HH:mm", Tr)}  ·  {f.CompetitionName}";
            if (f.StatusName.Length > 0 && f.Status != "played" && f.Status != "scheduled") ust += "  ·  " + f.StatusName;
        }
        TextRenderer.DrawText(e.Graphics, ust, kucuk, new Rectangle(r.X + 10, r.Y + 4, r.Width - 14, 18), ustRenk, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(e.Graphics, $"{f.HomeName} – {f.AwayName}{skor}", lstMaclar.Font, new Rectangle(r.X + 10, r.Y + 21, r.Width - 14, 22), Yazi, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

}
