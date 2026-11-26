' ============================================================================
'  SPOR ANALİZ - mac_sim sahnesi (Viz 3.14)
'
'  Bu betik iki iş yapar:
'   1) "Sahneyi kur" düğmesi: saha, çizgiler, 22 oyuncu, top, skor tabelası ve
'      bantları kurar. Şekiller ilk karelerde ölçülüp istenen boyuta getirilir.
'   2) Yayında: kumanda programının VIZ_COMMUNICATION*MAP'e yazdığı verilerle
'      sahneyi oynatır. Oyuncular gelen hedeflere iki mesaj arasında akarak gider.
'
'  Kurulum:
'   - Sahnenin köküne istediğiniz yazı tipiyle "YAZI" adlı bir metin konteyneri koyun.
'     Betik bütün yazıları bundan kopyalar; kendisi gizlenir.
'   - Yine köke "SIM" adlı boş bir konteyner koyun, bu betiği onun üzerine Script
'     eklentisi olarak ekleyin, derleyin ve "Sahneyi kur" düğmesine basın.
'
'  Koordinatlar:
'   - SAHA grubunun içi metre cinsindendir: x -52.5..52.5 (uzunluk), y -34..34 (en).
'   - EKRAN grubunun içi piksel cinsindendir: merkez (0,0), sağ +960, yukarı +540.
'
'  Kumandanın kullandığı anahtarlar (hepsi VIZ_COMMUNICATION*MAP SET_STRING_ELEMENT):
'   sim_poz    "x,y[,g];x,y[,g];... (22 oyuncu) ;x,y[,g] (top)"
'              x,y 0..1 arası: x=0 sol kale çizgisi, x=1 sağ; y=0 üst kenar, y=1 alt kenar.
'              g (isteğe bağlı) 0 ise nesne gizlenir (kırmızı kart, top oyun dışı).
'   sim_kadro  "no|kısa ad;no|kısa ad;..." 22 kayıt: önce ev sahibi (0 kaleci), sonra deplasman
'   sim_renk   "r,g,b;r,g,b"  ev sahibi ve deplasman forma rengi (0-255)
'   sim_metin  "EV_AD=..|DEP_AD=..|SKOR=1 - 0|DAKIKA=23'"  (istenen alanlar)
'   sim_sahip  topu taşıyan oyuncunun sırası (0-21), -1 = kimse
'   sim_bant   "başlık|alt yazı|r,g,b|saniye"   boş metin bandı kapatır; saniye 0 = kalıcı
'   sim_etiket "metin|r,g,b|saniye"             boş metin etiketi kapatır
'   sim_atak   operatörün çizdiği gol atağı; boş metin kapatır. Biçim:
'              "r,g,b|başlık|oyuncular|öğe;öğe;..."  (r,g,b = atak yapan takımın rengi)
'              öğeler (metre ve saniye; süre atağın başından sayılır):
'                C,t0,süre,x,y,dx,dy,uzunluk,açı,kalınlık   çizgi parçası: (x,y) noktasından (dx,dy) yönünde uzar
'                N,t0,x,y                                   dokunuş noktası
'                F,t0,x,y,numara[,ad]                       forma (ad formanın üstünde yazar)
'                P,t0                                       alt panel
' ============================================================================

dim kurDurum as Integer          ' 0 hazır, 1 şekiller ölçülüyor, 2-3 ekran ölçülüyor, 4 bitti
dim bekle as Integer
dim sablon as Container

dim fitC as Array[Container]     ' ölçülecek konteynerler
dim fitK as Array[Integer]       ' 0 dikdörtgen (en x boy), 1 daire (çap), 2 yazı (yükseklik)
dim fitA, fitB as Array[Double]
dim fitT as Array[String]        ' yazı ölçüldükten sonra konacak metin
dim fitOk as Array[Integer]

dim saha, ekran, cim, isim, isimYazi, bant, etiket as Container
dim oy, oyGovde, oyHalka, oyNo as Array[Container]
dim oyAd as Array[String]
dim bagli as Boolean

' Ara değerleme (sim_poz): her nesne için başlangıç ve hedef, metre cinsinden
dim fx, fy, tx, ty as Array[Double]
dim t0 as Double
dim dt as Double
dim sonMesaj as Double
dim sahip as Integer
dim bantBitis as Double
dim etiketBitis as Double

' Zaman: System.GetElapsedTime() belgeye göre saniye döner; birim ilk karelerde ölçülür (bkz. Saat)
dim birim as Double
dim birimBelli as Boolean
dim alanSay as Integer
dim ilkHam as Double
dim sonDurum as Double
dim evR, evG, evB, depR, depG, depB as Integer

' Gol atağı (sim_atak): sahnede hazır duran parçalar ve oynatma bilgisi
dim atak, atakPanel, aTop as Container
dim aC, aN, aF, aFGovde, aFHalka, aFNo, aFAd, aFAdArka as Array[Container]
dim cT0, cDur, cSx, cSy, cDx, cDy, cLen, cKal, nT0, fT0 as Array[Double]
dim cSay, nSay, fSay as Integer
dim atakBas, panelT0, topBasX, topBasY as Double
dim atakAktif as Boolean

' ---------------------------------------------------------------- parametreler

sub OnInitParameters()
	RegisterPushButton("kur", "Sahneyi kur", 1)
	RegisterParameterDouble("saha_px", "Saha genişliği (piksel)", 1400.0, 600.0, 1900.0)
	RegisterParameterDouble("ekran_y", "Sahne yüksekliği (piksel)", 1080.0, 480.0, 4320.0)
	RegisterPushButton("olcek", "Ölçeği yenile", 2)
end sub

