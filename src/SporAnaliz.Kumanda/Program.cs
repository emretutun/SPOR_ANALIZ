namespace SporAnaliz.Kumanda;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        AnaForm form;
        try
        {
            var ayar = KumandaAyarlari.Yukle();
            if (!ayar.Veri.UseFeed)
                throw new InvalidOperationException("Kumanda yalnızca maç veritabanıyla çalışır: appsettings.json içinde Data:Source \"feed\" olmalı.");
            form = new AnaForm(ayar);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "SPOR ANALİZ · Viz Kumanda", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        Application.Run(form);
    }
}
