namespace SporAnaliz.Kumanda;

partial class AnaForm
{
    /// <summary>Tasarımcının kullandığı bileşenler.</summary>
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Pencerenin yerleşimi. Visual Studio'da AnaForm.cs'e çift tıklayınca açılan tasarım ekranı bu kodu düzenler.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        pnlUst = new Panel();
        lblBaslik = new Label();
        lblAltBaslik = new Label();
        lblVizEtiket = new Label();
        txtHost = new TextBox();
        lblIkiNokta = new Label();
        txtPort = new TextBox();
        btnBaglan = new Button();
        lblViz = new Label();
        lblVeri = new Label();
        pnlSekme = new Panel();
        btnSekmeSim = new Button();
        btnSekmeCanli = new Button();
        tabSekmeler = new TabControl();
        tabSim = new TabPage();
        pnlOrta = new Panel();
        saha = new SahaOnizleme();
        pnlAkis = new Panel();
        lstAkis = new ListBox();
        lblAkisBaslik = new Label();
        pnlOrtaBosluk = new Panel();
        pnlTabela = new Panel();
        lblTabela = new Label();
        pnlSag = new Panel();
        flowAdimlar = new FlowLayoutPanel();
        lblAdimBaslik = new Label();
        btnAnaliz = new Button();
        lblBilgi = new Label();
        btnYukle = new Button();
        btnOynat = new Button();
        btnDuraklat = new Button();
        btnBastan = new Button();
        flowHiz = new FlowLayoutPanel();
        lblHiz = new Label();
        btnHiz1 = new Button();
        btnHiz2 = new Button();
        btnHiz4 = new Button();
        btnTemizle = new Button();
        lblKisayol = new Label();
        pnlSol = new Panel();
        lstMac = new ListView();
        colTarih = new ColumnHeader();
        colMac = new ColumnHeader();
        colSkor = new ColumnHeader();
        pnlSolBosluk = new Panel();
        pnlAra = new Panel();
        txtAra = new TextBox();
        pnlTarih = new Panel();
        dtBas = new DateTimePicker();
        dtBit = new DateTimePicker();
        pnlHafta = new Panel();
        cmbHafta = new ComboBox();
        cmbAsama = new ComboBox();
        cmbTurnuva = new ComboBox();
        lblMacBaslik = new Label();
        tabCanli = new TabPage();
        cPnlOrta = new Panel();
        cizim = new AtakCizimAlani();
        cLblDurum = new Label();
        cLblBaslik = new Label();
        cPnlSag = new Panel();
        flowSag = new FlowLayoutPanel();
        lblGolBaslik = new Label();
        flowTakim = new FlowLayoutPanel();
        btnEv = new Button();
        btnDep = new Button();
        pnlDakikaSkor = new Panel();
        lblDakika = new Label();
        txtDakika = new TextBox();
        lblSkor = new Label();
        numEv = new NumericUpDown();
        lblTire = new Label();
        numDep = new NumericUpDown();
        lblOyuncuBaslik = new Label();
        flowOyuncular = new FlowLayoutPanel();
        flowIslem = new FlowLayoutPanel();
        btnGeriAl = new Button();
        cBtnTemizle = new Button();
        btnOnizle = new Button();
        btnYeni = new Button();
        btnGonder = new Button();
        btnKaldir = new Button();
        lblKayitBaslik = new Label();
        lstKayitlar = new ListBox();
        btnSil = new Button();
        cPnlSol = new Panel();
        lstMaclar = new ListBox();
        cPnlSolBosluk = new Panel();
        cPnlAra = new Panel();
        cTxtAra = new TextBox();
        pnlGun = new Panel();
        dtGun = new DateTimePicker();
        cLblMacBaslik = new Label();
        tik = new System.Windows.Forms.Timer(components);
        durumTik = new System.Windows.Forms.Timer(components);
        gorunum = new KumandaGorunumu(components);
        pnlUst.SuspendLayout();
        pnlSekme.SuspendLayout();
        tabSekmeler.SuspendLayout();
        tabSim.SuspendLayout();
        pnlOrta.SuspendLayout();
        pnlAkis.SuspendLayout();
        pnlTabela.SuspendLayout();
        pnlSag.SuspendLayout();
        flowAdimlar.SuspendLayout();
        flowHiz.SuspendLayout();
        pnlSol.SuspendLayout();
        pnlAra.SuspendLayout();
        pnlTarih.SuspendLayout();
        pnlHafta.SuspendLayout();
        tabCanli.SuspendLayout();
        cPnlOrta.SuspendLayout();
        cPnlSag.SuspendLayout();
        flowSag.SuspendLayout();
        flowTakim.SuspendLayout();
        pnlDakikaSkor.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)numEv).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numDep).BeginInit();
        flowIslem.SuspendLayout();
        cPnlSol.SuspendLayout();
        cPnlAra.SuspendLayout();
        pnlGun.SuspendLayout();
        SuspendLayout();
        // 
        // pnlUst
        // 
        pnlUst.BackColor = Color.FromArgb(31, 35, 43);
        pnlUst.Controls.Add(lblBaslik);
        pnlUst.Controls.Add(lblAltBaslik);
        pnlUst.Controls.Add(lblVizEtiket);
        pnlUst.Controls.Add(txtHost);
        pnlUst.Controls.Add(lblIkiNokta);
        pnlUst.Controls.Add(txtPort);
        pnlUst.Controls.Add(btnBaglan);
        pnlUst.Controls.Add(lblViz);
        pnlUst.Controls.Add(lblVeri);
        pnlUst.Dock = DockStyle.Top;
        pnlUst.Location = new Point(0, 0);
        pnlUst.Name = "pnlUst";
        pnlUst.Size = new Size(1484, 58);
        pnlUst.TabIndex = 0;
        // 
        // lblBaslik
        // 
        lblBaslik.AutoSize = true;
        lblBaslik.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
        lblBaslik.ForeColor = Color.FromArgb(226, 230, 236);
        lblBaslik.Location = new Point(18, 15);
        lblBaslik.Name = "lblBaslik";
        lblBaslik.Size = new Size(126, 25);
        lblBaslik.TabIndex = 0;
        lblBaslik.Text = "SPOR ANALİZ";
        // 
        // lblAltBaslik
        // 
        lblAltBaslik.AutoSize = true;
        lblAltBaslik.Font = new Font("Segoe UI", 10F);
        lblAltBaslik.ForeColor = Color.FromArgb(140, 149, 163);
        lblAltBaslik.Location = new Point(146, 19);
        lblAltBaslik.Name = "lblAltBaslik";
        lblAltBaslik.Size = new Size(89, 19);
        lblAltBaslik.TabIndex = 1;
        lblAltBaslik.Text = "Viz Kumanda";
        // 
        // lblVizEtiket
        // 
        lblVizEtiket.AutoSize = true;
        lblVizEtiket.ForeColor = Color.FromArgb(140, 149, 163);
        lblVizEtiket.Location = new Point(330, 19);
        lblVizEtiket.Name = "lblVizEtiket";
        lblVizEtiket.Size = new Size(68, 17);
        lblVizEtiket.TabIndex = 2;
        lblVizEtiket.Text = "Viz Engine";
        // 
        // txtHost
        // 
        txtHost.BackColor = Color.FromArgb(42, 47, 57);
        txtHost.BorderStyle = BorderStyle.FixedSingle;
        txtHost.ForeColor = Color.FromArgb(226, 230, 236);
        txtHost.Location = new Point(410, 16);
        txtHost.Name = "txtHost";
        txtHost.Size = new Size(130, 25);
        txtHost.TabIndex = 1;
        txtHost.Text = "127.0.0.1";
        // 
        // lblIkiNokta
        // 
        lblIkiNokta.AutoSize = true;
        lblIkiNokta.ForeColor = Color.FromArgb(140, 149, 163);
        lblIkiNokta.Location = new Point(544, 19);
        lblIkiNokta.Name = "lblIkiNokta";
        lblIkiNokta.Size = new Size(11, 17);
        lblIkiNokta.TabIndex = 3;
        lblIkiNokta.Text = ":";
        // 
        // txtPort
        // 
        txtPort.BackColor = Color.FromArgb(42, 47, 57);
        txtPort.BorderStyle = BorderStyle.FixedSingle;
        txtPort.ForeColor = Color.FromArgb(226, 230, 236);
        txtPort.Location = new Point(556, 16);
        txtPort.Name = "txtPort";
        txtPort.Size = new Size(62, 25);
        txtPort.TabIndex = 2;
        txtPort.Text = "6100";
        // 
        // btnBaglan
        // 
        btnBaglan.BackColor = Color.FromArgb(42, 47, 57);
        btnBaglan.Cursor = Cursors.Hand;
        btnBaglan.FlatAppearance.BorderColor = Color.FromArgb(58, 64, 77);
        btnBaglan.FlatAppearance.MouseOverBackColor = Color.FromArgb(52, 58, 70);
        btnBaglan.FlatStyle = FlatStyle.Flat;
        btnBaglan.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        btnBaglan.ForeColor = Color.FromArgb(226, 230, 236);
        btnBaglan.Location = new Point(630, 13);
        btnBaglan.Name = "btnBaglan";
        btnBaglan.Size = new Size(110, 32);
        btnBaglan.TabIndex = 3;
        btnBaglan.Text = "BAĞLAN";
        btnBaglan.UseVisualStyleBackColor = false;
        btnBaglan.Click += btnBaglan_Click;
        // 
        // lblViz
        // 
        lblViz.ForeColor = Color.FromArgb(140, 149, 163);
        lblViz.Location = new Point(756, 18);
        lblViz.Name = "lblViz";
        lblViz.Size = new Size(380, 22);
        lblViz.TabIndex = 4;
        lblViz.Text = "● Bağlı değil";
        // 
        // lblVeri
        // 
        lblVeri.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        lblVeri.ForeColor = Color.FromArgb(140, 149, 163);
        lblVeri.Location = new Point(1188, 18);
        lblVeri.Name = "lblVeri";
        lblVeri.Size = new Size(278, 22);
        lblVeri.TabIndex = 5;
        lblVeri.Text = "Veri yükleniyor…";
        lblVeri.TextAlign = ContentAlignment.MiddleRight;
        // 
        // pnlSekme
        // 
        pnlSekme.BackColor = Color.FromArgb(24, 27, 33);
        pnlSekme.Controls.Add(btnSekmeSim);
        pnlSekme.Controls.Add(btnSekmeCanli);
        pnlSekme.Dock = DockStyle.Top;
        pnlSekme.Location = new Point(0, 58);
        pnlSekme.Name = "pnlSekme";
        pnlSekme.Size = new Size(1484, 48);
        pnlSekme.TabIndex = 4;
        // 
        // btnSekmeSim
        // 
        btnSekmeSim.BackColor = Color.FromArgb(42, 47, 57);
        btnSekmeSim.Cursor = Cursors.Hand;
        btnSekmeSim.FlatAppearance.BorderColor = Color.FromArgb(58, 64, 77);
        btnSekmeSim.FlatStyle = FlatStyle.Flat;
        btnSekmeSim.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        btnSekmeSim.ForeColor = Color.FromArgb(226, 230, 236);
        btnSekmeSim.Location = new Point(16, 8);
        btnSekmeSim.Name = "btnSekmeSim";
        btnSekmeSim.Size = new Size(180, 34);
        btnSekmeSim.TabIndex = 0;
        btnSekmeSim.Text = "SİMÜLASYON";
        btnSekmeSim.UseVisualStyleBackColor = false;
        btnSekmeSim.Click += btnSekmeSim_Click;
        // 
        // btnSekmeCanli
        // 
        btnSekmeCanli.BackColor = Color.FromArgb(42, 47, 57);
        btnSekmeCanli.Cursor = Cursors.Hand;
        btnSekmeCanli.FlatAppearance.BorderColor = Color.FromArgb(58, 64, 77);
        btnSekmeCanli.FlatStyle = FlatStyle.Flat;
        btnSekmeCanli.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        btnSekmeCanli.ForeColor = Color.FromArgb(226, 230, 236);
        btnSekmeCanli.Location = new Point(204, 8);
        btnSekmeCanli.Name = "btnSekmeCanli";
        btnSekmeCanli.Size = new Size(220, 34);
        btnSekmeCanli.TabIndex = 1;
        btnSekmeCanli.Text = "CANLI MAÇ · GOL ATAĞI";
        btnSekmeCanli.UseVisualStyleBackColor = false;
        btnSekmeCanli.Click += btnSekmeCanli_Click;
        // 
        // tabSekmeler
        // 
        tabSekmeler.Controls.Add(tabSim);
        tabSekmeler.Controls.Add(tabCanli);
        tabSekmeler.Dock = DockStyle.Fill;
        tabSekmeler.Location = new Point(0, 106);
        tabSekmeler.Name = "tabSekmeler";
        tabSekmeler.SelectedIndex = 0;
        tabSekmeler.Size = new Size(1484, 755);
        tabSekmeler.TabIndex = 5;
        // 
        // tabSim
        // 
        tabSim.BackColor = Color.FromArgb(24, 27, 33);
        tabSim.Controls.Add(pnlOrta);
        tabSim.Controls.Add(pnlSag);
        tabSim.Controls.Add(pnlSol);
        tabSim.Location = new Point(4, 26);
        tabSim.Name = "tabSim";
        tabSim.Size = new Size(1476, 725);
        tabSim.TabIndex = 0;
        tabSim.Text = "SİMÜLASYON";
        // 
        // pnlOrta
        // 
        pnlOrta.BackColor = Color.FromArgb(24, 27, 33);
        pnlOrta.Controls.Add(saha);
        pnlOrta.Controls.Add(pnlAkis);
        pnlOrta.Controls.Add(pnlOrtaBosluk);
        pnlOrta.Controls.Add(pnlTabela);
        pnlOrta.Dock = DockStyle.Fill;
        pnlOrta.Location = new Point(370, 0);
        pnlOrta.Name = "pnlOrta";
        pnlOrta.Padding = new Padding(16);
        pnlOrta.Size = new Size(792, 725);
        pnlOrta.TabIndex = 3;
        // 
        // saha
        // 
        saha.BackColor = Color.FromArgb(24, 27, 33);
        saha.Dock = DockStyle.Fill;
        saha.Location = new Point(16, 92);
        saha.Name = "saha";
        saha.Size = new Size(760, 417);
        saha.TabIndex = 0;
        saha.TabStop = false;
        // 
        // pnlAkis
        // 
        pnlAkis.Controls.Add(lstAkis);
        pnlAkis.Controls.Add(lblAkisBaslik);
        pnlAkis.Dock = DockStyle.Bottom;
        pnlAkis.Location = new Point(16, 509);
        pnlAkis.Name = "pnlAkis";
        pnlAkis.Padding = new Padding(0, 12, 0, 0);
        pnlAkis.Size = new Size(760, 200);
        pnlAkis.TabIndex = 1;
        // 
        // lstAkis
        // 
        lstAkis.BackColor = Color.FromArgb(31, 35, 43);
        lstAkis.BorderStyle = BorderStyle.None;
        lstAkis.Dock = DockStyle.Fill;
        lstAkis.DrawMode = DrawMode.OwnerDrawFixed;
        lstAkis.Font = new Font("Consolas", 10F);
        lstAkis.ForeColor = Color.FromArgb(196, 202, 212);
        lstAkis.IntegralHeight = false;
        lstAkis.ItemHeight = 22;
        lstAkis.Location = new Point(0, 42);
        lstAkis.Name = "lstAkis";
        lstAkis.Size = new Size(760, 158);
        lstAkis.TabIndex = 0;
        lstAkis.TabStop = false;
        lstAkis.DrawItem += lstAkis_DrawItem;
        // 
        // lblAkisBaslik
        // 
        lblAkisBaslik.BackColor = Color.FromArgb(31, 35, 43);
        lblAkisBaslik.Dock = DockStyle.Top;
        lblAkisBaslik.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        lblAkisBaslik.ForeColor = Color.FromArgb(140, 149, 163);
        lblAkisBaslik.Location = new Point(0, 12);
        lblAkisBaslik.Name = "lblAkisBaslik";
        lblAkisBaslik.Padding = new Padding(10, 0, 0, 0);
        lblAkisBaslik.Size = new Size(760, 30);
        lblAkisBaslik.TabIndex = 1;
        lblAkisBaslik.Text = "MAÇ AKIŞI";
        lblAkisBaslik.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // pnlOrtaBosluk
        // 
        pnlOrtaBosluk.Dock = DockStyle.Top;
        pnlOrtaBosluk.Location = new Point(16, 80);
        pnlOrtaBosluk.Name = "pnlOrtaBosluk";
        pnlOrtaBosluk.Size = new Size(760, 12);
        pnlOrtaBosluk.TabIndex = 2;
        // 
        // pnlTabela
        // 
        pnlTabela.BackColor = Color.FromArgb(31, 35, 43);
        pnlTabela.Controls.Add(lblTabela);
        pnlTabela.Dock = DockStyle.Top;
        pnlTabela.Location = new Point(16, 16);
        pnlTabela.Name = "pnlTabela";
        pnlTabela.Size = new Size(760, 64);
        pnlTabela.TabIndex = 3;
        // 
        // lblTabela
        // 
        lblTabela.Dock = DockStyle.Fill;
        lblTabela.Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold);
        lblTabela.ForeColor = Color.FromArgb(226, 230, 236);
        lblTabela.Location = new Point(0, 0);
        lblTabela.Name = "lblTabela";
        lblTabela.Size = new Size(760, 64);
        lblTabela.TabIndex = 0;
        lblTabela.Text = "—";
        lblTabela.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // pnlSag
        // 
        pnlSag.BackColor = Color.FromArgb(31, 35, 43);
        pnlSag.Controls.Add(flowAdimlar);
        pnlSag.Dock = DockStyle.Right;
        pnlSag.Location = new Point(1162, 0);
        pnlSag.Name = "pnlSag";
        pnlSag.Padding = new Padding(16, 12, 12, 16);
        pnlSag.Size = new Size(314, 725);
        pnlSag.TabIndex = 2;
        // 
        // flowAdimlar
        // 
        flowAdimlar.AutoScroll = true;
        flowAdimlar.Controls.Add(lblAdimBaslik);
        flowAdimlar.Controls.Add(btnAnaliz);
        flowAdimlar.Controls.Add(lblBilgi);
        flowAdimlar.Controls.Add(btnYukle);
        flowAdimlar.Controls.Add(btnOynat);
        flowAdimlar.Controls.Add(btnDuraklat);
        flowAdimlar.Controls.Add(btnBastan);
        flowAdimlar.Controls.Add(flowHiz);
        flowAdimlar.Controls.Add(btnTemizle);
        flowAdimlar.Controls.Add(lblKisayol);
        flowAdimlar.Dock = DockStyle.Fill;
        flowAdimlar.FlowDirection = FlowDirection.TopDown;
        flowAdimlar.Location = new Point(16, 12);
        flowAdimlar.Name = "flowAdimlar";
        flowAdimlar.Size = new Size(286, 697);
        flowAdimlar.TabIndex = 0;
        flowAdimlar.WrapContents = false;
        // 
        // lblAdimBaslik
        // 
        lblAdimBaslik.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        lblAdimBaslik.ForeColor = Color.FromArgb(140, 149, 163);
        lblAdimBaslik.Location = new Point(0, 0);
        lblAdimBaslik.Margin = new Padding(0);
        lblAdimBaslik.Name = "lblAdimBaslik";
        lblAdimBaslik.Size = new Size(280, 30);
        lblAdimBaslik.TabIndex = 0;
        lblAdimBaslik.Text = "ADIMLAR";
        lblAdimBaslik.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // btnAnaliz
        // 
        btnAnaliz.BackColor = Color.FromArgb(42, 47, 57);
        btnAnaliz.Cursor = Cursors.Hand;
        btnAnaliz.FlatAppearance.BorderColor = Color.FromArgb(58, 64, 77);
        btnAnaliz.FlatAppearance.MouseOverBackColor = Color.FromArgb(52, 58, 70);
        btnAnaliz.FlatStyle = FlatStyle.Flat;
        btnAnaliz.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
        btnAnaliz.ForeColor = Color.FromArgb(226, 230, 236);
        btnAnaliz.Location = new Point(0, 30);
        btnAnaliz.Margin = new Padding(0, 0, 0, 8);
        btnAnaliz.Name = "btnAnaliz";
        btnAnaliz.Padding = new Padding(10, 0, 10, 0);
        btnAnaliz.Size = new Size(280, 46);
        btnAnaliz.TabIndex = 0;
        btnAnaliz.Text = "1   ANALİZ ET";
        btnAnaliz.TextAlign = ContentAlignment.MiddleLeft;
        btnAnaliz.UseVisualStyleBackColor = false;
        btnAnaliz.Click += btnAnaliz_Click;
        // 
        // lblBilgi
        // 
        lblBilgi.BackColor = Color.FromArgb(38, 43, 52);
        lblBilgi.Font = new Font("Segoe UI", 9.75F);
        lblBilgi.ForeColor = Color.FromArgb(196, 202, 212);
        lblBilgi.Location = new Point(0, 84);
        lblBilgi.Margin = new Padding(0, 0, 0, 14);
        lblBilgi.Name = "lblBilgi";
        lblBilgi.Padding = new Padding(10, 8, 10, 8);
        lblBilgi.Size = new Size(280, 172);
        lblBilgi.TabIndex = 1;
        lblBilgi.Text = "Soldan bir maç seçin.";
        // 
        // btnYukle
        // 
        btnYukle.BackColor = Color.FromArgb(42, 47, 57);
        btnYukle.Cursor = Cursors.Hand;
        btnYukle.FlatAppearance.BorderColor = Color.FromArgb(58, 64, 77);
        btnYukle.FlatAppearance.MouseOverBackColor = Color.FromArgb(52, 58, 70);
        btnYukle.FlatStyle = FlatStyle.Flat;
        btnYukle.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
        btnYukle.ForeColor = Color.FromArgb(226, 230, 236);
        btnYukle.Location = new Point(0, 270);
        btnYukle.Margin = new Padding(0, 0, 0, 8);
        btnYukle.Name = "btnYukle";
        btnYukle.Padding = new Padding(10, 0, 10, 0);
        btnYukle.Size = new Size(280, 46);
        btnYukle.TabIndex = 1;
        btnYukle.Text = "2   VİZ'E YÜKLE";
        btnYukle.TextAlign = ContentAlignment.MiddleLeft;
        btnYukle.UseVisualStyleBackColor = false;
        btnYukle.Click += btnYukle_Click;
        // 
        // btnOynat
        // 
        btnOynat.BackColor = Color.FromArgb(42, 47, 57);
        btnOynat.Cursor = Cursors.Hand;
        btnOynat.FlatAppearance.BorderColor = Color.FromArgb(58, 64, 77);
        btnOynat.FlatAppearance.MouseOverBackColor = Color.FromArgb(52, 58, 70);
        btnOynat.FlatStyle = FlatStyle.Flat;
        btnOynat.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
        btnOynat.ForeColor = Color.FromArgb(226, 230, 236);
        btnOynat.Location = new Point(0, 324);
        btnOynat.Margin = new Padding(0, 0, 0, 8);
        btnOynat.Name = "btnOynat";
        btnOynat.Padding = new Padding(10, 0, 10, 0);
        btnOynat.Size = new Size(280, 46);
        btnOynat.TabIndex = 2;
        btnOynat.Text = "3   ▶  OYNAT";
        btnOynat.TextAlign = ContentAlignment.MiddleLeft;
        btnOynat.UseVisualStyleBackColor = false;
        btnOynat.Click += btnOynat_Click;
        // 
        // btnDuraklat
        // 
        btnDuraklat.BackColor = Color.FromArgb(42, 47, 57);
        btnDuraklat.Cursor = Cursors.Hand;
        btnDuraklat.FlatAppearance.BorderColor = Color.FromArgb(58, 64, 77);
        btnDuraklat.FlatAppearance.MouseOverBackColor = Color.FromArgb(52, 58, 70);
        btnDuraklat.FlatStyle = FlatStyle.Flat;
        btnDuraklat.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
        btnDuraklat.ForeColor = Color.FromArgb(226, 230, 236);
        btnDuraklat.Location = new Point(0, 378);
        btnDuraklat.Margin = new Padding(0, 0, 0, 8);
        btnDuraklat.Name = "btnDuraklat";
        btnDuraklat.Padding = new Padding(10, 0, 10, 0);
        btnDuraklat.Size = new Size(280, 46);
        btnDuraklat.TabIndex = 3;
        btnDuraklat.Text = "❚❚  DURAKLAT";
        btnDuraklat.TextAlign = ContentAlignment.MiddleLeft;
        btnDuraklat.UseVisualStyleBackColor = false;
        btnDuraklat.Click += btnDuraklat_Click;
        // 
        // btnBastan
        // 
        btnBastan.BackColor = Color.FromArgb(42, 47, 57);
        btnBastan.Cursor = Cursors.Hand;
        btnBastan.FlatAppearance.BorderColor = Color.FromArgb(58, 64, 77);
        btnBastan.FlatAppearance.MouseOverBackColor = Color.FromArgb(52, 58, 70);
        btnBastan.FlatStyle = FlatStyle.Flat;
        btnBastan.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
        btnBastan.ForeColor = Color.FromArgb(226, 230, 236);
        btnBastan.Location = new Point(0, 432);
        btnBastan.Margin = new Padding(0, 0, 0, 8);
        btnBastan.Name = "btnBastan";
        btnBastan.Padding = new Padding(10, 0, 10, 0);
        btnBastan.Size = new Size(280, 46);
        btnBastan.TabIndex = 4;
        btnBastan.Text = "↺  BAŞTAN";
        btnBastan.TextAlign = ContentAlignment.MiddleLeft;
        btnBastan.UseVisualStyleBackColor = false;
        btnBastan.Click += btnBastan_Click;
        // 
        // flowHiz
        // 
        flowHiz.Controls.Add(lblHiz);
        flowHiz.Controls.Add(btnHiz1);
        flowHiz.Controls.Add(btnHiz2);
        flowHiz.Controls.Add(btnHiz4);
        flowHiz.Location = new Point(0, 490);
        flowHiz.Margin = new Padding(0, 4, 0, 10);
        flowHiz.Name = "flowHiz";
        flowHiz.Size = new Size(280, 40);
        flowHiz.TabIndex = 5;
        flowHiz.WrapContents = false;
        // 
        // lblHiz
        // 
        lblHiz.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        lblHiz.ForeColor = Color.FromArgb(140, 149, 163);
        lblHiz.Location = new Point(0, 0);
        lblHiz.Margin = new Padding(0, 0, 10, 0);
        lblHiz.Name = "lblHiz";
        lblHiz.Size = new Size(40, 36);
        lblHiz.TabIndex = 0;
        lblHiz.Text = "HIZ";
        lblHiz.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // btnHiz1
        // 
        btnHiz1.BackColor = Color.FromArgb(42, 47, 57);
        btnHiz1.Cursor = Cursors.Hand;
        btnHiz1.FlatAppearance.BorderColor = Color.FromArgb(58, 64, 77);
        btnHiz1.FlatStyle = FlatStyle.Flat;
        btnHiz1.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        btnHiz1.ForeColor = Color.FromArgb(226, 230, 236);
        btnHiz1.Location = new Point(50, 0);
        btnHiz1.Margin = new Padding(0, 0, 8, 0);
        btnHiz1.Name = "btnHiz1";
        btnHiz1.Size = new Size(68, 36);
        btnHiz1.TabIndex = 5;
        btnHiz1.Text = "1x";
        btnHiz1.UseVisualStyleBackColor = false;
        btnHiz1.Click += btnHiz1_Click;
        // 
        // btnHiz2
        // 
        btnHiz2.BackColor = Color.FromArgb(42, 47, 57);
        btnHiz2.Cursor = Cursors.Hand;
        btnHiz2.FlatAppearance.BorderColor = Color.FromArgb(58, 64, 77);
        btnHiz2.FlatStyle = FlatStyle.Flat;
        btnHiz2.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        btnHiz2.ForeColor = Color.FromArgb(226, 230, 236);
        btnHiz2.Location = new Point(126, 0);
        btnHiz2.Margin = new Padding(0, 0, 8, 0);
        btnHiz2.Name = "btnHiz2";
        btnHiz2.Size = new Size(68, 36);
        btnHiz2.TabIndex = 6;
        btnHiz2.Text = "2x";
        btnHiz2.UseVisualStyleBackColor = false;
        btnHiz2.Click += btnHiz2_Click;
        // 
        // btnHiz4
        // 
        btnHiz4.BackColor = Color.FromArgb(42, 47, 57);
        btnHiz4.Cursor = Cursors.Hand;
        btnHiz4.FlatAppearance.BorderColor = Color.FromArgb(58, 64, 77);
        btnHiz4.FlatStyle = FlatStyle.Flat;
        btnHiz4.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        btnHiz4.ForeColor = Color.FromArgb(226, 230, 236);
        btnHiz4.Location = new Point(202, 0);
        btnHiz4.Margin = new Padding(0);
        btnHiz4.Name = "btnHiz4";
        btnHiz4.Size = new Size(68, 36);
        btnHiz4.TabIndex = 7;
        btnHiz4.Text = "4x";
        btnHiz4.UseVisualStyleBackColor = false;
        btnHiz4.Click += btnHiz4_Click;
        // 
        // btnTemizle
        // 
        btnTemizle.BackColor = Color.FromArgb(42, 47, 57);
        btnTemizle.Cursor = Cursors.Hand;
        btnTemizle.FlatAppearance.BorderColor = Color.FromArgb(58, 64, 77);
        btnTemizle.FlatAppearance.MouseOverBackColor = Color.FromArgb(52, 58, 70);
        btnTemizle.FlatStyle = FlatStyle.Flat;
        btnTemizle.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        btnTemizle.ForeColor = Color.FromArgb(226, 230, 236);
        btnTemizle.Location = new Point(0, 540);
        btnTemizle.Margin = new Padding(0, 0, 0, 16);
        btnTemizle.Name = "btnTemizle";
        btnTemizle.Padding = new Padding(10, 0, 10, 0);
        btnTemizle.Size = new Size(280, 40);
        btnTemizle.TabIndex = 8;
        btnTemizle.Text = "TEMİZLE  (bant, etiket)";
        btnTemizle.TextAlign = ContentAlignment.MiddleLeft;
        btnTemizle.UseVisualStyleBackColor = false;
        btnTemizle.Click += btnTemizle_Click;
        // 
        // lblKisayol
        // 
        lblKisayol.Font = new Font("Segoe UI", 9F);
        lblKisayol.ForeColor = Color.FromArgb(120, 129, 143);
        lblKisayol.Location = new Point(0, 596);
        lblKisayol.Margin = new Padding(0);
        lblKisayol.Name = "lblKisayol";
        lblKisayol.Size = new Size(280, 90);
        lblKisayol.TabIndex = 9;
        lblKisayol.Text = "Kısayollar\r\nF2  Analiz et        F3  Viz'e yükle\r\nF5  Oynat              F6  Duraklat\r\nF7  Baştan            F8  Temizle";
        // 
        // pnlSol
        // 
        pnlSol.BackColor = Color.FromArgb(31, 35, 43);
        pnlSol.Controls.Add(lstMac);
        pnlSol.Controls.Add(pnlSolBosluk);
        pnlSol.Controls.Add(pnlAra);
        pnlSol.Controls.Add(pnlTarih);
        pnlSol.Controls.Add(pnlHafta);
        pnlSol.Controls.Add(cmbTurnuva);
        pnlSol.Controls.Add(lblMacBaslik);
        pnlSol.Dock = DockStyle.Left;
        pnlSol.Location = new Point(0, 0);
        pnlSol.Name = "pnlSol";
        pnlSol.Padding = new Padding(16, 12, 16, 16);
        pnlSol.Size = new Size(370, 725);
        pnlSol.TabIndex = 1;
        // 
        // lstMac
        // 
        lstMac.BackColor = Color.FromArgb(36, 40, 49);
        lstMac.BorderStyle = BorderStyle.None;
        lstMac.Columns.AddRange(new ColumnHeader[] { colTarih, colMac, colSkor });
        lstMac.Dock = DockStyle.Fill;
        lstMac.Font = new Font("Segoe UI", 10F);
        lstMac.ForeColor = Color.FromArgb(226, 230, 236);
        lstMac.FullRowSelect = true;
        lstMac.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        lstMac.Location = new Point(16, 229);
        lstMac.MultiSelect = false;
        lstMac.Name = "lstMac";
        lstMac.OwnerDraw = true;
        lstMac.ShowItemToolTips = true;
        lstMac.Size = new Size(338, 480);
        lstMac.TabIndex = 6;
        lstMac.UseCompatibleStateImageBehavior = false;
        lstMac.View = View.Details;
        lstMac.DrawColumnHeader += lstMac_DrawColumnHeader;
        lstMac.DrawItem += lstMac_DrawItem;
        lstMac.DrawSubItem += lstMac_DrawSubItem;
        lstMac.SelectedIndexChanged += lstMac_SelectedIndexChanged;
        lstMac.DoubleClick += lstMac_DoubleClick;
        // 
        // colTarih
        // 
        colTarih.Text = "Tarih";
        colTarih.Width = 96;
        // 
        // colMac
        // 
        colMac.Text = "Maç";
        colMac.Width = 186;
        // 
        // colSkor
        // 
        colSkor.Text = "Skor";
        colSkor.Width = 52;
        // 
        // pnlSolBosluk
        // 
        pnlSolBosluk.Dock = DockStyle.Top;
        pnlSolBosluk.Location = new Point(16, 219);
        pnlSolBosluk.Name = "pnlSolBosluk";
        pnlSolBosluk.Size = new Size(338, 10);
        pnlSolBosluk.TabIndex = 7;
        // 
        // pnlAra
        // 
        pnlAra.Controls.Add(txtAra);
        pnlAra.Dock = DockStyle.Top;
        pnlAra.Location = new Point(16, 183);
        pnlAra.Name = "pnlAra";
        pnlAra.Padding = new Padding(0, 8, 0, 0);
        pnlAra.Size = new Size(338, 36);
        pnlAra.TabIndex = 8;
        // 
        // txtAra
        // 
        txtAra.BackColor = Color.FromArgb(42, 47, 57);
        txtAra.BorderStyle = BorderStyle.FixedSingle;
        txtAra.Dock = DockStyle.Fill;
        txtAra.Font = new Font("Segoe UI", 10F);
        txtAra.ForeColor = Color.FromArgb(226, 230, 236);
        txtAra.Location = new Point(0, 8);
        txtAra.Name = "txtAra";
        txtAra.PlaceholderText = "Takım ara…";
        txtAra.Size = new Size(338, 25);
        txtAra.TabIndex = 5;
        txtAra.TextChanged += txtAra_TextChanged;
        // 
        // pnlTarih
        // 
        pnlTarih.Controls.Add(dtBas);
        pnlTarih.Controls.Add(dtBit);
        pnlTarih.Dock = DockStyle.Top;
        pnlTarih.Location = new Point(16, 141);
        pnlTarih.Name = "pnlTarih";
        pnlTarih.Size = new Size(338, 42);
        pnlTarih.TabIndex = 9;
        // 
        // dtBas
        // 
        dtBas.Format = DateTimePickerFormat.Short;
        dtBas.Location = new Point(0, 9);
        dtBas.Name = "dtBas";
        dtBas.Size = new Size(160, 25);
        dtBas.TabIndex = 3;
        dtBas.ValueChanged += tarih_ValueChanged;
        // 
        // dtBit
        // 
        dtBit.Format = DateTimePickerFormat.Short;
        dtBit.Location = new Point(178, 9);
        dtBit.Name = "dtBit";
        dtBit.Size = new Size(160, 25);
        dtBit.TabIndex = 4;
        dtBit.ValueChanged += tarih_ValueChanged;
        // 
        // pnlHafta
        // 
        pnlHafta.Controls.Add(cmbHafta);
        pnlHafta.Controls.Add(cmbAsama);
        pnlHafta.Dock = DockStyle.Top;
        pnlHafta.Location = new Point(16, 67);
        pnlHafta.Name = "pnlHafta";
        pnlHafta.Padding = new Padding(0, 8, 0, 0);
        pnlHafta.Size = new Size(338, 74);
        pnlHafta.TabIndex = 10;
        pnlHafta.Visible = false;
        // 
        // cmbHafta
        // 
        cmbHafta.BackColor = Color.FromArgb(42, 47, 57);
        cmbHafta.Dock = DockStyle.Bottom;
        cmbHafta.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbHafta.FlatStyle = FlatStyle.Flat;
        cmbHafta.Font = new Font("Segoe UI", 10F);
        cmbHafta.ForeColor = Color.FromArgb(226, 230, 236);
        cmbHafta.Location = new Point(0, 49);
        cmbHafta.Name = "cmbHafta";
        cmbHafta.Size = new Size(338, 25);
        cmbHafta.TabIndex = 2;
        cmbHafta.SelectedIndexChanged += cmbHafta_SelectedIndexChanged;
        // 
        // cmbAsama
        // 
        cmbAsama.BackColor = Color.FromArgb(42, 47, 57);
        cmbAsama.Dock = DockStyle.Top;
        cmbAsama.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbAsama.FlatStyle = FlatStyle.Flat;
        cmbAsama.Font = new Font("Segoe UI", 10F);
        cmbAsama.ForeColor = Color.FromArgb(226, 230, 236);
        cmbAsama.Location = new Point(0, 8);
        cmbAsama.Name = "cmbAsama";
        cmbAsama.Size = new Size(338, 25);
        cmbAsama.TabIndex = 1;
        cmbAsama.SelectedIndexChanged += cmbAsama_SelectedIndexChanged;
        // 
        // cmbTurnuva
        // 
        cmbTurnuva.BackColor = Color.FromArgb(42, 47, 57);
        cmbTurnuva.Dock = DockStyle.Top;
        cmbTurnuva.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbTurnuva.FlatStyle = FlatStyle.Flat;
        cmbTurnuva.Font = new Font("Segoe UI", 10F);
        cmbTurnuva.ForeColor = Color.FromArgb(226, 230, 236);
        cmbTurnuva.Location = new Point(16, 42);
        cmbTurnuva.Name = "cmbTurnuva";
        cmbTurnuva.Size = new Size(338, 25);
        cmbTurnuva.TabIndex = 0;
        cmbTurnuva.SelectedIndexChanged += cmbTurnuva_SelectedIndexChanged;
        // 
        // lblMacBaslik
        // 
        lblMacBaslik.Dock = DockStyle.Top;
        lblMacBaslik.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        lblMacBaslik.ForeColor = Color.FromArgb(140, 149, 163);
        lblMacBaslik.Location = new Point(16, 12);
        lblMacBaslik.Name = "lblMacBaslik";
        lblMacBaslik.Size = new Size(338, 30);
        lblMacBaslik.TabIndex = 11;
        lblMacBaslik.Text = "MAÇ SEÇİMİ";
        lblMacBaslik.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // tabCanli
        // 
        tabCanli.BackColor = Color.FromArgb(24, 27, 33);
        tabCanli.Controls.Add(cPnlOrta);
        tabCanli.Controls.Add(cPnlSag);
        tabCanli.Controls.Add(cPnlSol);
        tabCanli.Location = new Point(4, 24);
        tabCanli.Name = "tabCanli";
        tabCanli.Size = new Size(1476, 727);
        tabCanli.TabIndex = 1;
        tabCanli.Text = "CANLI MAÇ";
        // 
        // cPnlOrta
        // 
        cPnlOrta.BackColor = Color.FromArgb(24, 27, 33);
        cPnlOrta.Controls.Add(cizim);
        cPnlOrta.Controls.Add(cLblDurum);
        cPnlOrta.Controls.Add(cLblBaslik);
        cPnlOrta.Dock = DockStyle.Fill;
        cPnlOrta.Location = new Point(330, 0);
        cPnlOrta.Name = "cPnlOrta";
        cPnlOrta.Padding = new Padding(16);
        cPnlOrta.Size = new Size(800, 727);
        cPnlOrta.TabIndex = 1;
        // 
        // cizim
        // 
        cizim.BackColor = Color.FromArgb(24, 27, 33);
        cizim.Dock = DockStyle.Fill;
        cizim.Location = new Point(16, 72);
        cizim.Name = "cizim";
        cizim.SahaBoslugu = 26;
        cizim.Size = new Size(768, 599);
        cizim.TabIndex = 0;
        cizim.Degisti += cizim_Degisti;
        // 
        // cLblDurum
        // 
        cLblDurum.BackColor = Color.FromArgb(31, 35, 43);
        cLblDurum.Dock = DockStyle.Bottom;
        cLblDurum.ForeColor = Color.FromArgb(196, 202, 212);
        cLblDurum.Location = new Point(16, 671);
        cLblDurum.Name = "cLblDurum";
        cLblDurum.Padding = new Padding(12, 0, 0, 0);
        cLblDurum.Size = new Size(768, 40);
        cLblDurum.TabIndex = 1;
        cLblDurum.Text = "Soldan maçı seçin.";
        cLblDurum.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // cLblBaslik
        // 
        cLblBaslik.BackColor = Color.FromArgb(31, 35, 43);
        cLblBaslik.Dock = DockStyle.Top;
        cLblBaslik.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold);
        cLblBaslik.ForeColor = Color.FromArgb(226, 230, 236);
        cLblBaslik.Location = new Point(16, 16);
        cLblBaslik.Name = "cLblBaslik";
        cLblBaslik.Size = new Size(768, 56);
        cLblBaslik.TabIndex = 2;
        cLblBaslik.Text = "—";
        cLblBaslik.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // cPnlSag
        // 
        cPnlSag.BackColor = Color.FromArgb(31, 35, 43);
        cPnlSag.Controls.Add(flowSag);
        cPnlSag.Dock = DockStyle.Right;
        cPnlSag.Location = new Point(1130, 0);
        cPnlSag.Name = "cPnlSag";
        cPnlSag.Padding = new Padding(16, 12, 8, 16);
        cPnlSag.Size = new Size(346, 727);
        cPnlSag.TabIndex = 2;
        // 
        // flowSag
        // 
        flowSag.AutoScroll = true;
        flowSag.Controls.Add(lblGolBaslik);
        flowSag.Controls.Add(flowTakim);
        flowSag.Controls.Add(pnlDakikaSkor);
        flowSag.Controls.Add(lblOyuncuBaslik);
        flowSag.Controls.Add(flowOyuncular);
        flowSag.Controls.Add(flowIslem);
        flowSag.Controls.Add(lblKayitBaslik);
        flowSag.Controls.Add(lstKayitlar);
        flowSag.Controls.Add(btnSil);
        flowSag.Dock = DockStyle.Fill;
        flowSag.FlowDirection = FlowDirection.TopDown;
        flowSag.Location = new Point(16, 12);
        flowSag.Name = "flowSag";
        flowSag.Size = new Size(322, 699);
        flowSag.TabIndex = 0;
        flowSag.WrapContents = false;
        // 
        // lblGolBaslik
        // 
        lblGolBaslik.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        lblGolBaslik.ForeColor = Color.FromArgb(140, 149, 163);
        lblGolBaslik.Location = new Point(0, 0);
        lblGolBaslik.Margin = new Padding(0);
        lblGolBaslik.Name = "lblGolBaslik";
        lblGolBaslik.Size = new Size(300, 30);
        lblGolBaslik.TabIndex = 0;
        lblGolBaslik.Text = "GOLÜ ATAN TAKIM";
        lblGolBaslik.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // flowTakim
        // 
        flowTakim.Controls.Add(btnEv);
        flowTakim.Controls.Add(btnDep);
        flowTakim.Location = new Point(0, 30);
        flowTakim.Margin = new Padding(0, 0, 0, 8);
        flowTakim.Name = "flowTakim";
        flowTakim.Size = new Size(300, 44);
        flowTakim.TabIndex = 1;
        flowTakim.WrapContents = false;
        // 
        // btnEv
        // 
        btnEv.BackColor = Color.FromArgb(42, 47, 57);
        btnEv.Cursor = Cursors.Hand;
        btnEv.FlatAppearance.BorderColor = Color.FromArgb(58, 64, 77);
        btnEv.FlatStyle = FlatStyle.Flat;
        btnEv.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        btnEv.ForeColor = Color.FromArgb(226, 230, 236);
        btnEv.Location = new Point(0, 0);
        btnEv.Margin = new Padding(0, 0, 8, 0);
        btnEv.Name = "btnEv";
        btnEv.Size = new Size(146, 42);
        btnEv.TabIndex = 0;
        btnEv.Text = "EV SAHİBİ";
        btnEv.UseVisualStyleBackColor = false;
        btnEv.Click += btnEv_Click;
        // 
        // btnDep
        // 
        btnDep.BackColor = Color.FromArgb(42, 47, 57);
        btnDep.Cursor = Cursors.Hand;
        btnDep.FlatAppearance.BorderColor = Color.FromArgb(58, 64, 77);
        btnDep.FlatStyle = FlatStyle.Flat;
        btnDep.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        btnDep.ForeColor = Color.FromArgb(226, 230, 236);
        btnDep.Location = new Point(154, 0);
        btnDep.Margin = new Padding(0);
        btnDep.Name = "btnDep";
        btnDep.Size = new Size(146, 42);
        btnDep.TabIndex = 1;
        btnDep.Text = "DEPLASMAN";
        btnDep.UseVisualStyleBackColor = false;
        btnDep.Click += btnDep_Click;
        // 
        // pnlDakikaSkor
        // 
        pnlDakikaSkor.Controls.Add(lblDakika);
        pnlDakikaSkor.Controls.Add(txtDakika);
        pnlDakikaSkor.Controls.Add(lblSkor);
        pnlDakikaSkor.Controls.Add(numEv);
        pnlDakikaSkor.Controls.Add(lblTire);
        pnlDakikaSkor.Controls.Add(numDep);
        pnlDakikaSkor.Location = new Point(0, 82);
        pnlDakikaSkor.Margin = new Padding(0, 0, 0, 6);
        pnlDakikaSkor.Name = "pnlDakikaSkor";
        pnlDakikaSkor.Size = new Size(300, 36);
        pnlDakikaSkor.TabIndex = 2;
        // 
        // lblDakika
        // 
        lblDakika.ForeColor = Color.FromArgb(140, 149, 163);
        lblDakika.Location = new Point(0, 7);
        lblDakika.Name = "lblDakika";
        lblDakika.Size = new Size(52, 22);
        lblDakika.TabIndex = 0;
        lblDakika.Text = "Dakika";
        // 
        // txtDakika
        // 
        txtDakika.BackColor = Color.FromArgb(42, 47, 57);
        txtDakika.BorderStyle = BorderStyle.FixedSingle;
        txtDakika.Font = new Font("Segoe UI", 10.5F);
        txtDakika.ForeColor = Color.FromArgb(226, 230, 236);
        txtDakika.Location = new Point(54, 4);
        txtDakika.Name = "txtDakika";
        txtDakika.PlaceholderText = "23";
        txtDakika.Size = new Size(66, 26);
        txtDakika.TabIndex = 2;
        txtDakika.TextChanged += bilgi_Changed;
        // 
        // lblSkor
        // 
        lblSkor.ForeColor = Color.FromArgb(140, 149, 163);
        lblSkor.Location = new Point(136, 7);
        lblSkor.Name = "lblSkor";
        lblSkor.Size = new Size(38, 22);
        lblSkor.TabIndex = 3;
        lblSkor.Text = "Skor";
        // 
        // numEv
        // 
        numEv.BackColor = Color.FromArgb(42, 47, 57);
        numEv.BorderStyle = BorderStyle.FixedSingle;
        numEv.Font = new Font("Segoe UI", 10.5F);
        numEv.ForeColor = Color.FromArgb(226, 230, 236);
        numEv.Location = new Point(176, 4);
        numEv.Maximum = new decimal(new int[] { 30, 0, 0, 0 });
        numEv.Name = "numEv";
        numEv.Size = new Size(50, 26);
        numEv.TabIndex = 3;
        numEv.ValueChanged += bilgi_Changed;
        // 
        // lblTire
        // 
        lblTire.ForeColor = Color.FromArgb(140, 149, 163);
        lblTire.Location = new Point(228, 7);
        lblTire.Name = "lblTire";
        lblTire.Size = new Size(14, 22);
        lblTire.TabIndex = 4;
        lblTire.Text = "-";
        lblTire.TextAlign = ContentAlignment.TopCenter;
        // 
        // numDep
        // 
        numDep.BackColor = Color.FromArgb(42, 47, 57);
        numDep.BorderStyle = BorderStyle.FixedSingle;
        numDep.Font = new Font("Segoe UI", 10.5F);
        numDep.ForeColor = Color.FromArgb(226, 230, 236);
        numDep.Location = new Point(244, 4);
        numDep.Maximum = new decimal(new int[] { 30, 0, 0, 0 });
        numDep.Name = "numDep";
        numDep.Size = new Size(50, 26);
        numDep.TabIndex = 4;
        numDep.ValueChanged += bilgi_Changed;
        // 
        // lblOyuncuBaslik
        // 
        lblOyuncuBaslik.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        lblOyuncuBaslik.ForeColor = Color.FromArgb(140, 149, 163);
        lblOyuncuBaslik.Location = new Point(0, 124);
        lblOyuncuBaslik.Margin = new Padding(0);
        lblOyuncuBaslik.Name = "lblOyuncuBaslik";
        lblOyuncuBaslik.Size = new Size(300, 30);
        lblOyuncuBaslik.TabIndex = 3;
        lblOyuncuBaslik.Text = "OYUNCU  (tıkla ya da numarayı yaz)";
        lblOyuncuBaslik.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // flowOyuncular
        // 
        flowOyuncular.AutoScroll = true;
        flowOyuncular.BackColor = Color.FromArgb(36, 40, 49);
        flowOyuncular.Location = new Point(0, 154);
        flowOyuncular.Margin = new Padding(0, 0, 0, 10);
        flowOyuncular.Name = "flowOyuncular";
        flowOyuncular.Padding = new Padding(4);
        flowOyuncular.Size = new Size(300, 250);
        flowOyuncular.TabIndex = 4;
        // 
        // flowIslem
        // 
        flowIslem.Controls.Add(btnGeriAl);
        flowIslem.Controls.Add(cBtnTemizle);
        flowIslem.Controls.Add(btnOnizle);
        flowIslem.Controls.Add(btnYeni);
        flowIslem.Controls.Add(btnGonder);
        flowIslem.Controls.Add(btnKaldir);
        flowIslem.Location = new Point(0, 414);
        flowIslem.Margin = new Padding(0, 0, 0, 6);
        flowIslem.Name = "flowIslem";
        flowIslem.Size = new Size(300, 162);
        flowIslem.TabIndex = 5;
        // 
        // btnGeriAl
        // 
        btnGeriAl.BackColor = Color.FromArgb(42, 47, 57);
        btnGeriAl.Cursor = Cursors.Hand;
        btnGeriAl.FlatAppearance.BorderColor = Color.FromArgb(58, 64, 77);
        btnGeriAl.FlatStyle = FlatStyle.Flat;
        btnGeriAl.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        btnGeriAl.ForeColor = Color.FromArgb(226, 230, 236);
        btnGeriAl.Location = new Point(0, 0);
        btnGeriAl.Margin = new Padding(0, 0, 8, 8);
        btnGeriAl.Name = "btnGeriAl";
        btnGeriAl.Size = new Size(146, 40);
        btnGeriAl.TabIndex = 0;
        btnGeriAl.Text = "↶  GERİ AL";
        btnGeriAl.UseVisualStyleBackColor = false;
        btnGeriAl.Click += btnGeriAl_Click;
        // 
        // cBtnTemizle
        // 
        cBtnTemizle.BackColor = Color.FromArgb(42, 47, 57);
        cBtnTemizle.Cursor = Cursors.Hand;
        cBtnTemizle.FlatAppearance.BorderColor = Color.FromArgb(58, 64, 77);
        cBtnTemizle.FlatStyle = FlatStyle.Flat;
        cBtnTemizle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        cBtnTemizle.ForeColor = Color.FromArgb(226, 230, 236);
        cBtnTemizle.Location = new Point(154, 0);
        cBtnTemizle.Margin = new Padding(0, 0, 0, 8);
        cBtnTemizle.Name = "cBtnTemizle";
        cBtnTemizle.Size = new Size(146, 40);
        cBtnTemizle.TabIndex = 1;
        cBtnTemizle.Text = "ÇİZİMİ SİL";
        cBtnTemizle.UseVisualStyleBackColor = false;
        cBtnTemizle.Click += cBtnTemizle_Click;
        // 
        // btnOnizle
        // 
        btnOnizle.BackColor = Color.FromArgb(42, 47, 57);
        btnOnizle.Cursor = Cursors.Hand;
        btnOnizle.FlatAppearance.BorderColor = Color.FromArgb(58, 64, 77);
        btnOnizle.FlatStyle = FlatStyle.Flat;
        btnOnizle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        btnOnizle.ForeColor = Color.FromArgb(226, 230, 236);
        btnOnizle.Location = new Point(0, 48);
        btnOnizle.Margin = new Padding(0, 0, 8, 8);
        btnOnizle.Name = "btnOnizle";
        btnOnizle.Size = new Size(146, 40);
        btnOnizle.TabIndex = 2;
        btnOnizle.Text = "▶  ÖNİZLE   F4";
        btnOnizle.UseVisualStyleBackColor = false;
        btnOnizle.Click += btnOnizle_Click;
        // 
        // btnYeni
        // 
        btnYeni.BackColor = Color.FromArgb(42, 47, 57);
        btnYeni.Cursor = Cursors.Hand;
        btnYeni.FlatAppearance.BorderColor = Color.FromArgb(58, 64, 77);
        btnYeni.FlatStyle = FlatStyle.Flat;
        btnYeni.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        btnYeni.ForeColor = Color.FromArgb(226, 230, 236);
        btnYeni.Location = new Point(154, 48);
        btnYeni.Margin = new Padding(0, 0, 0, 8);
        btnYeni.Name = "btnYeni";
        btnYeni.Size = new Size(146, 40);
        btnYeni.TabIndex = 3;
        btnYeni.Text = "+  YENİ GOL";
        btnYeni.UseVisualStyleBackColor = false;
        btnYeni.Click += btnYeni_Click;
        // 
        // btnGonder
        // 
        btnGonder.BackColor = Color.FromArgb(52, 130, 92);
        btnGonder.Cursor = Cursors.Hand;
        btnGonder.FlatAppearance.BorderColor = Color.FromArgb(52, 130, 92);
        btnGonder.FlatAppearance.MouseOverBackColor = Color.FromArgb(62, 144, 104);
        btnGonder.FlatStyle = FlatStyle.Flat;
        btnGonder.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
        btnGonder.ForeColor = Color.FromArgb(226, 230, 236);
        btnGonder.Location = new Point(0, 96);
        btnGonder.Margin = new Padding(0, 0, 0, 8);
        btnGonder.Name = "btnGonder";
        btnGonder.Size = new Size(300, 46);
        btnGonder.TabIndex = 4;
        btnGonder.Text = "VİZ'E GÖNDER VE OYNAT   F5";
        btnGonder.UseVisualStyleBackColor = false;
        btnGonder.Click += btnGonder_Click;
        // 
        // btnKaldir
        // 
        btnKaldir.BackColor = Color.FromArgb(42, 47, 57);
        btnKaldir.Cursor = Cursors.Hand;
        btnKaldir.FlatAppearance.BorderColor = Color.FromArgb(58, 64, 77);
        btnKaldir.FlatStyle = FlatStyle.Flat;
        btnKaldir.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        btnKaldir.ForeColor = Color.FromArgb(226, 230, 236);
        btnKaldir.Location = new Point(0, 150);
        btnKaldir.Margin = new Padding(0);
        btnKaldir.Name = "btnKaldir";
        btnKaldir.Size = new Size(300, 36);
        btnKaldir.TabIndex = 5;
        btnKaldir.Text = "VİZ'DEN KALDIR   F8";
        btnKaldir.UseVisualStyleBackColor = false;
        btnKaldir.Click += btnKaldir_Click;
        // 
        // lblKayitBaslik
        // 
        lblKayitBaslik.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        lblKayitBaslik.ForeColor = Color.FromArgb(140, 149, 163);
        lblKayitBaslik.Location = new Point(0, 588);
        lblKayitBaslik.Margin = new Padding(0, 6, 0, 0);
        lblKayitBaslik.Name = "lblKayitBaslik";
        lblKayitBaslik.Size = new Size(300, 28);
        lblKayitBaslik.TabIndex = 6;
        lblKayitBaslik.Text = "BU MAÇIN KAYITLI GOLLERİ";
        lblKayitBaslik.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lstKayitlar
        // 
        lstKayitlar.BackColor = Color.FromArgb(36, 40, 49);
        lstKayitlar.BorderStyle = BorderStyle.None;
        lstKayitlar.Font = new Font("Segoe UI", 10F);
        lstKayitlar.ForeColor = Color.FromArgb(226, 230, 236);
        lstKayitlar.IntegralHeight = false;
        lstKayitlar.Location = new Point(0, 616);
        lstKayitlar.Margin = new Padding(0, 0, 0, 6);
        lstKayitlar.Name = "lstKayitlar";
        lstKayitlar.Size = new Size(300, 110);
        lstKayitlar.TabIndex = 6;
        lstKayitlar.SelectedIndexChanged += lstKayitlar_SelectedIndexChanged;
        // 
        // btnSil
        // 
        btnSil.BackColor = Color.FromArgb(42, 47, 57);
        btnSil.Cursor = Cursors.Hand;
        btnSil.FlatAppearance.BorderColor = Color.FromArgb(58, 64, 77);
        btnSil.FlatStyle = FlatStyle.Flat;
        btnSil.Font = new Font("Segoe UI", 9F);
        btnSil.ForeColor = Color.FromArgb(196, 202, 212);
        btnSil.Location = new Point(0, 732);
        btnSil.Margin = new Padding(0);
        btnSil.Name = "btnSil";
        btnSil.Size = new Size(300, 32);
        btnSil.TabIndex = 7;
        btnSil.Text = "Seçili kaydı sil";
        btnSil.UseVisualStyleBackColor = false;
        btnSil.Click += btnSil_Click;
        // 
        // cPnlSol
        // 
        cPnlSol.BackColor = Color.FromArgb(31, 35, 43);
        cPnlSol.Controls.Add(lstMaclar);
        cPnlSol.Controls.Add(cPnlSolBosluk);
        cPnlSol.Controls.Add(cPnlAra);
        cPnlSol.Controls.Add(pnlGun);
        cPnlSol.Controls.Add(cLblMacBaslik);
        cPnlSol.Dock = DockStyle.Left;
        cPnlSol.Location = new Point(0, 0);
        cPnlSol.Name = "cPnlSol";
        cPnlSol.Padding = new Padding(16, 12, 16, 16);
        cPnlSol.Size = new Size(330, 727);
        cPnlSol.TabIndex = 0;
        // 
        // lstMaclar
        // 
        lstMaclar.BackColor = Color.FromArgb(36, 40, 49);
        lstMaclar.BorderStyle = BorderStyle.None;
        lstMaclar.Dock = DockStyle.Fill;
        lstMaclar.DrawMode = DrawMode.OwnerDrawFixed;
        lstMaclar.Font = new Font("Segoe UI", 10F);
        lstMaclar.ForeColor = Color.FromArgb(226, 230, 236);
        lstMaclar.IntegralHeight = false;
        lstMaclar.ItemHeight = 46;
        lstMaclar.Location = new Point(16, 118);
        lstMaclar.Name = "lstMaclar";
        lstMaclar.Size = new Size(298, 593);
        lstMaclar.TabIndex = 2;
        lstMaclar.DrawItem += lstMaclar_DrawItem;
        lstMaclar.SelectedIndexChanged += lstMaclar_SelectedIndexChanged;
        // 
        // cPnlSolBosluk
        // 
        cPnlSolBosluk.Dock = DockStyle.Top;
        cPnlSolBosluk.Location = new Point(16, 108);
        cPnlSolBosluk.Name = "cPnlSolBosluk";
        cPnlSolBosluk.Size = new Size(298, 10);
        cPnlSolBosluk.TabIndex = 3;
        // 
        // cPnlAra
        // 
        cPnlAra.Controls.Add(cTxtAra);
        cPnlAra.Dock = DockStyle.Top;
        cPnlAra.Location = new Point(16, 72);
        cPnlAra.Name = "cPnlAra";
        cPnlAra.Padding = new Padding(0, 8, 0, 0);
        cPnlAra.Size = new Size(298, 36);
        cPnlAra.TabIndex = 4;
        // 
        // cTxtAra
        // 
        cTxtAra.BackColor = Color.FromArgb(42, 47, 57);
        cTxtAra.BorderStyle = BorderStyle.FixedSingle;
        cTxtAra.Dock = DockStyle.Fill;
        cTxtAra.Font = new Font("Segoe UI", 10F);
        cTxtAra.ForeColor = Color.FromArgb(226, 230, 236);
        cTxtAra.Location = new Point(0, 8);
        cTxtAra.Name = "cTxtAra";
        cTxtAra.PlaceholderText = "Takım ara…";
        cTxtAra.Size = new Size(298, 25);
        cTxtAra.TabIndex = 1;
        cTxtAra.TextChanged += cTxtAra_TextChanged;
        // 
        // pnlGun
        // 
        pnlGun.Controls.Add(dtGun);
        pnlGun.Dock = DockStyle.Top;
        pnlGun.Location = new Point(16, 42);
        pnlGun.Name = "pnlGun";
        pnlGun.Size = new Size(298, 30);
        pnlGun.TabIndex = 5;
        // 
        // dtGun
        // 
        dtGun.Dock = DockStyle.Fill;
        dtGun.Location = new Point(0, 0);
        dtGun.Name = "dtGun";
        dtGun.Size = new Size(298, 25);
        dtGun.TabIndex = 0;
        dtGun.ValueChanged += dtGun_ValueChanged;
        // 
        // cLblMacBaslik
        // 
        cLblMacBaslik.Dock = DockStyle.Top;
        cLblMacBaslik.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        cLblMacBaslik.ForeColor = Color.FromArgb(140, 149, 163);
        cLblMacBaslik.Location = new Point(16, 12);
        cLblMacBaslik.Name = "cLblMacBaslik";
        cLblMacBaslik.Size = new Size(298, 30);
        cLblMacBaslik.TabIndex = 6;
        cLblMacBaslik.Text = "FİKSTÜR  ·  seçilen günden itibaren";
        cLblMacBaslik.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // tik
        // 
        tik.Interval = 15;
        tik.Tick += tik_Tick;
        // 
        // durumTik
        // 
        durumTik.Interval = 2000;
        durumTik.Tick += durumTik_Tick;
        // 
        // gorunum
        // 
        gorunum.OyuncuFontu = new Font("Segoe UI", 9.25F, FontStyle.Bold);
        gorunum.YedekFontu = new Font("Segoe UI", 9.25F);
        // 
        // AnaForm
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(24, 27, 33);
        ClientSize = new Size(1484, 861);
        Controls.Add(tabSekmeler);
        Controls.Add(pnlSekme);
        Controls.Add(pnlUst);
        Font = new Font("Segoe UI", 9.75F);
        ForeColor = Color.FromArgb(226, 230, 236);
        KeyPreview = true;
        MinimumSize = new Size(1280, 760);
        Name = "AnaForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "SPOR ANALİZ · Viz Kumanda";
        FormClosing += AnaForm_FormClosing;
        Shown += AnaForm_Shown;
        pnlUst.ResumeLayout(false);
        pnlUst.PerformLayout();
        pnlSekme.ResumeLayout(false);
        tabSekmeler.ResumeLayout(false);
        tabSim.ResumeLayout(false);
        pnlOrta.ResumeLayout(false);
        pnlAkis.ResumeLayout(false);
        pnlTabela.ResumeLayout(false);
        pnlSag.ResumeLayout(false);
        flowAdimlar.ResumeLayout(false);
        flowHiz.ResumeLayout(false);
        pnlSol.ResumeLayout(false);
        pnlAra.ResumeLayout(false);
        pnlAra.PerformLayout();
        pnlTarih.ResumeLayout(false);
        pnlHafta.ResumeLayout(false);
        tabCanli.ResumeLayout(false);
        cPnlOrta.ResumeLayout(false);
        cPnlSag.ResumeLayout(false);
        flowSag.ResumeLayout(false);
        flowTakim.ResumeLayout(false);
        pnlDakikaSkor.ResumeLayout(false);
        pnlDakikaSkor.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)numEv).EndInit();
        ((System.ComponentModel.ISupportInitialize)numDep).EndInit();
        flowIslem.ResumeLayout(false);
        cPnlSol.ResumeLayout(false);
        cPnlAra.ResumeLayout(false);
        cPnlAra.PerformLayout();
        pnlGun.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private Panel pnlUst;
    private Panel pnlSekme;
    private Button btnSekmeSim;
    private Button btnSekmeCanli;
    private TabControl tabSekmeler;
    private TabPage tabSim;
    private TabPage tabCanli;
    private Panel cPnlSol;
    private ListBox lstMaclar;
    private Panel cPnlSolBosluk;
    private Panel cPnlAra;
    private TextBox cTxtAra;
    private Panel pnlGun;
    private DateTimePicker dtGun;
    private Label cLblMacBaslik;
    private Panel cPnlSag;
    private FlowLayoutPanel flowSag;
    private Label lblGolBaslik;
    private FlowLayoutPanel flowTakim;
    private Button btnEv;
    private Button btnDep;
    private Panel pnlDakikaSkor;
    private Label lblDakika;
    private TextBox txtDakika;
    private Label lblSkor;
    private NumericUpDown numEv;
    private Label lblTire;
    private NumericUpDown numDep;
    private Label lblOyuncuBaslik;
    private FlowLayoutPanel flowOyuncular;
    private FlowLayoutPanel flowIslem;
    private Button btnGeriAl;
    private Button cBtnTemizle;
    private Button btnOnizle;
    private Button btnYeni;
    private Button btnGonder;
    private Button btnKaldir;
    private Label lblKayitBaslik;
    private ListBox lstKayitlar;
    private Button btnSil;
    private Panel cPnlOrta;
    private AtakCizimAlani cizim;
    private Label cLblDurum;
    private Label cLblBaslik;
    private Label lblBaslik;
    private Label lblAltBaslik;
    private Label lblVizEtiket;
    private TextBox txtHost;
    private Label lblIkiNokta;
    private TextBox txtPort;
    private Button btnBaglan;
    private Label lblViz;
    private Label lblVeri;
    private Panel pnlSol;
    private ListView lstMac;
    private ColumnHeader colTarih;
    private ColumnHeader colMac;
    private ColumnHeader colSkor;
    private Panel pnlSolBosluk;
    private Panel pnlAra;
    private TextBox txtAra;
    private Panel pnlTarih;
    private DateTimePicker dtBas;
    private DateTimePicker dtBit;
    private Panel pnlHafta;
    private ComboBox cmbHafta;
    private ComboBox cmbAsama;
    private ComboBox cmbTurnuva;
    private Label lblMacBaslik;
    private Panel pnlSag;
    private FlowLayoutPanel flowAdimlar;
    private Label lblAdimBaslik;
    private Button btnAnaliz;
    private Label lblBilgi;
    private Button btnYukle;
    private Button btnOynat;
    private Button btnDuraklat;
    private Button btnBastan;
    private FlowLayoutPanel flowHiz;
    private Label lblHiz;
    private Button btnHiz1;
    private Button btnHiz2;
    private Button btnHiz4;
    private Button btnTemizle;
    private Label lblKisayol;
    private Panel pnlOrta;
    private SahaOnizleme saha;
    private Panel pnlAkis;
    private ListBox lstAkis;
    private Label lblAkisBaslik;
    private Panel pnlOrtaBosluk;
    private Panel pnlTabela;
    private Label lblTabela;
    private System.Windows.Forms.Timer tik;
    private System.Windows.Forms.Timer durumTik;
    private KumandaGorunumu gorunum;
}