sub OnInit()
	birim = 1.0
	birimBelli = false
	alanSay = 0
	sonDurum = 0.0
	fx.Size = 23
	fy.Size = 23
	tx.Size = 23
	ty.Size = 23
	sahip = -1
	evR = 232
	evG = 163
	evB = 23
	depR = 122
	depG = 0
	depB = 60
	Baglan()
	dim m as SharedMemory
	m = VizCommunication.Map
	m.RegisterChangedCallback("sim_poz")
	m.RegisterChangedCallback("sim_kadro")
	m.RegisterChangedCallback("sim_renk")
	m.RegisterChangedCallback("sim_metin")
	m.RegisterChangedCallback("sim_sahip")
	m.RegisterChangedCallback("sim_bant")
	m.RegisterChangedCallback("sim_etiket")
	m.RegisterChangedCallback("sim_atak")
	cT0.Size = 80
	cDur.Size = 80
	cSx.Size = 80
	cSy.Size = 80
	cDx.Size = 80
	cDy.Size = 80
	cLen.Size = 80
	cKal.Size = 80
	nT0.Size = 16
	fT0.Size = 16
	atakAktif = false
end sub

sub OnExecAction(buttonId as Integer)
	if buttonId = 1 then Kur()
	if buttonId = 2 then
		if bagli then
			OlcegiAyarla()
		else
			println("Önce 'Sahneyi kur' düğmesine basın.")
		end if
	end if
end sub

' ---------------------------------------------------------------- yardımcılar

' Saniye cinsinden geçen süre. GetElapsedTime'ın birimi ilk 25 karede ölçülür:
' kare başına artış 1'den büyükse milisaniye kabul edilir.
function Saat() as Double
	Saat = System.GetElapsedTime() * birim
end function

sub ZamanBiriminiOlc()
	dim ham, fark as Double
	ham = System.GetElapsedTime()
	alanSay = alanSay + 1
	if alanSay = 1 then ilkHam = ham
	if alanSay = 26 then
		fark = (ham - ilkHam) / 25.0
		if fark > 1.0 then
			birim = 0.001
		else
			birim = 1.0
		end if
		birimBelli = true
	end if
end sub

' Dışarıdan okunabilen durum satırı (VIZ_COMMUNICATION*MAP GET_STRING_ELEMENT "sim_durum")
sub DurumYaz()
	dim simdi as Double
	simdi = Saat()
	if simdi - sonDurum < 1.0 then exit sub
	sonDurum = simdi
	dim atakVar as Integer
	atakVar = 0
	if atak <> null then atakVar = 1
	VizCommunication.Map["sim_durum"] = "ham=" & CStr(System.GetElapsedTime()) & " birim=" & CStr(birim) & " olculdu=" & CStr(birimBelli) & " kare=" & CStr(alanSay) & " bagli=" & CStr(bagli) & " atak=" & CStr(atakVar) & " bantBitis=" & CStr(bantBitis) & " saat=" & CStr(simdi) & " dt=" & CStr(dt)
end sub

function Iki(n as Integer) as String
	if n < 10 then
		Iki = "0" & CStr(n)
	else
		Iki = CStr(n)
	end if
end function

' 0-21 sırasına göre oyuncu konteynerinin adı: E01..E11 ev sahibi, D01..D11 deplasman
function OyuncuAdi(i as Integer) as String
	if i < 11 then
		OyuncuAdi = "E" & Iki(i + 1)
	else
		OyuncuAdi = "D" & Iki(i - 10)
	end if
end function

function Grup(parent as Container, ad as String, x as Double, y as Double, z as Double) as Container
	dim c as Container
	c = parent.AddContainer(TL_DOWN)
	c.Name = ad
	c.Position.xyz = CVertex(x, y, z)
	Grup = c
end function

' Işıktan etkilenmeyen düz renk; a = saydamlık 0-100
sub Boya(c as Container, r as Integer, g as Integer, b as Integer, a as Double)
	dim k as Color
	k.SetRGB(r / 255.0, g / 255.0, b / 255.0)
	if c.Material = null then c.CreateMaterial()
	c.Material.Lit = false
	c.Material.Color = k
	c.Material.Diffuse = k
	c.Material.Ambient = k
	c.Material.Emission = k
	if a < 99.9 then
		if c.Alpha = null then c.CreateAlpha()
		c.Alpha.Value = a
	end if
end sub

sub Olcu(c as Container, tur as Integer, a as Double, b as Double, metin as String)
	fitC.Push(c)
	fitK.Push(tur)
	fitA.Push(a)
	fitB.Push(b)
	fitT.Push(metin)
	fitOk.Push(0)
end sub

function Dikdortgen(parent as Container, ad as String, x as Double, y as Double, z as Double, w as Double, h as Double, r as Integer, g as Integer, b as Integer, a as Double) as Container
	dim c as Container
	c = Grup(parent, ad, x, y, z)
	c.CreateGeometry("BUILT_IN*GEOM*Rectangle")
	Boya(c, r, g, b, a)
	Olcu(c, 0, w, h, "")
	Dikdortgen = c
end function

function Daire(parent as Container, ad as String, x as Double, y as Double, z as Double, cap as Double, r as Integer, g as Integer, b as Integer, a as Double) as Container
	dim c as Container
	c = Grup(parent, ad, x, y, z)
	c.CreateGeometry("BUILT_IN*GEOM*Circle")
	Boya(c, r, g, b, a)
	Olcu(c, 1, cap, cap, "")
	Daire = c
end function

' hiza: TEXT_ORIENTATION_LEFT / CENTER / RIGHT
function Yazi(parent as Container, ad as String, x as Double, y as Double, z as Double, yukseklik as Double, hiza as Integer, metin as String, r as Integer, g as Integer, b as Integer) as Container
	dim c as Container
	c = sablon.CopyTo(parent, TL_DOWN)
	c.Name = ad
	c.Active = true
	c.Position.xyz = CVertex(x, y, z)
	c.Scaling.xyz = CVertex(1.0, 1.0, 1.0)
	c.Geometry.Text = "88"
	c.Geometry.Orientation = hiza
	c.Geometry.VerticalOrientation = TEXT_ORIENTATION_VERTICAL_CENTER
	Boya(c, r, g, b, 100.0)
	Olcu(c, 2, yukseklik, 0.0, metin)
	Yazi = c
end function

' Çizgi: beyaz, ince dikdörtgen
sub Cizgi(parent as Container, ad as String, x as Double, y as Double, w as Double, h as Double)
	Dikdortgen(parent, ad, x, y, 0.06, w, h, 255, 255, 255, 88.0)
