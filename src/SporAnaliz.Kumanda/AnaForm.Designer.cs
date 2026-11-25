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
        pnlOrta = new Panel();
        saha = new SahaOnizleme();
        pnlAkis = new Panel();
        lstAkis = new ListBox();
        lblAkisBaslik = new Label();
        pnlOrtaBosluk = new Panel();
        pnlTabela = new Panel();
        lblTabela = new Label();
        tik = new System.Windows.Forms.Timer(components);
        durumTik = new System.Windows.Forms.Timer(components);
        pnlUst.SuspendLayout();
        pnlSol.SuspendLayout();
        pnlAra.SuspendLayout();
        pnlTarih.SuspendLayout();
        pnlHafta.SuspendLayout();
        pnlSag.SuspendLayout();
        flowAdimlar.SuspendLayout();
        flowHiz.SuspendLayout();
        pnlOrta.SuspendLayout();
        pnlAkis.SuspendLayout();
        pnlTabela.SuspendLayout();
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
        lblBaslik.Text = "SPOR ANALİZ";
        //
        // lblAltBaslik
        //
        lblAltBaslik.AutoSize = true;
        lblAltBaslik.Font = new Font("Segoe UI", 10F);
        lblAltBaslik.ForeColor = Color.FromArgb(140, 149, 163);
        lblAltBaslik.Location = new Point(146, 19);
        lblAltBaslik.Name = "lblAltBaslik";
        lblAltBaslik.Text = "Viz Kumanda";
        //
        // lblVizEtiket
        //
        lblVizEtiket.AutoSize = true;
        lblVizEtiket.ForeColor = Color.FromArgb(140, 149, 163);
        lblVizEtiket.Location = new Point(330, 19);
        lblVizEtiket.Name = "lblVizEtiket";
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
        lblViz.Text = "● Bağlı değil";
        //
        // lblVeri
        //
        lblVeri.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        lblVeri.ForeColor = Color.FromArgb(140, 149, 163);
        lblVeri.Location = new Point(1188, 18);
        lblVeri.Name = "lblVeri";
        lblVeri.Size = new Size(278, 22);
        lblVeri.Text = "Veri yükleniyor…";
        lblVeri.TextAlign = ContentAlignment.MiddleRight;
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
        pnlSol.Location = new Point(0, 58);
        pnlSol.Name = "pnlSol";
        pnlSol.Padding = new Padding(16, 12, 16, 16);
        pnlSol.Size = new Size(370, 803);
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
        lstMac.HideSelection = false;
        lstMac.MultiSelect = false;
        lstMac.Name = "lstMac";
        lstMac.OwnerDraw = true;
        lstMac.ShowItemToolTips = true;
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
        pnlSolBosluk.Name = "pnlSolBosluk";
        pnlSolBosluk.Size = new Size(338, 10);
        //
        // pnlAra
        //
        pnlAra.Controls.Add(txtAra);
        pnlAra.Dock = DockStyle.Top;
        pnlAra.Name = "pnlAra";
        pnlAra.Padding = new Padding(0, 8, 0, 0);
        pnlAra.Size = new Size(338, 36);
        //
        // txtAra
        //
        txtAra.BackColor = Color.FromArgb(42, 47, 57);
        txtAra.BorderStyle = BorderStyle.FixedSingle;
        txtAra.Dock = DockStyle.Fill;
        txtAra.Font = new Font("Segoe UI", 10F);
        txtAra.ForeColor = Color.FromArgb(226, 230, 236);
        txtAra.Name = "txtAra";
        txtAra.PlaceholderText = "Takım ara…";
        txtAra.TabIndex = 5;
        txtAra.TextChanged += txtAra_TextChanged;
        //
        // pnlTarih
        //
        pnlTarih.Controls.Add(dtBas);
        pnlTarih.Controls.Add(dtBit);
        pnlTarih.Dock = DockStyle.Top;
        pnlTarih.Name = "pnlTarih";
        pnlTarih.Size = new Size(338, 42);
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
        pnlHafta.Name = "pnlHafta";
        pnlHafta.Padding = new Padding(0, 8, 0, 0);
        pnlHafta.Size = new Size(338, 74);
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
        cmbHafta.Name = "cmbHafta";
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
        cmbAsama.Name = "cmbAsama";
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
        cmbTurnuva.Name = "cmbTurnuva";
        cmbTurnuva.TabIndex = 0;
        cmbTurnuva.SelectedIndexChanged += cmbTurnuva_SelectedIndexChanged;
        //
        // lblMacBaslik
        //
        lblMacBaslik.Dock = DockStyle.Top;
        lblMacBaslik.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        lblMacBaslik.ForeColor = Color.FromArgb(140, 149, 163);
        lblMacBaslik.Name = "lblMacBaslik";
        lblMacBaslik.Size = new Size(338, 30);
        lblMacBaslik.Text = "MAÇ SEÇİMİ";
        lblMacBaslik.TextAlign = ContentAlignment.MiddleLeft;
        //
        // pnlSag
        //
        pnlSag.BackColor = Color.FromArgb(31, 35, 43);
        pnlSag.Controls.Add(flowAdimlar);
        pnlSag.Dock = DockStyle.Right;
        pnlSag.Location = new Point(1170, 58);
        pnlSag.Name = "pnlSag";
        pnlSag.Padding = new Padding(16, 12, 12, 16);
        pnlSag.Size = new Size(314, 803);
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
        flowAdimlar.Name = "flowAdimlar";
        flowAdimlar.WrapContents = false;
        //
        // lblAdimBaslik
        //
        lblAdimBaslik.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        lblAdimBaslik.ForeColor = Color.FromArgb(140, 149, 163);
        lblAdimBaslik.Margin = new Padding(0);
        lblAdimBaslik.Name = "lblAdimBaslik";
        lblAdimBaslik.Size = new Size(280, 30);
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
        lblBilgi.Margin = new Padding(0, 0, 0, 14);
        lblBilgi.Name = "lblBilgi";
        lblBilgi.Padding = new Padding(10, 8, 10, 8);
        lblBilgi.Size = new Size(280, 172);
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
        flowHiz.Margin = new Padding(0, 4, 0, 10);
        flowHiz.Name = "flowHiz";
        flowHiz.Size = new Size(280, 40);
        flowHiz.WrapContents = false;
        //
        // lblHiz
        //
        lblHiz.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        lblHiz.ForeColor = Color.FromArgb(140, 149, 163);
        lblHiz.Margin = new Padding(0, 0, 10, 0);
        lblHiz.Name = "lblHiz";
        lblHiz.Size = new Size(40, 36);
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
        lblKisayol.Margin = new Padding(0);
        lblKisayol.Name = "lblKisayol";
        lblKisayol.Size = new Size(280, 90);
        lblKisayol.Text = "Kısayollar\r\nF2  Analiz et        F3  Viz'e yükle\r\nF5  Oynat              F6  Duraklat\r\nF7  Baştan            F8  Temizle";
        //
        // pnlOrta
        //
        pnlOrta.BackColor = Color.FromArgb(24, 27, 33);
        pnlOrta.Controls.Add(saha);
        pnlOrta.Controls.Add(pnlAkis);
        pnlOrta.Controls.Add(pnlOrtaBosluk);
        pnlOrta.Controls.Add(pnlTabela);
        pnlOrta.Dock = DockStyle.Fill;
        pnlOrta.Location = new Point(370, 58);
        pnlOrta.Name = "pnlOrta";
        pnlOrta.Padding = new Padding(16);
        pnlOrta.Size = new Size(800, 803);
        pnlOrta.TabIndex = 3;
        //
        // saha
        //
        saha.Dock = DockStyle.Fill;
        saha.Name = "saha";
        saha.TabStop = false;
        //
        // pnlAkis
        //
        pnlAkis.Controls.Add(lstAkis);
        pnlAkis.Controls.Add(lblAkisBaslik);
        pnlAkis.Dock = DockStyle.Bottom;
        pnlAkis.Name = "pnlAkis";
        pnlAkis.Padding = new Padding(0, 12, 0, 0);
        pnlAkis.Size = new Size(768, 200);
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
        lstAkis.Name = "lstAkis";
        lstAkis.TabStop = false;
        lstAkis.DrawItem += lstAkis_DrawItem;
        //
        // lblAkisBaslik
        //
        lblAkisBaslik.BackColor = Color.FromArgb(31, 35, 43);
        lblAkisBaslik.Dock = DockStyle.Top;
        lblAkisBaslik.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        lblAkisBaslik.ForeColor = Color.FromArgb(140, 149, 163);
        lblAkisBaslik.Name = "lblAkisBaslik";
        lblAkisBaslik.Padding = new Padding(10, 0, 0, 0);
        lblAkisBaslik.Size = new Size(768, 30);
        lblAkisBaslik.Text = "MAÇ AKIŞI";
        lblAkisBaslik.TextAlign = ContentAlignment.MiddleLeft;
        //
        // pnlOrtaBosluk
        //
        pnlOrtaBosluk.Dock = DockStyle.Top;
        pnlOrtaBosluk.Name = "pnlOrtaBosluk";
        pnlOrtaBosluk.Size = new Size(768, 12);
        //
        // pnlTabela
        //
        pnlTabela.BackColor = Color.FromArgb(31, 35, 43);
        pnlTabela.Controls.Add(lblTabela);
        pnlTabela.Dock = DockStyle.Top;
        pnlTabela.Name = "pnlTabela";
        pnlTabela.Size = new Size(768, 64);
        //
        // lblTabela
        //
        lblTabela.Dock = DockStyle.Fill;
        lblTabela.Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold);
        lblTabela.ForeColor = Color.FromArgb(226, 230, 236);
        lblTabela.Name = "lblTabela";
        lblTabela.Text = "—";
        lblTabela.TextAlign = ContentAlignment.MiddleCenter;
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
        // AnaForm
        //
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(24, 27, 33);
        ClientSize = new Size(1484, 861);
        Controls.Add(pnlOrta);
        Controls.Add(pnlSag);
        Controls.Add(pnlSol);
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
        pnlSol.ResumeLayout(false);
        pnlAra.ResumeLayout(false);
        pnlAra.PerformLayout();
        pnlTarih.ResumeLayout(false);
        pnlHafta.ResumeLayout(false);
        pnlSag.ResumeLayout(false);
        flowAdimlar.ResumeLayout(false);
        flowHiz.ResumeLayout(false);
        pnlOrta.ResumeLayout(false);
        pnlAkis.ResumeLayout(false);
        pnlTabela.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion

    private Panel pnlUst;
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
}
