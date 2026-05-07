namespace NumeradorPdfApp;

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        PdfSharp.Fonts.GlobalFontSettings.UseWindowsFontsUnderWindows = true;

        if (args.Length > 0)
        {
            return RunCommandLine(args);
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
        return 0;
    }

    private static int RunCommandLine(string[] args)
    {
        if (args.Length < 3 || !args[0].Equals("--number-folder", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("Uso: NumeradorPdfApp.exe --number-folder <carpeta-origen> <carpeta-destino> [texto-prefijo]");
            return 2;
        }

        try
        {
            var sourceFolder = Path.GetFullPath(args[1]);
            var destinationFolder = Path.GetFullPath(args[2]);
            var prefix = args.Length >= 4 ? args[3] : string.Empty;

            var files = Directory.EnumerateFiles(sourceFolder, "*.pdf", SearchOption.AllDirectories)
                .Where(file => !IsInFolder(file, destinationFolder))
                .OrderBy(file => Path.GetFileName(file), NaturalStringComparer.Instance)
                .ThenBy(file => file, NaturalStringComparer.Instance)
                .ToArray();

            if (files.Length == 0)
            {
                Console.Error.WriteLine("No se han encontrado PDFs de origen fuera de la carpeta destino.");
                return 3;
            }

            var settings = new NumberingSettings(files, destinationFolder, prefix);
            var plan = NumberingPlanner.Build(settings, CancellationToken.None);
            var progress = new Progress<NumberingProgress>(item => Console.WriteLine($"{item.Percent}% - {item.Message}"));
            var result = PdfNumberer.Execute(plan, progress, CancellationToken.None);
            Console.WriteLine($"Proceso terminado. {result.Documents.Count} PDF(s), {result.TotalPages} página(s).");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static bool IsInFolder(string filePath, string folderPath)
    {
        var fileFullPath = Path.GetFullPath(filePath);
        var folderFullPath = Path.GetFullPath(folderPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        return fileFullPath.StartsWith(folderFullPath, StringComparison.OrdinalIgnoreCase);
    }
}