end sub

' Zemin rengine göre okunabilir yazı rengi (açık zeminde koyu, koyu zeminde beyaz)
function Koyu(r as Integer, g as Integer, b as Integer) as Boolean
	Koyu = (0.299 * r + 0.587 * g + 0.114 * b) > 140.0
end function

' ---------------------------------------------------------------- kurulum

sub Kur()
	sablon = Scene.FindContainer("YAZI")
	if sablon = null then
		println("HATA: Sahnede 'YAZI' adlı bir metin konteyneri yok. Önce onu ekleyin.")
		exit sub
	end if
	sablon.Active = false

	bagli = false
	DeleteChildren()
	fitC.Clear()
	fitK.Clear()
	fitA.Clear()
	fitB.Clear()
	fitT.Clear()
	fitOk.Clear()

	dim arka as Color
	arka.SetRGB(15 / 255.0, 42 / 255.0, 26 / 255.0)
	Scene.BackgroundColor = arka

	' ---- saha (metre)
	saha = Grup(this, "SAHA", 0.0, 0.0, 0.0)
	cim = Dikdortgen(saha, "CIM", 0.0, 0.0, 0.0, 105.0, 68.0, 27, 102, 57, 100.0)

	dim s as Integer
	dim sx as Double
	dim yan as String
	for s = 0 to 1
		if s = 0 then
			sx = -1.0
			yan = "SOL"
		else
			sx = 1.0
			yan = "SAG"
		end if
		' Ceza yayı: beyaz daire + çim rengi daire, kutu içindeki kısmı çim rengi örtüyle kapanır
		Daire(saha, "YAY_" & yan, sx * 41.5, 0.0, 0.01, 18.52, 255, 255, 255, 88.0)
		Daire(saha, "YAY_IC_" & yan, sx * 41.5, 0.0, 0.02, 18.08, 27, 102, 57, 100.0)
		Dikdortgen(saha, "CEZA_ORTU_" & yan, sx * 44.25, 0.0, 0.03, 16.5, 40.32, 27, 102, 57, 100.0)
	next

	Daire(saha, "ORTA_DAIRE", 0.0, 0.0, 0.01, 18.52, 255, 255, 255, 88.0)
	Daire(saha, "ORTA_DAIRE_IC", 0.0, 0.0, 0.02, 18.08, 27, 102, 57, 100.0)

	' Biçilmiş çim görüntüsü: 14 şeridin yarısı hafif açık
	dim i as Integer
	for i = 0 to 13
		if i mod 2 = 0 then
			Dikdortgen(saha, "SERIT_" & Iki(i), -52.5 + 7.5 * i + 3.75, 0.0, 0.04, 7.5, 68.0, 255, 255, 255, 5.0)
		end if
	next

	' Çizgiler (0.22 m)
	dim w as Double
	w = 0.22
	Cizgi(saha, "KENAR_UST", 0.0, 34.0, 105.0 + w, w)
	Cizgi(saha, "KENAR_ALT", 0.0, -34.0, 105.0 + w, w)
	Cizgi(saha, "KENAR_SOL", -52.5, 0.0, w, 68.0 + w)
	Cizgi(saha, "KENAR_SAG", 52.5, 0.0, w, 68.0 + w)
	Cizgi(saha, "ORTA_CIZGI", 0.0, 0.0, w, 68.0)
	Daire(saha, "ORTA_NOKTA", 0.0, 0.0, 0.07, 0.6, 255, 255, 255, 88.0)
	for s = 0 to 1
		if s = 0 then
			sx = -1.0
			yan = "SOL"
		else
			sx = 1.0
			yan = "SAG"
		end if
		Cizgi(saha, "CEZA_ON_" & yan, sx * 36.0, 0.0, w, 40.32)
		Cizgi(saha, "CEZA_UST_" & yan, sx * 44.25, 20.16, 16.5, w)
		Cizgi(saha, "CEZA_ALT_" & yan, sx * 44.25, -20.16, 16.5, w)
		Cizgi(saha, "KALE_ALAN_ON_" & yan, sx * 47.0, 0.0, w, 18.32)
		Cizgi(saha, "KALE_ALAN_UST_" & yan, sx * 49.75, 9.16, 5.5, w)
		Cizgi(saha, "KALE_ALAN_ALT_" & yan, sx * 49.75, -9.16, 5.5, w)
		Daire(saha, "PENALTI_" & yan, sx * 41.5, 0.0, 0.07, 0.5, 255, 255, 255, 88.0)
		' Kale: kale çizgisinin dışında, hafif saydam ağ ve çerçeve
		Dikdortgen(saha, "KALE_AG_" & yan, sx * 53.5, 0.0, 0.05, 2.0, 7.32, 255, 255, 255, 12.0)
		Cizgi(saha, "KALE_ARKA_" & yan, sx * 54.5, 0.0, w, 7.32 + w)
		Cizgi(saha, "KALE_UST_" & yan, sx * 53.5, 3.66, 2.0, w)
		Cizgi(saha, "KALE_ALT_" & yan, sx * 53.5, -3.66, 2.0, w)
	next

	' ---- oyuncular: varsayılan 4-3-3 dizilişi (kumanda bağlanınca kendi konumlarını gönderir)
	dim oyuncular as Container
	oyuncular = Grup(saha, "OYUNCULAR", 0.0, 0.0, 0.3)
	dim px, py, ox, oyy as Double
	dim p, c as Container
	dim k as Integer
	for i = 0 to 21
		k = i mod 11
		if k = 0 then
			px = 0.045
			py = 0.5
		elseif k <= 4 then
			px = 0.16
			py = 0.5 + (k - 2.5) * 0.22
		elseif k <= 7 then
			px = 0.31
			py = 0.5 + (k - 6.0) * 0.24
		else
			px = 0.44
			py = 0.5 + (k - 9.0) * 0.27
		end if
		if i >= 11 then
			px = 1.0 - px
			py = 1.0 - py
		end if
		ox = (px - 0.5) * 105.0
		oyy = (0.5 - py) * 68.0
		p = Grup(oyuncular, OyuncuAdi(i), ox, oyy, 0.0)
		if i < 11 then
			Daire(p, "HALKA", 0.0, 0.0, 0.0, 3.8, 255, 255, 255, 95.0)
			Daire(p, "GOVDE", 0.0, 0.0, 0.02, 3.2, 232, 163, 23, 100.0)
			Yazi(p, "NO", 0.0, 0.0, 0.04, 1.6, TEXT_ORIENTATION_CENTER, CStr(k + 1), 17, 17, 17)
		else
			Daire(p, "HALKA", 0.0, 0.0, 0.0, 3.8, 255, 255, 255, 95.0)
			Daire(p, "GOVDE", 0.0, 0.0, 0.02, 3.2, 122, 0, 60, 100.0)
			Yazi(p, "NO", 0.0, 0.0, 0.04, 1.6, TEXT_ORIENTATION_CENTER, CStr(k + 1), 255, 255, 255)
		end if
	next

	' ---- top
	dim top as Container
	top = Grup(saha, "TOP", 0.0, 0.0, 0.5)
	Daire(top, "GOLGE", 0.15, -0.3, 0.0, 1.6, 0, 0, 0, 40.0)
	Daire(top, "TOP_IC", 0.0, 0.0, 0.02, 1.4, 255, 255, 255, 100.0)

	' ---- topu taşıyan oyuncunun adı
	isim = Grup(saha, "ISIM", 0.0, 3.4, 0.7)
	Dikdortgen(isim, "ISIM_ARKA", 0.0, 0.0, 0.0, 9.0, 2.3, 8, 11, 17, 86.0)
	Yazi(isim, "ISIM_YAZI", 0.0, 0.0, 0.02, 1.3, TEXT_ORIENTATION_CENTER, "Oyuncu", 255, 255, 255)

	' ---- gol atağı: kumandanın dolduracağı hazır parçalar (80 çizgi parçası, 16 nokta, 16 forma, top)
	' Çizgi parçası: dış konteyner uzunluk ve açıyı taşır, içindeki 1 x 1 metrelik dikdörtgen şekli.
	dim atakG, ap as Container
	atakG = Grup(saha, "ATAK", 0.0, 0.0, 0.6)
	for i = 0 to 79
		p = Grup(atakG, "A_C" & Iki(i), 0.0, 0.0, 0.0)
		Dikdortgen(p, "G", 0.0, 0.0, 0.0, 1.0, 1.0, 255, 255, 255, 92.0)
	next
	for i = 0 to 15
		Daire(atakG, "A_N" & Iki(i), 0.0, 0.0, 0.05, 1.2, 255, 255, 255, 100.0)
	next
	for i = 0 to 15
		p = Grup(atakG, "A_F" & Iki(i), 0.0, 0.0, 0.1)
		Daire(p, "HALKA", 0.0, 0.0, 0.0, 4.0, 255, 255, 255, 95.0)
		Daire(p, "GOVDE", 0.0, 0.0, 0.02, 3.4, 232, 163, 23, 100.0)
		Yazi(p, "NO", 0.0, 0.0, 0.04, 1.7, TEXT_ORIENTATION_CENTER, "10", 17, 17, 17)
		Dikdortgen(p, "AD_ARKA", 0.0, 3.3, 0.05, 10.0, 2.2, 8, 11, 17, 82.0)
		Yazi(p, "AD", 0.0, 3.3, 0.07, 1.3, TEXT_ORIENTATION_CENTER, "Oyuncu", 255, 255, 255)
	next
	p = Grup(atakG, "A_TOP", 0.0, 0.0, 0.2)
	Daire(p, "GOLGE", 0.15, -0.3, 0.0, 1.6, 0, 0, 0, 40.0)
	Daire(p, "TOP_IC", 0.0, 0.0, 0.02, 1.4, 255, 255, 255, 100.0)

	' ---- ekran katmanı (piksel)
	ekran = Grup(this, "EKRAN", 0.0, 0.0, 1.0)

	dim tabela as Container
	tabela = Grup(ekran, "TABELA", 0.0, 492.0, 0.0)
	Dikdortgen(tabela, "T_ARKA", 0.0, 0.0, 0.0, 700.0, 64.0, 8, 11, 17, 92.0)
	Dikdortgen(tabela, "T_EV_RENK", -344.0, 0.0, 0.5, 12.0, 64.0, 232, 163, 23, 100.0)
	Dikdortgen(tabela, "T_DEP_RENK", 344.0, 0.0, 0.5, 12.0, 64.0, 122, 0, 60, 100.0)
	Dikdortgen(tabela, "T_SKOR_ARKA", 0.0, 0.0, 0.5, 130.0, 52.0, 5, 7, 11, 100.0)
	Yazi(tabela, "T_EV_AD", -82.0, 0.0, 1.0, 26.0, TEXT_ORIENTATION_RIGHT, "EV SAHİBİ", 255, 255, 255)
	Yazi(tabela, "T_SKOR", 0.0, 0.0, 1.0, 34.0, TEXT_ORIENTATION_CENTER, "0 - 0", 255, 255, 255)
	Yazi(tabela, "T_DEP_AD", 82.0, 0.0, 1.0, 26.0, TEXT_ORIENTATION_LEFT, "DEPLASMAN", 255, 255, 255)
	Dikdortgen(tabela, "T_DAKIKA_ARKA", 412.0, 0.0, 0.5, 100.0, 64.0, 243, 245, 249, 100.0)
	Yazi(tabela, "T_DAKIKA", 412.0, 0.0, 1.0, 26.0, TEXT_ORIENTATION_CENTER, "0'", 11, 14, 20)

	bant = Grup(ekran, "BANT", 0.0, 0.0, 4.0)
	Dikdortgen(bant, "B_ARKA", 0.0, 0.0, 0.0, 1920.0, 190.0, 8, 11, 17, 86.0)
	Dikdortgen(bant, "B_UST", 0.0, 92.0, 0.5, 1920.0, 6.0, 227, 6, 19, 100.0)
	Dikdortgen(bant, "B_ALT", 0.0, -92.0, 0.5, 1920.0, 6.0, 227, 6, 19, 100.0)
	Yazi(bant, "B_BASLIK", 0.0, 18.0, 1.0, 80.0, TEXT_ORIENTATION_CENTER, "GOL!  1 - 0", 255, 255, 255)
	Yazi(bant, "B_ALT_YAZI", 0.0, -50.0, 1.0, 30.0, TEXT_ORIENTATION_CENTER, "Oyuncu  ·  23'", 255, 255, 255)

	etiket = Grup(ekran, "ETIKET", 0.0, -478.0, 3.0)
	Dikdortgen(etiket, "E_ARKA", 0.0, 0.0, 0.0, 760.0, 50.0, 8, 11, 17, 88.0)
	Daire(etiket, "E_NOKTA", -350.0, 0.0, 0.5, 16.0, 232, 163, 23, 100.0)
	Yazi(etiket, "E_YAZI", 12.0, 0.0, 1.0, 24.0, TEXT_ORIENTATION_CENTER, "23'  KORNER  ·  Takım", 255, 255, 255)

	' ---- gol atağının alt paneli: başlık ve atakta yer alan oyuncular
	ap = Grup(ekran, "ATAK_PANEL", 0.0, -430.0, 3.5)
	Dikdortgen(ap, "AP_ARKA", 0.0, 0.0, 0.0, 1240.0, 140.0, 8, 11, 17, 90.0)
	Dikdortgen(ap, "AP_SERIT", 0.0, 67.0, 0.5, 1240.0, 6.0, 227, 6, 19, 100.0)
	Yazi(ap, "AP_BASLIK", 0.0, 26.0, 1.0, 38.0, TEXT_ORIENTATION_CENTER, "GOL", 255, 255, 255)
	Yazi(ap, "AP_OYUNCULAR", 0.0, -30.0, 1.0, 28.0, TEXT_ORIENTATION_CENTER, "Oyuncular", 210, 216, 226)

	bekle = 0
	kurDurum = 1
	println("Sahne kuruldu: " & CStr(fitC.Size) & " nesne ölçülüyor...")
