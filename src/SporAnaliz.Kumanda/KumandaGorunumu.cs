using System.ComponentModel;

namespace SporAnaliz.Kumanda;

/// <summary>
/// Kumandanın kodla boyanan kısımlarının görünümü: vurgu renkleri, liste satırları, durum yazıları, forma düğmeleri.
/// Tasarım ekranında pencerenin altındaki bileşen tepsisinde durur; Properties penceresinden değiştirilir.
/// (Sabit düğme ve panel renkleri zaten kendi özelliklerinde: onlara tasarım ekranında tıklayın.)
/// </summary>
[ToolboxItem(true)]
[Description("Kumandanın kodla boyanan kısımlarının renk ve boyutları")]
public sealed class KumandaGorunumu : Component
{
    public KumandaGorunumu() { }
    public KumandaGorunumu(IContainer container) => container.Add(this);

    // ---- genel
    [Category("Genel"), Description("Pencere zemini"), DefaultValue(typeof(Color), "24, 27, 33")]
    public Color ZeminRengi { get; set; } = Color.FromArgb(24, 27, 33);

    [Category("Genel"), Description("Panel ve başlık şeritleri"), DefaultValue(typeof(Color), "31, 35, 43")]
    public Color YuzeyRengi { get; set; } = Color.FromArgb(31, 35, 43);

    [Category("Genel"), Description("Seçili olmayan düğme"), DefaultValue(typeof(Color), "42, 47, 57")]
    public Color DugmeRengi { get; set; } = Color.FromArgb(42, 47, 57);

    [Category("Genel"), Description("Fare üstündeyken düğme"), DefaultValue(typeof(Color), "52, 58, 70")]
    public Color DugmeUstuRengi { get; set; } = Color.FromArgb(52, 58, 70);

    [Category("Genel"), Description("Düğme kenarı"), DefaultValue(typeof(Color), "58, 64, 77")]
    public Color KenarRengi { get; set; } = Color.FromArgb(58, 64, 77);

    [Category("Genel"), Description("Ana yazı"), DefaultValue(typeof(Color), "226, 230, 236")]
    public Color YaziRengi { get; set; } = Color.FromArgb(226, 230, 236);

    [Category("Genel"), Description("İkincil yazı, başlıklar"), DefaultValue(typeof(Color), "140, 149, 163")]
    public Color SolukYaziRengi { get; set; } = Color.FromArgb(140, 149, 163);

    // ---- vurgu
    [Category("Vurgu"), Description("Sıradaki adım, seçili sekme, seçili hız, seçili satır"), DefaultValue(typeof(Color), "58, 110, 168")]
    public Color VurguRengi { get; set; } = Color.FromArgb(58, 110, 168);

    [Category("Vurgu"), Description("Vurgulu düğmenin fare üstü rengi"), DefaultValue(typeof(Color), "68, 122, 182")]
    public Color VurguUstuRengi { get; set; } = Color.FromArgb(68, 122, 182);

    [Category("Vurgu"), Description("OYNAT düğmesi hazırken"), DefaultValue(typeof(Color), "52, 130, 92")]
    public Color OynatRengi { get; set; } = Color.FromArgb(52, 130, 92);

    [Category("Vurgu"), Description("OYNAT düğmesinin fare üstü rengi"), DefaultValue(typeof(Color), "62, 144, 104")]
    public Color OynatUstuRengi { get; set; } = Color.FromArgb(62, 144, 104);

    // ---- listeler
    [Category("Listeler"), Description("Liste satırı (tek)"), DefaultValue(typeof(Color), "36, 40, 49")]
    public Color SatirRengi { get; set; } = Color.FromArgb(36, 40, 49);

    [Category("Listeler"), Description("Liste satırı (çift)"), DefaultValue(typeof(Color), "40, 44, 54")]
    public Color Satir2Rengi { get; set; } = Color.FromArgb(40, 44, 54);

    // ---- durum yazıları
    [Category("Durum yazıları"), Description("Hata, bağlantı kopuk, eksik bilgi"), DefaultValue(typeof(Color), "222, 120, 120")]
    public Color HataRengi { get; set; } = Color.FromArgb(222, 120, 120);

    [Category("Durum yazıları"), Description("Gol, sahne hazır, gönderildi"), DefaultValue(typeof(Color), "124, 196, 150")]
    public Color BasariRengi { get; set; } = Color.FromArgb(124, 196, 150);

    [Category("Durum yazıları"), Description("Kart, uyarı, yanıt yok"), DefaultValue(typeof(Color), "214, 180, 100")]
    public Color UyariRengi { get; set; } = Color.FromArgb(214, 180, 100);

    // ---- oyuncu düğmeleri (canlı maç)
    [Category("Oyuncu düğmeleri"), Description("Forma düğmesinin boyutu"), DefaultValue(typeof(Size), "134, 32")]
    public Size OyuncuDugmeBoyutu { get; set; } = new(134, 32);

    [Category("Oyuncu düğmeleri"), Description("İlk 11 oyuncularının yazı tipi"), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Font OyuncuFontu { get; set; } = new("Segoe UI", 9.25f, FontStyle.Bold);

    [Category("Oyuncu düğmeleri"), Description("Yedeklerin yazı tipi"), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Font YedekFontu { get; set; } = new("Segoe UI", 9.25f);

    [Category("Oyuncu düğmeleri"), Description("Yedeklerin yazı rengi"), DefaultValue(typeof(Color), "140, 149, 163")]
    public Color YedekYaziRengi { get; set; } = Color.FromArgb(140, 149, 163);

    [Category("Oyuncu düğmeleri"), Description("Seçili noktanın oyuncusu"), DefaultValue(typeof(Color), "58, 110, 168")]
    public Color SeciliOyuncuRengi { get; set; } = Color.FromArgb(58, 110, 168);
}
