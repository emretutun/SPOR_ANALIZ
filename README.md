# SPOR ANALİZ

Seçilen bir futbol maçını geçmiş verilerden analiz eden, 10.000 kez simüle eden ve temsili 90 dakikayı 2D sahada oynatan web uygulaması. Televizyon yayınında yorumcuların analizini desteklemek için tasarlandı.

Bu belge iki şeyi anlatır: programı nasıl çalıştıracağınızı ve modelin baştan sona nasıl çalıştığını. Kod okumadan da anlaşılacak biçimde yazıldı.

**İçindekiler**

1. [Çalıştırma](#1-çalıştırma)
2. [Program ne yapıyor](#2-program-ne-yapıyor)
3. [Veri](#3-veri)
4. [Model: adım adım](#4-model-adım-adım)
5. [Kadro ve oyuncular](#5-kadro-ve-oyuncular)
6. [Çıktılar nasıl üretiliyor](#6-çıktılar-nasıl-üretiliyor)
7. [Sahadaki 2D oynatma](#7-sahadaki-2d-oynatma)
8. [Model ne kadar isabetli](#8-model-ne-kadar-isabetli)
9. [Yolda öğrendiklerimiz](#9-yolda-öğrendiklerimiz)
10. [Modelin bilmediği şeyler](#10-modelin-bilmediği-şeyler)
11. [Kod yapısı](#11-kod-yapısı)
12. [Araçlar](#12-araçlar)
13. [Sıradaki adımlar](#13-sıradaki-adımlar)

---

## 1. Çalıştırma

1. `src/SporAnaliz.Web/appsettings.example.json` dosyasını aynı klasöre `appsettings.json` adıyla kopyalayın, MySQL kullanıcı adı ve şifresini yazın.
2. Visual Studio'da `SPOR_ANALIZ.sln` dosyasını açın, başlangıç projesi olarak **SporAnaliz.Web**'i seçip çalıştırın. Komut satırından:

   ```
   dotnet run --project src/SporAnaliz.Web
   ```

3. Tarayıcıda `http://localhost:5080` adresini açın.

Gereksinimler: .NET 10 ve maç veritabanını barındıran bir MySQL 8. Veritabanının adı `appsettings.json` içinde `Data:Database` ile verilir.

---

## 2. Program ne yapıyor

Sol menüden bir maç seçilir. Program o maç için şunları üretir:

- **Sonuç yüzdeleri:** ev sahibi kazanır / beraberlik / deplasman kazanır.
- **Sonuca göre skorlar:** her sonuç için en olası üç skor.
- **Beklenen maç istatistikleri:** gol, şut, isabetli şut, korner, sarı kart, top hakimiyeti.
- **Gol dakikaları:** golün maçın hangi diliminde beklendiği.
- **Gol beklenen oyuncular:** her oyuncunun maçta en az bir gol atma yüzdesi.
- **Kadrolar:** açıklanan ilk 11 ya da açıklanmadıysa muhtemel 11.
- **Güç karşılaştırması, form, aralarındaki maçlar.**
- **Maçın anahtarı:** sayılardan üretilen kısa yorum cümleleri.
- **Simülasyon:** temsili bir maçın 2D sahada, oyuncu isimleri ve forma numaralarıyla oynatılması.
- **Tahmin – gerçek sonuç karşılaştırması:** oynanmış bir maçta modelin o sonuca ne kadar olasılık verdiği.

Maç seçimi iki yolla yapılır: bir turnuva seçip sezon ve hafta belirlemek, ya da "Tüm turnuvalar"da yaklaşan / sonucu veritabanında olmayan / oynanmış maçları listelemek.

**Yapay zeka yoktur.** Bütün sayılar istatistik modelinden ve simülasyondan gelir; yorum cümleleri sabit şablonlardır.

**Dışarıya bağlantı yoktur.** Program çalışırken internete istek atmaz; yalnızca yerel MySQL'i okur.

---

## 3. Veri

### Kaynak

Yerel bir maç veritabanı: lisanslı bir spor verisi sağlayıcısının akışının kopyası. Adı `Data:Database` ayarıyla verilir. Program bu veritabanını **yalnızca okur**, hiçbir tabloya yazmaz. Ürettiği analizleri bellekte tutar; uygulama kapanınca silinir.

| Tablo | İçerik | Nasıl kullanılıyor |
|---|---|---|
| `soccer_matches` | Maçlar: tarih, takımlar, skor, turnuva, sezon, hafta | Fikstür listesi ve güç hesabı |
| `soccer_stats` | Maç istatistikleri: şut, isabetli şut, korner, kart, top hakimiyeti | İstatistik beklentileri ve güç hesabı |
| `soccer_events` | Maç olayları: gol, kart, değişiklik; oyuncu adıyla | Oyuncuların son dönemdeki golleri |
| `soccer_lineups` | Maç kadroları: ilk 11, yedekler, diziliş, teknik direktör | Kadro, muhtemel 11, saha dizilişi |
| `soccer_players` | Takımların güncel kadroları (71 takım) | Oyuncu listesi |
| `soccer_tournaments`, `soccer_teams` | Turnuva adları, takım ülkeleri | Listeleme ve lig grupları |

### Kapsam

Güç hesabına yalnızca **kulüp turnuvaları** girer: Süper Lig, 1. Lig, Türkiye Kupası, Süper Kupa, Şampiyonlar Ligi, Avrupa Ligi, Konferans Ligi, UEFA Süper Kupa ve beş büyük lig (İngiltere, İspanya, İtalya, Almanya, Fransa). Hazırlık maçları ve milli maçlar dahil değildir.

Toplam 15.714 tamamlanmış kulüp maçı var (Ağustos 2020 – Eylül 2026). Beş büyük ligin verisi Ağustos 2023'te, şut istatistikleri ise Eylül 2024'te başlıyor; bu yüzden şut verisi 6.874 maçta mevcut.

### Bilinmesi gereken veri sınırları

- **Veri 20 Eylül 2026'da duruyor.** Fikstür Mayıs 2027'ye kadar girilmiş, ama 20 Eylül'den sonraki maçların sonucu yok. Sayfanın üstünde "Veri: 20.09.2026'ya kadar" yazar. Bugünkü bir maçın tahmini iki aylık eski formla yapılır.
- **Sakatlık ve ceza bilgisi yok.**
- **Takım renkleri ve logoları yok.** Bilinen 40 kulübe elle renk verildi, diğerleri otomatik renk alır.
- **Bazı kayıtlar eksik ya da bozuk** (boş takım, eksik alan). Bunlar atlanır, analiz durmaz.

### Sızıntı yok

Bir maç her zaman **kendi tarihinden önceki** veriyle analiz edilir. Geçmiş bir maçı seçtiğinizde model, o maçın sonucunu ve sonrasını bilmez; o günün bilgisiyle konuşur. Bu sayede oynanmış maçlar modeli sınamak için kullanılabilir.

---

## 4. Model: adım adım

Model iki parçadır. **Güç modeli** iki takımın bu maçta kaç gol atmasını beklediğimizi hesaplar. **Simülasyon** o beklentiyle maçı 10.000 kez oynatır.

### 4.1 Güç: her takıma iki sayı

Her takımın iki değeri vardır:

- **Hücum:** ortalama bir takıma göre ne kadar gol atıyor. 1 ortalama, 1,5 yüzde 50 fazlası.
- **Savunma:** ortalama bir takıma göre ne kadar gol yiyor. Düşük olan iyidir.

Bir maçtaki beklenen gol bu değerlerin çarpımıdır:

```
Ev sahibinin beklenen golü = lig ortalaması × iç saha çarpanı × ev sahibinin hücumu × deplasmanın savunması
Deplasmanın beklenen golü  = lig ortalaması ×                   deplasmanın hücumu × ev sahibinin savunması
```

İç saha çarpanı veriden hesaplanır ve yaklaşık 1,34 çıkar: ev sahibi ortalamada yüzde 34 fazla gol atar. Şu an bütün takımlar için aynıdır.

### 4.2 Bu değerler nasıl bulunuyor

Maçtan önceki dört yılın bütün kulüp maçları alınır. Model şu soruyu çözer: *hangi hücum ve savunma değerleri bu maçlardaki golleri en iyi açıklar?*

Çözüm tekrarlı hesapla bulunur:

1. Herkes 1 kabul edilir.
2. Her takımın hücumu, attığı gollerin rakiplerinin savunmasına göre beklenen gole oranından yeniden hesaplanır.
3. Aynısı savunma için, yeni hücum değerleriyle yapılır.
4. Lig ortalaması ve iç saha çarpanı güncellenir.
5. 40 tur tekrarlanır; değerler oturur.

Hücum ve savunmanın **sırayla** güncellenmesi önemlidir. İkisi aynı anda güncellendiğinde hesap salınıma girip dağılıyordu (bkz. [bölüm 9](#9-yolda-öğrendiklerimiz)).

### 4.3 Beş ayar

Hepsi `ModelOptions.cs` içindedir ve geçmiş maçlarla sınanarak seçilmiştir.

| Ayar | Değer | Ne yapar |
|---|---|---|
| **Yarı ömür** | 500 gün | Yeni maç eskisinden ağır sayılır. 500 gün önceki maç yarı ağırlıkta, 1.000 gün önceki çeyrek ağırlıktadır. |
| **Önsel maç** | 8 | Az maçı olan takım hakkında emin olamayız. Her takıma, grubunun ortalaması gibi oynadığı 8 "sözde maç" eklenir; az verisi olan takım ortalamaya yakın kalır, çok verisi olan bundan az etkilenir. |
| **Grup çekimi** | 0,85 | Az maçlı takım genel ortalamaya değil, **kendi liginin ya da ülkesinin** seviyesine çekilir. Böylece zayıf bir ligin az maçlı takımı "ortalama bir Avrupa takımı" sayılmaz. |
| **Avrupa ağırlığı** | 2 | Farklı liglerin takımlarını kıyaslamanın tek yolu Avrupa kupalarında oynadıkları maçlardır. Bu maçlar az olduğu için iki kat sayılır. |
| **Şut payı** | 0,3 | Güç yalnızca gollerden değil, şutlardan da hesaplanır (aşağıda). |

### 4.4 Şutların güce katılması

Gol az ve şansa açık bir veridir: 20 şut atıp 0-1 kaybeden takım, yalnızca gole bakan bir model için kötü oynamıştır.

Şut istatistiği olan maçlarda model her takım için bir **şutlardan beklenen gol** hesaplar:

```
şutlardan beklenen gol = a × isabetli şut + b × isabetsiz şut
```

`a` ve `b`, eldeki maçlarda gerçek gollere en iyi uyan değerler olarak veriden bulunur. Güç hesabına giren değer bunun gerçek golle karışımıdır:

```
etkili gol = 0,7 × gerçek gol + 0,3 × şutlardan beklenen gol
```

Şut verisi olmayan maçlarda yalnızca gerçek gol kullanılır.

### 4.5 Ekrandaki güç puanı

Ekranda görünen puan (örneğin Galatasaray 1.818, Aston Villa 1.927) hücum ve savunmanın tek sayıya çevrilmiş halidir:

```
güç puanı = 1700 + 560 × ( ln(hücum ÷ savunma) ÷ 2 )
```

Yalnızca gösterim içindir. Tahminde hücum ve savunma ayrı ayrı kullanılır; iki takım aynı puana sahip olup biri hücumcu, diğeri savunmacı olabilir.

### 4.6 Maç istatistiği beklentileri

Şut, korner, kart ve top hakimiyeti beklentisi takımların son 18 aydaki ortalamalarından gelir, üç düzeltmeyle:

- **Rakibe göre:** takımın attığı şut ortalaması ile rakibin verdiği şut ortalaması birlikte değerlendirilir.
- **Gol beklentisine göre:** beklenen şut, takımın şutlarını gole çevirme oranı ile beklenen golden de hesaplanır; iki tahminin ortası alınır.
- **Lig seviyesine göre:** top hakimiyeti, takımın karşılaştığı rakiplerin gücüne göre düzeltilir. Yoksa zayıf ligde yüzde 60 topa sahip olan takım, güçlü bir rakibe karşı da öyle görünür.

### 4.7 Simülasyon: maçı dakika dakika oynatmak

Maç 97 dilime bölünür: 90 dakika, ilk yarıda 2 ve ikinci yarıda 5 dakika uzatma. Her dilimde her takım için sırayla şu sorular sorulur ve her biri için rastgele bir sayı çekilir:

1. **Şut oldu mu?** Olasılık, beklenen şut sayısının o dilime düşen payıdır.
2. **Şut gol mü?** Olasılık = beklenen gol ÷ beklenen şut. Bu oran, toplam gol beklentisinin tam tutmasını sağlar.
3. **Gol değilse isabetli mi?** Yüzde 30 olasılıkla.
4. **Korner oldu mu? Sarı kart? Kırmızı kart?** Her biri için ayrı.

Üç şey maçı düz bir zar oyunundan ayırır:

- **Tempo sona doğru artar.** Son dakikalarda olay olasılığı başa göre yaklaşık yüzde 35 yüksektir.
- **Skor oyunu değiştirir.** Geride olan takım biraz daha bastırır (yüzde 3, 60. dakikadan sonra yüzde 6), öndeki yüzde 2 çekilir. Bu değerler bilerek küçüktür (bkz. [bölüm 9](#9-yolda-öğrendiklerimiz)).
- **Kırmızı kart** gören takımın üretimi yüzde 25 düşer, rakibinki yüzde 15 artar.

Her simülasyon bir **tohum** sayısıyla başlar. Aynı tohum her zaman aynı maçı verir; bu sayede aynı maçın analizi her açılışta aynı çıkar.

---

## 5. Kadro ve oyuncular

### Kadro nereden geliyor

- **Açıklandıysa:** maç kadrosu (ilk 11, yedekler, diziliş, teknik direktör) doğrudan kullanılır. Ekranda "Açıklanan 11" yazar.
- **Açıklanmadıysa:** takımın son 8 maçında en çok ilk 11 başlayan oyunculardan bir **muhtemel 11** çıkarılır; yakın maçlar daha ağır sayılır. Yedekler, aynı maçlarda kadroda görünen diğer oyunculardır. Ekranda "Muhtemel 11" yazar.

Kadro maçtan yaklaşık bir saat önce açıklanır. O saatte "Yeniden hesapla"ya basıldığında analiz gerçek 11'e göre güncellenir.

### Olaylar kime yazılıyor

Her oyuncunun bir **ağırlığı** vardır: son 18 aydaki golleri artı mevkiine göre bir taban (forvet 3, orta saha 1,5, defans 0,4, kaleci 0). Bir şut ya da gol olduğunda, **o an sahada olan** oyuncular arasından ağırlıkla orantılı olarak biri seçilir. Kartlar sahadaki oyunculara eşit olasılıkla dağıtılır.

Üç kural her zaman geçerlidir:

- Maç kadrosunda olmayan oyuncu hiçbir olaya giremez.
- Yedek oyuncu ancak oyuna girdikten sonra olaya girer.
- Çıkan ya da kırmızı kart gören oyuncu bir daha olaya girmez.

### Oyuncu değişiklikleri

Her simülasyonda değişiklikler de oynanır:

- Her takım 3–5 değişiklik yapar (çoğunlukla 4 ya da 5), en fazla üç ayrı anda.
- Hepsi ikinci yarıdadır; çoğu 58–86. dakikalar arasında, bazen devre arasında.
- Giren oyuncu, mümkünse aynı mevkideki bir oyuncunun yerine girer.
- Kaleci değiştirilmez; sonradan giren oyuncu tekrar çıkarılmaz.

Değişiklik kararları ayrı bir rastgele dizi kullanır. Bu yüzden kadronun verilip verilmemesi maçın gidişatını (şutlar, goller, skor) değiştirmez; yalnızca olayların kime yazıldığını değiştirir.

### Önemli sınır

**Kadro takımın gücünü değiştirmez.** Yıldız oyuncu kadroda olmasa bile takımın beklenen golü aynı kalır; değişen yalnızca golü kimin atacağıdır. Bu, modelin en büyük eksiğidir (bkz. [bölüm 13](#13-sıradaki-adımlar)).

---

## 6. Çıktılar nasıl üretiliyor

Aynı maç 10.000 kez oynatılır ve sonuçlar sayılır.

| Çıktı | Nasıl hesaplanıyor |
|---|---|
| Sonuç yüzdeleri | 10.000 maçın kaçını ev sahibi kazandı, kaçı berabere bitti, kaçını deplasman kazandı |
| Skor dağılımı | Her skor kaç kez çıktı |
| Beklenen istatistikler | Şut, korner, kart sayılarının 10.000 maçtaki ortalaması |
| Gol dakikaları | Gollerin 15'er dakikalık dilimlere dağılımı |
| İlk golü atan taraf | Kaç maçta ilk golü hangi takım attı, kaçı golsüz bitti |
| Oyuncunun gol yüzdesi | O oyuncu kaç maçta en az bir gol attı |

### Neden "sonuca göre skorlar"

Tek tek skorlar sıralandığında dengeli maçların yaklaşık yüzde 70'inde 1-1 birinci çıkar. Bu bir hata değildir: iki takımın da beklenen golü 1 ile 2 arasındaysa en olası tek skor matematiksel olarak 1-1'dir, gerçek futbolda da en sık çıkan skor odur (yüzde 11). Ama ekranda her maçta "1-1" görmek bir şey söylemez.

Bu yüzden skorlar sonuca göre ayrılır: ev sahibi kazanırsa en olası üç skor, berabere biterse üç skor, deplasman kazanırsa üç skor.

### Temsili maç

Sahada oynatılan maç, 10.000 simülasyondan biridir: **en olası sonucun en olası skoruyla** biten ve şut sayıları ortalamaya en yakın olan, mümkünse kırmızı kartsız maç. Böylece oynatılan maç üstteki yüzdelerle çelişmez.

Bunun bir yan etkisi vardır: temsili maç neredeyse hiç berabere bitmez, çünkü beraberlik nadiren tek başına en olası sonuçtur.

### Güç karşılaştırması

Beş eksende 0–100 arası değer; her biri takımın bütün takımlar içindeki yüzdelik sırasıdır: hücum, savunma, form (son 5 maçın puanı), saha (ev sahibinin iç saha, deplasmanın dış saha puan ortalaması), top hakimiyeti.

### Yorum cümleleri

"Maçın anahtarı" bölümündeki cümleler sabit şablonlardır ve hesaplanan sayılarla doldurulur. Favori ifadesi iki takımın galibiyet yüzdeleri arasındaki farka göre seçilir: 8 puandan az ise "çok yakın", 25 puandan az ise "bir adım önde", fazlası "net favori".

---

## 7. Sahadaki 2D oynatma

Temsili maçın olayları (dakika, tür, oyuncu) sunucudan gelir. Sahadaki hareketin tamamı tarayıcıda, `pitch.js` içinde üretilir. Hesaba etkisi yoktur; yalnızca görseldir.

### Temel ilke

**Oyuncular yavaş, top hızlıdır.** Oyuncular dizilişlerinde durur ve yavaşça kayar; top oyuncudan oyuncuya pasla gider. Kimse topun peşinden koşmaz. Koşan tek kişi, şut noktasına giden oyuncudur.

### Diziliş

- Oyuncular kadrodaki dizilişe göre yerleşir (4-2-3-1 ise öyle), forma numaraları üzerlerindedir.
- **Savunma hattı oyunun oynandığı bölgeye göre ilerler.** Hücum eden takımın defansı orta sahaya kadar çıkar, orta sahası ceza sahasına yaklaşır; savunan takım kalesinin önünde toplanır.
- **Hatlar düz çizgi değildir.** Her oyuncunun maça özgü küçük bir sapması vardır; kanat oyuncuları hücumda öne çıkar, topun olduğu taraftaki oyuncular oyuna yaklaşır.
- Oyuncunun olağan hızı saniyede saha boyunun yüzde 11'i ile sınırlıdır. Şuta koşan oyuncu bunun 2,2 katına kadar çıkabilir.

### Zaman

Zaman ekseni esnektir: olaysız bir dakika ekranda 0,56 saniye sürer; olaylı dakika, olayın sahada oynanması kadar uzar. Saat her zaman olayın dakikasını gösterir. Bir maç 1x hızda yaklaşık 105–130 saniye sürer; 2x ve 4x hız seçilebilir.

### Olaylar sahada nasıl oynanır

| Olay | Sahada |
|---|---|
| **Açık oyun** | Top, baskının olduğu bölgedeki oyuncular arasında pasla dolaşır |
| **Hücum hazırlığı** | Birkaç dakika içinde şut olacaksa o takım topu alır ve öne yerleşir |
| **Şut** | Oyuncu şut noktasına gelir, pas ona ulaşır, vurur. İsabetsiz şut ceza sahası çevresinden, isabetli şut ve gol daha yakından atılır |
| **Gol** | Top ağlarda bekler, "GOL!" bandı çıkar, sonra santra yapılır |
| **Geriden gelen oyuncunun golü** | Duran top olarak oynanır: top serbest vuruş noktasında bekler, takım ceza sahasına yerleşir, orta gelir |
| **Korner** | Top köşe bayrağında belirir, bekler, ortalanır |
| **Oyuncu değişikliği** | Çıkan oyuncunun dairesi giren oyuncuya geçer; numara ve isim değişir, yeşil halkayla işaretlenir |
| **Kırmızı kart** | Oyuncu sahadan kalkar |

Top kimdeyse etrafında halka ve üstünde adı görünür. Sahanın altında o an ne olduğunu söyleyen bir etiket çıkar. Aynı maç her oynatılışta aynı görünür.

### Görselin sınırları

- Paslar taktik bir anlam taşımaz; top, bölgedeki oyunculara rastgele dağılır.
- Rakip topa basmaz, araya girmez.
- Ev sahibi iki yarıda da sağa hücum eder.
- Çıkan oyuncu sahadan yürüyerek çıkmaz; dairenin kimliği yerinde değişir.

---

## 8. Model ne kadar isabetli

### Nasıl ölçüyoruz

**Geçmişe dönük sınama:** Ağustos 2024'ten itibaren her 30 günde bir, yalnızca o güne kadarki maçlarla model kurulur ve sonraki 30 günün maçları tahmin edilir. Tahminler gerçek sonuçlarla karşılaştırılır. Model hiçbir zaman geleceği görmez.

### Sonuçlar (7.669 maç, Ağustos 2024 – Eylül 2026)

| Ölçü | Değer | Anlamı |
|---|---|---|
| Sonuç isabeti | %52,5 | En olası dediği sonucun tutma oranı. İyi halka açık modeller %53–54 civarındadır |
| Log-kayıp | 0,9908 | Düşük olan iyi. Hiçbir şey bilmeyen tahmin 1,0643 |
| RPS | 0,2053 | Düşük olan iyi |
| Maç başına gol | tahmin 2,78 / gerçek 2,85 | Hafifçe az tahmin ediyor |
| Beraberlik | tahmin %23,9 / gerçek %23,8 | Tutuyor |

### Verdiği olasılıklar tutuyor mu

En önemli özellik budur: model "yüzde 60" dediğinde gerçekten yüzde 60 mı oluyor?

| Favoriye verdiği olasılık | Gerçekleşen |
|---|---|
| %39 | %40 |
| %48 | %49 |
| %59 | %62 |
| %71 | %74 |
| %83 | %84 |

### Turnuvaya göre

En iyi tahmin edilen Şampiyonlar Ligi (%58 isabet), en zoru Premier Lig (%48,7). Süper Lig %54,1.

### Bilinen yanlılık

Model, ülke dışı rakiplere karşı İngiliz takımlarını olduğundan zayıf görüyor (maç başına beklenen 1,79 puan, gerçek 2,08), Türk takımlarını biraz güçlü görüyor (beklenen 1,58, gerçek 1,44).

### Formül ve simülasyon aynı şeyi söylüyor mu

Sınama güç modelini formülle ölçer; ekrandaki yüzdeler ise simülasyondan gelir. İkisi aynı 314 maçta karşılaştırıldı: log-kayıpları aynı (0,9960 ve 0,9959), ortalama fark 1 yüzde puanın altında.

### Tek maç isabeti ölçmez

Yüzde 40 verilen bir sonucun çıkmaması modelin yanlış olduğunu göstermez. Modeli sınamak için tek maça değil yüzlerce maça bakılır. "En olası skor" özellikle zayıf bir tahmindir: birinci sıradaki skor maçların yalnızca yaklaşık yüzde 11'inde tutar, model de o skora zaten o kadar olasılık verir.

---

## 9. Yolda öğrendiklerimiz

Bu bölüm, modelin neden bugünkü haliyle kurulduğunu anlatır. Her madde gerçek bir hatadan çıktı.

**Gerçek veride model dağıldı.** İlk sürümde beklenen gol 0,01 çıkıyordu. Neden: hücum ve savunma aynı anda güncelleniyordu; az maçlı yüzlerce takım varken hesap iki uç arasında salınıyordu. Çözüm: önce hücum, sonra yeni hücumla savunma güncelleniyor. Bunu denetleyen bir test var.

**Türk takımları fazla güçlü görünüyordu.** İlk sıralamada Fenerbahçe Avrupa'nın 7'ncisiydi. Sınama doğruladı: beklenen 1,77 puan, gerçek 1,44. Neden: az maçlı zayıf takımlar "ortalama" sayılıyordu, onları farklı yenen takımlar olduğundan güçlü görünüyordu. Çözüm: az maçlı takım kendi liginin seviyesine çekiliyor ve Avrupa maçları iki kat sayılıyor.

**Simülasyon beraberliği 3 puan fazla üretiyordu.** "Geride olan takım bastırır" etkisi ilk halinde fazla güçlüydü (yüzde 10–20). Formül yüzde 24,3 beraberlik verirken simülasyon yüzde 27,5 veriyordu; gerçek yüzde 24,8'di. Etki küçültüldü, simülasyon şimdi yüzde 25,3 veriyor. Ders: formül ile simülasyon ayrı ayrı sınanmalı.

**Şutlar beklenenden az katkı yaptı.** Şutları güce katmanın en büyük kazancı getireceği düşünülmüştü; log-kayıpta yalnızca 0,001 iyileşme sağladı. Neden: şut verisi yalnızca Eylül 2024'ten beri var. Veri biriktikçe etkisinin artması beklenir.

**Ayar aramasının sonucu her zaman alınmadı.** Arama Avrupa ağırlığını 1,5'e çekmek istedi. Log-kayıp farkı yok denecek kadar azdı ama Türk takımlarının yanlılığını büyütüyordu; 2'de bırakıldı.

**Kadroda olmayan oyuncu şut atıyordu.** Kadro açıklanmamışken takımın geniş kadrosundaki herkes olay havuzunda kalıyordu. Artık yalnızca maç kadrosu ve yalnızca sahada olanlar olaya girebiliyor.

**Bozuk kayıtlar analizi durduruyordu.** Bazı kadro kayıtlarında takımlardan biri boş geliyor; 316 maçın 74'ünde analiz hata veriyordu. Artık bu kayıtlar atlanıyor.

**Sahadaki hareket üç kez yeniden yazıldı.** Önce oyuncular topun peşinden koşuyordu, sonra şutlar orta sahadan atılıyordu, sonra hatlar cetvelle çizilmiş gibi duruyordu. Bugünkü kural: oyuncular yavaş, top hızlı, şut her zaman kaleye makul mesafeden.

---

## 10. Modelin bilmediği şeyler

- **Kim oynuyor, güce yansımıyor.** Güç takıma aittir, oyunculara değil.
- **Sakatlık, ceza, yorgunluk, maçın önemi** hesapta yok.
- **Şut kalitesi yok.** Penaltı ile 30 metreden şut aynı değerdedir; şutun nereden atıldığı veride bulunmuyor.
- **İç saha avantajı herkes için aynı.**
- **Dinlenme süresi** hesapta yok: üç gün önce maç oynamış takım ile bir hafta dinlenmiş takım aynı sayılır.
- **Teknik direktör değişikliği, transfer** gibi kırılmaları ancak sonuçlara yansıdıkça, gecikmeli görür.
- **Değişiklikler taktiğe bakmaz.** Geride olan takım forvet almaz; kimin gireceği rastgeledir.
- **Muhtemel 11 tahmindir.** Sakat ya da cezalı bir oyuncu listede çıkabilir.
- **Milli maçlar** kapsam dışıdır.

---

## 11. Kod yapısı

```
SPOR_ANALIZ.sln
src/
  SporAnaliz.Core      Model ve simülasyon. Veritabanı ya da web bilmez.
    Domain/            Takım, maç, kadro gibi veri sınıfları
    Abstractions/      IMatchData: motorun veri kaynağından beklediği sözleşme
    Model/             StrengthModel (güç), ModelOptions (ayarlar), PoissonMath (formülle olasılık)
    Simulation/        MatchSimulator: maçı dakika dakika oynatır
    Analysis/          Analyzer (hesap), AnalysisService (veriyi toplar, sonucu saklar)
    Evaluation/        Backtester: geçmişe dönük sınama
    Playback/          Temsili maçın saniye saniye oynatılması (pitch.js'in C# karşılığı); çizilen gol atağının zamanlaması (AtakPlani)
  SporAnaliz.Data      Veri kaynakları. Core'a bağlıdır.
    Feed/              Yerel maç veritabanı; YALNIZCA okunur
    TestData/          Üretilmiş varsayımsal veri (yalnızca geliştirme ve testler için)
    Atak/              Çizilen gol atakları (yerel htspor_tahmin veritabanı, gol_atak tablosu)
  SporAnaliz.Web       HTTP uçları ve sayfa. Core ve Data'ya bağlıdır.
    Endpoints/         /api/fixtures, /api/analysis, /api/rounds, /api/teams ...
    Setup/             Veri kaynağının kaydı ve açılış
    wwwroot/           index.html, app.css, app.js (sayfa), pitch.js (2D saha)
  SporAnaliz.Kumanda   Viz kumandası (WinForms). Core ve Data'ya bağlıdır.
    Viz/               Viz Engine bağlantısı ve sahneye gönderilen veriler
viz/
  MacSim.vbs           mac_sim sahnesinin betiği (sahneyi kurar, gelen veriyi çizer)
tools/
  SporAnaliz.Tools     Geliştirme araçları
tests/
  SporAnaliz.Tests     Motorun, oynatmanın ve atak planının birim testleri (35 test)
```

Bağımlılık tek yönlüdür: **Web → Data → Core**. Core hiçbir şeye bağlı değildir; model üzerinde çalışırken veritabanına ya da sayfaya dokunmak gerekmez.

### Bir analiz isteğinin yolu

1. Sayfa `/api/analysis/{maçNo}` ucunu çağırır.
2. `AnalysisService` maçın girdilerini veri kaynağından toplar: geçmiş maçlar, takım istatistikleri, oyuncular, kadrolar.
3. `Analyzer` güç modelini kurar (`StrengthModel`), beklenen golleri hesaplar, `MatchSimulator` ile 10.000 maç oynatır, sonuçları sayar, temsili maçı seçer.
4. Sonuç JSON olarak sayfaya döner ve bellekte saklanır.
5. Sayfa (`app.js`) sonuçları çizer; `pitch.js` temsili maçı sahada oynatır.

### Ayarlar

`appsettings.json`:

- `ConnectionStrings:MySql`: MySQL bağlantısı.
- `Data:Source`: `feed` (varsayılan; yerel maç veritabanı) ya da `test` (üretilmiş veri).
- `Data:Database`: veritabanının adı.
- `Model`: isteğe bağlı. Yazılırsa `ModelOptions` içindeki varsayılanları ezer, örneğin `"Model": { "ShotWeight": 0.5 }`.

Yeni bir veri kaynağı eklemek için `IMatchData` arayüzünü uygulayan bir sınıf yazıp `Setup/DataSetup.cs` içinde kaydetmek yeterlidir.

---

## 12. Araçlar

Çözüm klasöründen çalıştırılır:

```
dotnet test                                                        # birim testleri
dotnet run --project tools/SporAnaliz.Tools -- backtest            # geçmiş sınaması (varsayılan ayarlarla)
dotnet run --project tools/SporAnaliz.Tools -- backtest shot=0.5   # bir ayarı değiştirerek (half, prior, pull, euro, shot)
dotnet run --project tools/SporAnaliz.Tools -- tune                # ayarları veriyle seç, ayrı dönemde doğrula
dotnet run --project tools/SporAnaliz.Tools -- simcheck            # formül ile simülasyonu aynı maçlarda karşılaştır
dotnet run --project tools/SporAnaliz.Tools -- explore             # yerel veritabanlarını listele
dotnet run --project tools/SporAnaliz.Tools -- selftest            # veritabanısız motor denemesi
```

### Kural

**Modelde yapılan her değişiklik sınamadan geçmelidir.** "Mantıklı görünüyor" diye modele bir şey girmez.

- Güç modelini değiştirdiyseniz `backtest` çalıştırın.
- Simülasyonu değiştirdiyseniz `simcheck` çalıştırın.
- Bir ayarı değiştirmeden önce `tune` ile bakın. `tune` ayarları Ağustos 2024 – Kasım 2025 döneminde seçer ve hiç bakmadığı Aralık 2025 – Eylül 2026 döneminde doğrular; böylece aynı maçlara bakıp ezberleme önlenir.

Bakılacak sayılar: isabet, log-kayıp ve RPS, tahmin edilen ve gerçek gol ile beraberlik, ülke dışı rakiplere karşı beklenen ve gerçek puan.

---

## 13. Viz kumandası

Analizin temsili maçını Viz Engine'deki `/000_WORK/mac_sim` sahnesinde kuşbakışı, tam ekran oynatan ayrı bir Windows programı: `src/SporAnaliz.Kumanda`.

### Kurulum

1. `src/SporAnaliz.Kumanda/appsettings.example.json` dosyasını aynı klasöre `appsettings.json` adıyla kopyalayıp doldurun (veritabanı ve Viz adresi). Bu dosya depoya girmez.
2. Viz Artist'te `mac_sim` sahnesi açık olmalı. Sahne betiği `viz/MacSim.vbs`'tir; sahne bir kez "Sahneyi kur" düğmesiyle kurulur ve kaydedilir.
3. Programı Visual Studio'da başlangıç projesi seçip çalıştırın ya da `dotnet run --project src/SporAnaliz.Kumanda`.

### Kullanım

Her adımın kendi düğmesi ve kısayolu vardır:

| Düğme | Kısayol | Ne yapar |
|---|---|---|
| BAĞLAN | | Viz Engine'e bağlanır (varsayılan 127.0.0.1:6100). Üst şeritte sahnenin hazır olup olmadığı görünür. |
| ANALİZ ET | F2 | Soldan seçilen maçı analiz eder (10.000 simülasyon), oynatılacak maçı hazırlar. Maça çift tıklamak da aynıdır. |
| VİZ'E YÜKLE | F3 | Kadro, renkler, tabela ve başlama dizilişini sahneye gönderir. |
| OYNAT | F5 | Maçı oynatır; ortadaki önizleme Viz'de görünenin aynısıdır. |
| DURAKLAT | F6 | Durdurur; OYNAT kaldığı yerden sürdürür. |
| BAŞTAN | F7 | Maçı başa alır ve sahneyi başlama dizilişine döndürür. |
| HIZ 1x / 2x / 4x | | Oynatma hızı. 1x'te maç yaklaşık 1 dakika sürer. |
| TEMİZLE | F8 | Ekrandaki bandı ve etiketi kapatır. |

Maçlar turnuva → aşama → hafta ile ya da "Tüm turnuvalar" seçilip tarih aralığıyla listelenir; üstteki kutu takım adına göre süzer. Viz bağlı değilken de OYNAT çalışır, yalnızca önizleme oynar.

### Sahneyle konuşma

Program sahneye yalnızca Viz'in paylaşımlı belleğine (`VIZ_COMMUNICATION*MAP`) yazar; sahnedeki betik değişiklikleri görür ve çizer. Sahne ağacına komut gönderilmez, bu yüzden Artist düzenleme modundayken de çalışır.

| Anahtar | Biçim | Anlamı |
|---|---|---|
| `sim_poz` | `x,y[,0];…` 22 oyuncu + top | Konumlar 0–1 arası (x sağa, y aşağı). `,0` gizle (kırmızı kart, top oyun dışı). Saniyede ~10 kez gelir; sahne aradaki kareleri doldurur. |
| `sim_kadro` | `no\|ad;…` 22 oyuncu | Forma numaraları ve isimler (değişiklikte yenilenir). |
| `sim_renk` | `r,g,b;r,g,b` | Ev ve deplasman renkleri. |
| `sim_metin` | `EV_AD=…\|DEP_AD=…\|SKOR=…\|DAKIKA=…` | Tabela. |
| `sim_sahip` | oyuncu sırası ya da `-1` | Topu taşıyan oyuncunun isim etiketi. |
| `sim_bant` | `başlık\|alt\|r,g,b\|saniye` | Alt bant (GOL, KIRMIZI KART, DEVRE ARASI, MAÇ SONU). Boş değer kapatır, 0 saniye kalıcıdır. |
| `sim_etiket` | `metin\|r,g,b\|saniye` | Küçük olay etiketi (şut, korner, kart, değişiklik). |
| `sim_durum` | sahne yazar | Sahnenin her saniye yazdığı durum; program bağlantıyı bununla denetler. |

Viz yalnızca değer değişince sahneye haber verir; program aynı bandı tekrar göstermek için önce boş değer yazar.

### Canlı maç: gol atağı

Kumandanın ikinci sekmesi (**CANLI MAÇ · GOL ATAĞI**). Gerçek bir maçta gol olunca operatör atağı sahaya çizer, Viz golün nasıl geldiğini canlandırır: dokunuş noktaları, pas (düz çizgi), top sürme (kesikli çizgi), şut, formalar ve altta atakta yer alan oyuncular ile pas sayısı. Atak her zaman sağdaki kaleye akar.

1. Soldan günü ve maçı seçin. Kadro ve takım renkleri gelir.
2. Golü atan takımı seçin, dakikayı ve skoru girin.
3. Atağın başladığı yere tıklayın, sağdan oyuncuyu seçin (ya da numarasını yazın).
4. Pas: yeni yere tıklayıp oyuncuyu seçin. Top sürme: yalnızca tıklayın (aynı oyuncu devam eder).
5. Son nokta golü atan oyuncudur; kaleye şut kendiliğinden çizilir.
6. **ÖNİZLE (F4)** ile kumandada, **VİZ'E GÖNDER (F5)** ile yayında oynatın. **VİZ'DEN KALDIR (F8)** kapatır.

Sağ tık noktayı siler, noktalar sürüklenerek taşınır, Ctrl+Z son noktayı geri alır.

Gönderilen her atak yerel MySQL'deki kendi veritabanımıza (`htspor_tahmin`, tablo `gol_atak`) kaydedilir; maç seçilince o maçın kayıtlı golleri listelenir ve tek tıkla yeniden açılıp gönderilebilir. Kayıt yalnızca bu bilgisayardaki sunucuya yapılır: bağlantı dizesi başka bir sunucuyu gösterirse ya da maç veritabanını gösterirse kayıt kapanır.

Sahne tarafında bunun için `viz/MacSim.vbs` betiğinin güncel hâli gerekir: betiği SIM konteynerine yeniden yapıştırıp derleyin, **Sahneyi kur** düğmesine basın ve sahneyi kaydedin.

| Anahtar | Biçim | Anlamı |
|---|---|---|
| `sim_atak` | `r,g,b\|başlık\|oyuncular\|öğe;öğe;…` | Çizilen atak. Öğeler: `C` çizgi parçası, `N` nokta, `F` forma, `P` alt panel (ayrıntı betiğin başında). Boş değer kapatır. |

---

## 14. Sıradaki adımlar

Öncelik sırasıyla:

1. **Güncel veri.** En büyük eksik model değil, verinin 20 Eylül'de durmasıdır. Canlı beslenen veritabanına bağlanmak, aşağıdaki adımların hepsinden daha çok fark yaratır.
2. **Kadroya duyarlı güç.** Bugünkü 11'in, takımın gücünü üreten 11'lerden ne kadar farklı olduğunu ölçmek. Asıl değeri yayında: "kadro açıklandı, tahmin şöyle değişti" diyebilmek. Bunun için sakatlık ve ceza bilgisi gerekir; veride yoktur. İki yol var: editörün panelden elle girmesi ya da ek bir veri servisi (sakatlık ve tahmini ilk 11 satan sağlayıcılar mevcut).
3. **Lig seviyesi düzeltmesi.** İngiliz takımlarının Avrupa'da zayıf görünmesini hedefler.
4. **Takıma özel iç saha ve dinlenme süresi.** İkisi de veride hazır ve küçük işlerdir.
5. **Yayın ekranı.** Viz'de kuşbakışı oynatma ve kumandası hazır (bölüm 13); tasarımın yayın grafikleriyle uyumlu hâle getirilmesi kaldı.
6. **Yapay zeka yorumu.** Şablon cümleler yerine üretilmiş yorum metni; isteğe bağlı, ücretli.

### Yayına çıkmadan önce

- Veri sağlayıcısıyla yapılan anlaşmanın, veriyi yayında grafik olarak kullanmayı kapsadığı teyit edilmeli.
- Program şu an veritabanına yönetici kullanıcısıyla bağlanıyor; yalnızca okuma yetkisi olan ayrı bir kullanıcı açılmalı.
- Kurumsal renkler, yazı tipi ve takım logoları eklenmeli; şu anki renkler geçicidir.
- Modelin saçma bir sonuç vermesi ihtimaline karşı yayın öncesi bir onay adımı tanımlanmalı.
- Proje sürüm kontrolüne (git) alınmalı.