end sub

' Kurulumdan sonraki karelerde: şekilleri ölç ve boyutla, sonra ekran ölçeğini ayarla
sub KurulumuTamamla()
	dim i, kalan as Integer
	dim d, v1, v2 as Vertex
	dim sc, pw, ppu, hedef as Double
	if kurDurum = 1 then
		kalan = 0
		for i = 0 to fitC.UBound
			if fitOk[i] = 0 then
				d = fitC[i].GetBoundingBoxDimensions()
				if d.x > 0.0001 and d.y > 0.0001 then
					if fitK[i] = 0 then
						fitC[i].Scaling.xyz = CVertex(fitA[i] / d.x, fitB[i] / d.y, 1.0)
					elseif fitK[i] = 1 then
						sc = fitA[i] / d.x
						fitC[i].Scaling.xyz = CVertex(sc, sc, 1.0)
					else
						sc = fitA[i] / d.y
						fitC[i].Scaling.xyz = CVertex(sc, sc, sc)
						fitC[i].Geometry.Text = fitT[i]
					end if
					fitOk[i] = 1
				else
					kalan = kalan + 1
				end if
			end if
		next
		bekle = bekle + 1
		if kalan = 0 then
			kurDurum = 2
		elseif bekle > 250 then
			println("UYARI: " & CStr(kalan) & " nesne ölçülemedi (geometri oluşmamış olabilir).")
			kurDurum = 2
		end if
	elseif kurDurum = 2 then
		kurDurum = 3                       ' ölçekler bir kare sonra ekrana yansır
	elseif kurDurum = 3 then
		OlcegiAyarla()
		kurDurum = 4
		Baglan()
		isim.Active = false
		bant.Active = false
		etiket.Active = false
		AtakGizle()
		println("Kurulum tamamlandı. Sahneyi kaydedebilirsiniz.")
	end if
