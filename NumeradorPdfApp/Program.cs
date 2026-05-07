namespace NumeradorPdfApp;

static class Program
{
    [STAThread]
    static void Main()
    {
        PdfSharp.Fonts.GlobalFontSettings.UseWindowsFontsUnderWindows = true;
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }
}