end sub

' SAHA'yı ekranda istenen genişliğe getirir ve EKRAN'ı 1 birim = 1 piksel olacak şekilde ölçekler.
' Hesap kameradan yapılır: kameranın z=0 düzlemindeki görüş yüksekliği = 2 x uzaklık x tan(görüş açısı / 2).
' (Ekran ölçümü kullanılmaz: Viz Artist'te o ölçüm önizleme penceresinin boyutuna göre geliyor.)
' İstenildiği kadar tekrar çalıştırılabilir.
sub OlcegiAyarla()
	dim kam as Camera
	dim d, fovy, yari, gorunur, ppu, sc, hedef as Double
	kam = Scene.CurrentCamera
	d = kam.Position.z
	fovy = kam.Fovy
	if d <= 0.0 or fovy <= 0.0 then
		println("UYARI: Kamera bilgisi okunamadı (uzaklık " & CStr(d) & ", açı " & CStr(fovy) & ").")
		exit sub
	end if
	if fovy < 3.2 then
		yari = fovy / 2.0                              ' açı radyan gelmiş
	else
		yari = fovy * 3.14159265358979 / 360.0         ' derece -> yarım açı (radyan)
	end if
	gorunur = 2.0 * d * Sin(yari) / Cos(yari)
	ppu = GetParameterDouble("ekran_y") / gorunur
	hedef = GetParameterDouble("saha_px")
	sc = hedef / (105.0 * ppu)
	saha.Scaling.xyz = CVertex(sc, sc, sc)
	ekran.Scaling.xyz = CVertex(1.0 / ppu, 1.0 / ppu, 1.0 / ppu)
	println("Ölçek: kamera uzaklığı " & CStr(d) & ", görüş açısı " & CStr(fovy) & "; 1 birim = " & CStr(ppu) & " piksel; saha " & CStr(hedef) & " piksel.")
end sub

' ---------------------------------------------------------------- yayın

' Kurulmuş sahnedeki nesneleri bulur
sub Baglan()
	dim i as Integer
	bagli = false
	saha = FindSubContainer("SAHA")
	ekran = FindSubContainer("EKRAN")
	if saha = null or ekran = null then exit sub
	cim = saha.FindSubContainer("CIM")
	isim = saha.FindSubContainer("ISIM")
	isimYazi = saha.FindSubContainer("ISIM$ISIM_YAZI")
	bant = ekran.FindSubContainer("BANT")
	etiket = ekran.FindSubContainer("ETIKET")
	oy.Clear()
	oyGovde.Clear()
	oyHalka.Clear()
	oyNo.Clear()
	oyAd.Clear()
	for i = 0 to 21
		oy.Push(saha.FindSubContainer("OYUNCULAR$" & OyuncuAdi(i)))
		oyGovde.Push(oy[i].FindSubContainer("GOVDE"))
		oyHalka.Push(oy[i].FindSubContainer("HALKA"))
		oyNo.Push(oy[i].FindSubContainer("NO"))
		oyAd.Push("")
		fx[i] = oy[i].Position.x
		fy[i] = oy[i].Position.y
		tx[i] = fx[i]
		ty[i] = fy[i]
	next
	oy.Push(saha.FindSubContainer("TOP"))
	AtakBagla()
	fx[22] = oy[22].Position.x
	fy[22] = oy[22].Position.y
	tx[22] = fx[22]
	ty[22] = fy[22]
	t0 = Saat()
	dt = 0.1
	sonMesaj = t0
	bagli = true
	SonDegerleriUygula()
end sub

' Paylaşımlı bellek yalnızca değer DEĞİŞİNCE haber verir. Sahne yeniden kurulduğunda ya da betik
' yeniden derlendiğinde bellekte duran son kadro, renk, yazı ve konumlar burada sahneye geri yüklenir.
' Bant ve etiket geçici olduğu için geri yüklenmez.
sub SonDegerleriUygula()
	dim m as SharedMemory
	dim anahtar as Array[String]
	dim i as Integer
	m = VizCommunication.Map
	anahtar.Push("sim_renk")
	anahtar.Push("sim_kadro")
	anahtar.Push("sim_metin")
	anahtar.Push("sim_poz")
	anahtar.Push("sim_sahip")
	for i = 0 to anahtar.UBound
		if m.ContainsKey(anahtar[i]) then OnSharedMemoryVariableChanged(m, anahtar[i])
	next
end sub

sub OnExecPerField()
	dim i as Integer
	dim u, simdi as Double
	if not birimBelli then ZamanBiriminiOlc()
	DurumYaz()
	if kurDurum >= 1 and kurDurum <= 3 then
		KurulumuTamamla()
		exit sub
	end if
	if not bagli then exit sub
	simdi = Saat()
	u = (simdi - t0) / dt
	if u > 1.0 then u = 1.0
	if u < 0.0 then u = 0.0
	for i = 0 to 22
		oy[i].Position.x = fx[i] + (tx[i] - fx[i]) * u
		oy[i].Position.y = fy[i] + (ty[i] - fy[i]) * u
	next
	if sahip >= 0 and sahip <= 21 then
		isim.Position.x = oy[sahip].Position.x
		isim.Position.y = oy[sahip].Position.y + 3.4
	end if
	if bantBitis > 0.0 and simdi > bantBitis then
		bant.Active = false
		bantBitis = 0.0
	end if
	if etiketBitis > 0.0 and simdi > etiketBitis then
		etiket.Active = false
		etiketBitis = 0.0
	end if
	if atakAktif then AtakCiz()
end sub

sub OnSharedMemoryVariableChanged(map as SharedMemory, mapKey as String)
	dim s as String
	if not bagli then exit sub
	s = TirnakSil(map[mapKey])
	if mapKey = "sim_poz" then
		KonumAl(s)
	elseif mapKey = "sim_kadro" then
		KadroAl(s)
	elseif mapKey = "sim_renk" then
		RenkAl(s)
	elseif mapKey = "sim_metin" then
		MetinAl(s)
	elseif mapKey = "sim_sahip" then
		SahipAl(s)
	elseif mapKey = "sim_bant" then
		BantAl(s)
	elseif mapKey = "sim_etiket" then
		EtiketAl(s)
	elseif mapKey = "sim_atak" then
		AtakAl(s)
	end if
end sub

' Viz, SET_STRING_ELEMENT ile yazılan değeri çevresindeki tırnaklarla birlikte saklıyor; onları atar
function TirnakSil(s as String) as String
	dim q as String
	q = Chr(34)
	if s.Length >= 1 then
		if s.GetSubstring(0, 1) = q then s = s.GetSubstring(1, s.Length - 1)
	end if
	if s.Length >= 1 then
		if s.GetSubstring(s.Length - 1, 1) = q then s = s.GetSubstring(0, s.Length - 1)
	end if
	TirnakSil = s
end function

' Yeni hedefler gelince: nesneler o anki yerlerinden yeni hedefe, iki mesaj arası süre boyunca akar
sub KonumAl(s as String)
	dim parca, xy as Array[String]
	dim i as Integer
	dim simdi, u as Double
	s.Split(";", parca)
	simdi = Saat()
	u = (simdi - t0) / dt
	if u > 1.0 then u = 1.0
	if u < 0.0 then u = 0.0
	dt = simdi - sonMesaj
	if dt < 0.03 then dt = 0.03
	if dt > 0.4 then dt = 0.4
	sonMesaj = simdi
	for i = 0 to 22
		if i <= parca.UBound then
			parca[i].Split(",", xy)
			if xy.Size >= 2 then
				fx[i] = fx[i] + (tx[i] - fx[i]) * u
				fy[i] = fy[i] + (ty[i] - fy[i]) * u
				tx[i] = (CDbl(xy[0]) - 0.5) * 105.0
				ty[i] = (0.5 - CDbl(xy[1])) * 68.0
				if xy.Size >= 3 then
					oy[i].Active = (xy[2] <> "0")
				else
					oy[i].Active = true
				end if
			end if
		end if
	next
	t0 = simdi
end sub

sub KadroAl(s as String)
	dim parca, alan as Array[String]
	dim i as Integer
	s.Split(";", parca)
	for i = 0 to 21
		if i <= parca.UBound then
			parca[i].Split("|", alan)
			if alan.Size >= 1 then oyNo[i].Geometry.Text = alan[0]
			if alan.Size >= 2 then oyAd[i] = alan[1]
		end if
	next
	if sahip >= 0 and sahip <= 21 then isimYazi.Geometry.Text = oyAd[sahip]
end sub

sub RenkAl(s as String)
	dim parca, rgb as Array[String]
	dim i, r, g, b as Integer
	s.Split(";", parca)
	if parca.Size < 2 then exit sub
	parca[0].Split(",", rgb)
	if rgb.Size >= 3 then
		evR = CInt(rgb[0])
		evG = CInt(rgb[1])
		evB = CInt(rgb[2])
	end if
	parca[1].Split(",", rgb)
	if rgb.Size >= 3 then
		depR = CInt(rgb[0])
		depG = CInt(rgb[1])
		depB = CInt(rgb[2])
	end if
	for i = 0 to 21
		if i < 11 then
			r = evR
			g = evG
			b = evB
		else
			r = depR
			g = depG
			b = depB
		end if
		if i mod 11 = 0 then
			' Kaleci: koyu forma, takım rengi halka
			Boya(oyGovde[i], 26, 30, 37, 100.0)
			Boya(oyHalka[i], r, g, b, 100.0)
			Boya(oyNo[i], 255, 255, 255, 100.0)
		else
			Boya(oyGovde[i], r, g, b, 100.0)
			if Koyu(r, g, b) then
				Boya(oyHalka[i], 10, 12, 16, 85.0)
				Boya(oyNo[i], 17, 17, 17, 100.0)
			else
				Boya(oyHalka[i], 255, 255, 255, 95.0)
				Boya(oyNo[i], 255, 255, 255, 100.0)
			end if
		end if
	next
	Boya(ekran.FindSubContainer("TABELA$T_EV_RENK"), evR, evG, evB, 100.0)
	Boya(ekran.FindSubContainer("TABELA$T_DEP_RENK"), depR, depG, depB, 100.0)
end sub

sub MetinAl(s as String)
	dim parca as Array[String]
	dim i, k as Integer
	dim ad, deger as String
	dim c as Container
	s.Split("|", parca)
	for i = 0 to parca.UBound
		k = parca[i].Find("=")
		if k > 0 then
			ad = parca[i].Left(k)
			deger = parca[i].GetSubstring(k + 1, parca[i].Length - k - 1)
			c = ekran.FindSubContainer("TABELA$T_" & ad)
			if c <> null then c.Geometry.Text = deger
		end if
	next
end sub

sub SahipAl(s as String)
	sahip = CInt(s)
	if sahip >= 0 and sahip <= 21 then
		isimYazi.Geometry.Text = oyAd[sahip]
		isim.Active = (oyAd[sahip] <> "")
	else
		isim.Active = false
	end if
end sub

sub BantAl(s as String)
	dim parca, rgb as Array[String]
	dim sure as Double
	s.Split("|", parca)
	if parca.Size < 1 or s = "" then
		bant.Active = false
		bantBitis = 0.0
		exit sub
	end if
	bant.FindSubContainer("B_BASLIK").Geometry.Text = parca[0]
	if parca.Size >= 2 then
		bant.FindSubContainer("B_ALT_YAZI").Geometry.Text = parca[1]
	else
		bant.FindSubContainer("B_ALT_YAZI").Geometry.Text = ""
	end if
	if parca.Size >= 3 then
		parca[2].Split(",", rgb)
		if rgb.Size >= 3 then
			Boya(bant.FindSubContainer("B_UST"), CInt(rgb[0]), CInt(rgb[1]), CInt(rgb[2]), 100.0)
			Boya(bant.FindSubContainer("B_ALT"), CInt(rgb[0]), CInt(rgb[1]), CInt(rgb[2]), 100.0)
		end if
	end if
	sure = 0.0
	if parca.Size >= 4 then sure = CDbl(parca[3])
	bant.Active = true
	if sure > 0.0 then
		bantBitis = Saat() + sure
	else
		bantBitis = 0.0
	end if
end sub

sub EtiketAl(s as String)
	dim parca, rgb as Array[String]
	dim sure as Double
	s.Split("|", parca)
	if parca.Size < 1 or s = "" then
		etiket.Active = false
		etiketBitis = 0.0
		exit sub
	end if
	etiket.FindSubContainer("E_YAZI").Geometry.Text = parca[0]
	if parca.Size >= 2 then
		parca[1].Split(",", rgb)
		if rgb.Size >= 3 then Boya(etiket.FindSubContainer("E_NOKTA"), CInt(rgb[0]), CInt(rgb[1]), CInt(rgb[2]), 100.0)
	end if
	sure = 0.0
	if parca.Size >= 3 then sure = CDbl(parca[2])
	etiket.Active = true
	if sure > 0.0 then
		etiketBitis = Saat() + sure
	else
		etiketBitis = 0.0
	end if
end sub

' ---------------------------------------------------------------- gol atağı

' Kurulmuş sahnedeki atak parçalarını bulur (sahne bu betiğin eski sürümüyle kurulduysa atak = null kalır)
sub AtakBagla()
	dim i as Integer
	atak = saha.FindSubContainer("ATAK")
	atakPanel = ekran.FindSubContainer("ATAK_PANEL")
	aC.Clear()
	aN.Clear()
	aF.Clear()
	aFGovde.Clear()
	aFHalka.Clear()
	aFNo.Clear()
	aFAd.Clear()
	aFAdArka.Clear()
	if atak = null or atakPanel = null then
		atak = null
		exit sub
	end if
	for i = 0 to 79
		aC.Push(atak.FindSubContainer("A_C" & Iki(i)))
	next
	for i = 0 to 15
		aN.Push(atak.FindSubContainer("A_N" & Iki(i)))
		aF.Push(atak.FindSubContainer("A_F" & Iki(i)))
		aFGovde.Push(aF[i].FindSubContainer("GOVDE"))
		aFHalka.Push(aF[i].FindSubContainer("HALKA"))
		aFNo.Push(aF[i].FindSubContainer("NO"))
		aFAd.Push(aF[i].FindSubContainer("AD"))
		aFAdArka.Push(aF[i].FindSubContainer("AD_ARKA"))
	next
	aTop = atak.FindSubContainer("A_TOP")
	AtakGizle()
end sub

sub AtakGizle()
	dim i as Integer
	atakAktif = false
	if atak = null then exit sub
	for i = 0 to aC.UBound
		aC[i].Active = false
	next
	for i = 0 to aN.UBound
		aN[i].Active = false
		aF[i].Active = false
	next
	aTop.Active = false
	atakPanel.Active = false
	atak.Active = false
end sub

sub AtakAl(s as String)
	dim bas, oge, al, rgb as Array[String]
	dim i, r, g, b as Integer
	AtakGizle()
	if s = "" then exit sub
	if atak = null then
		println("Sahnede ATAK katmanı yok: betiği güncelleyip 'Sahneyi kur' düğmesine basın.")
		exit sub
	end if
	s.Split("|", bas)
	if bas.Size < 4 then exit sub
	r = 232
	g = 163
	b = 23
	bas[0].Split(",", rgb)
	if rgb.Size >= 3 then
		r = CInt(rgb[0])
		g = CInt(rgb[1])
		b = CInt(rgb[2])
	end if
	atakPanel.FindSubContainer("AP_BASLIK").Geometry.Text = bas[1]
	atakPanel.FindSubContainer("AP_OYUNCULAR").Geometry.Text = bas[2]
	Boya(atakPanel.FindSubContainer("AP_SERIT"), r, g, b, 100.0)
	for i = 0 to aF.UBound
		Boya(aFGovde[i], r, g, b, 100.0)
		if Koyu(r, g, b) then
			Boya(aFHalka[i], 10, 12, 16, 85.0)
			Boya(aFNo[i], 17, 17, 17, 100.0)
		else
			Boya(aFHalka[i], 255, 255, 255, 95.0)
			Boya(aFNo[i], 255, 255, 255, 100.0)
		end if
	next

	cSay = 0
	nSay = 0
	fSay = 0
	panelT0 = -1.0
	topBasX = 0.0
	topBasY = 0.0
	bas[3].Split(";", oge)
	for i = 0 to oge.UBound
		oge[i].Split(",", al)
		if al.Size >= 2 then
			if al[0] = "C" and al.Size >= 10 and cSay <= aC.UBound then
				cT0[cSay] = CDbl(al[1])
				cDur[cSay] = CDbl(al[2])
				if cDur[cSay] < 0.01 then cDur[cSay] = 0.01
				cSx[cSay] = CDbl(al[3])
				cSy[cSay] = CDbl(al[4])
				cDx[cSay] = CDbl(al[5])
				cDy[cSay] = CDbl(al[6])
				cLen[cSay] = CDbl(al[7])
				cKal[cSay] = CDbl(al[9])
				aC[cSay].Rotation.xyz = CVertex(0.0, 0.0, CDbl(al[8]))
				cSay = cSay + 1
			elseif al[0] = "N" and al.Size >= 4 and nSay <= aN.UBound then
				nT0[nSay] = CDbl(al[1])
				aN[nSay].Position.xyz = CVertex(CDbl(al[2]), CDbl(al[3]), 0.05)
				if nSay = 0 then
					topBasX = CDbl(al[2])
					topBasY = CDbl(al[3])
				end if
				nSay = nSay + 1
			elseif al[0] = "F" and al.Size >= 5 and fSay <= aF.UBound then
				fT0[fSay] = CDbl(al[1])
				aF[fSay].Position.xyz = CVertex(CDbl(al[2]), CDbl(al[3]), 0.1)
				aFNo[fSay].Geometry.Text = al[4]
				if aFAd[fSay] <> null then
					if al.Size >= 6 then
						aFAd[fSay].Geometry.Text = al[5]
						aFAd[fSay].Active = (al[5] <> "")
						aFAdArka[fSay].Active = (al[5] <> "")
					else
						aFAd[fSay].Active = false
						aFAdArka[fSay].Active = false
					end if
				end if
				fSay = fSay + 1
			elseif al[0] = "P" then
				panelT0 = CDbl(al[1])
			end if
		end if
	next
	atakBas = Saat()
	atak.Active = true
	atakAktif = true
end sub

' Her karede: çizgiler baştan sona uzar, noktalar ve formalar zamanı gelince belirir, top en son çizginin ucundadır
sub AtakCiz()
	dim t, u, bx, by, k as Double
	dim i as Integer
	t = Saat() - atakBas
	bx = topBasX
	by = topBasY
	for i = 0 to cSay - 1
		u = (t - cT0[i]) / cDur[i]
		if u <= 0.0 then
			aC[i].Active = false
		else
			if u > 1.0 then u = 1.0
			aC[i].Active = true
			aC[i].Position.xyz = CVertex(cSx[i] + cDx[i] * u * 0.5, cSy[i] + cDy[i] * u * 0.5, 0.0)
			aC[i].Scaling.xyz = CVertex(cLen[i] * u + 0.001, cKal[i], 1.0)
			bx = cSx[i] + cDx[i] * u
			by = cSy[i] + cDy[i] * u
		end if
	next
	for i = 0 to nSay - 1
		aN[i].Active = (t >= nT0[i])
	next
	for i = 0 to fSay - 1
		if t < fT0[i] then
			aF[i].Active = false
		else
			k = (t - fT0[i]) / 0.22
			if k > 1.0 then k = 1.0
			k = k * (2.0 - k)
			aF[i].Active = true
			aF[i].Scaling.xyz = CVertex(k, k, 1.0)
		end if
	next
	aTop.Active = (nSay > 0)
	aTop.Position.xyz = CVertex(bx, by, 0.2)
	atakPanel.Active = (panelT0 >= 0.0 and t >= panelT0)
end sub
