using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace NumeradorPdfApp;

public partial class Form1 : Form
{
    private const string AppVersion = "1.0";

    private readonly ListBox filesList = new();
    private readonly TextBox prefixInput = new();
    private readonly TextBox partMarkerInput = new();
    private readonly TextBox destinationInput = new();
    private readonly ProgressBar progressBar = new();
    private readonly Label statusLabel = new();
    private readonly Button numberButton = new();
    private readonly Button addFilesButton = new();
    private readonly Button addFolderButton = new();
    private readonly Button removeFileButton = new();
    private readonly Button clearFilesButton = new();
    private readonly Button browseDestinationButton = new();

    private NumberingPlan? lastPlan;

    public Form1()
    {
        InitializeComponent();
        BuildUi();
        WireEvents();
    }

    private void BuildUi()
    {
        Text = $"Numerador de PDF v{AppVersion}";
        MinimumSize = new Size(980, 520);
        StartPosition = FormStartPosition.CenterScreen;
        AllowDrop = true;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(14),
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        root.Controls.Add(BuildFilesPanel(), 0, 0);
        root.Controls.Add(BuildParametersPanel(), 0, 1);
        root.Controls.Add(BuildProgressPanel(), 0, 2);
        root.Controls.Add(BuildActionsPanel(), 0, 3);
    }

    private Control BuildFilesPanel()
    {
        var group = new GroupBox
        {
            Text = "PDFs a numerar",
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        group.Controls.Add(layout);

        filesList.Dock = DockStyle.Fill;
        filesList.AllowDrop = true;
        filesList.HorizontalScrollbar = true;
        layout.Controls.Add(filesList, 0, 0);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
        };

        addFilesButton.Text = "Añadir PDFs";
        addFolderButton.Text = "Añadir carpeta";
        removeFileButton.Text = "Quitar";
        clearFilesButton.Text = "Limpiar";

        foreach (var button in new[] { addFilesButton, addFolderButton, removeFileButton, clearFilesButton })
        {
            button.Width = 124;
            button.Height = 32;
            buttons.Controls.Add(button);
        }

        layout.Controls.Add(buttons, 1, 0);
        return group;
    }

    private Control BuildParametersPanel()
    {
        var group = new GroupBox
        {
            Text = "Parámetros",
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 4,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        group.Controls.Add(layout);

        layout.Controls.Add(new Label { Text = "Texto antes del número", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        layout.Controls.Add(new Label { Text = "Nomenclatura serie", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 1, 0);

        prefixInput.Dock = DockStyle.Fill;
        prefixInput.PlaceholderText = "Ej.: Página";
        layout.Controls.Add(prefixInput, 0, 1);

        partMarkerInput.Text = "_part";
        partMarkerInput.Dock = DockStyle.Fill;
        layout.Controls.Add(partMarkerInput, 1, 1);

        layout.Controls.Add(new Label { Text = "Ruta destino", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 2);

        var destinationPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
        };
        destinationPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        destinationPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        destinationInput.Dock = DockStyle.Fill;
        browseDestinationButton.Text = "Examinar";
        browseDestinationButton.Dock = DockStyle.Fill;
        destinationPanel.Controls.Add(destinationInput, 0, 0);
        destinationPanel.Controls.Add(browseDestinationButton, 1, 0);
        layout.SetColumnSpan(destinationPanel, 3);
        layout.Controls.Add(destinationPanel, 0, 3);

        return group;
    }

    private Control BuildProgressPanel()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));

        progressBar.Dock = DockStyle.Fill;
        progressBar.Minimum = 0;
        progressBar.Maximum = 100;
        progressBar.Margin = new Padding(0, 8, 12, 8);

        statusLabel.Text = "Listo.";
        statusLabel.Dock = DockStyle.Fill;
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;

        layout.Controls.Add(progressBar, 0, 0);
        layout.Controls.Add(statusLabel, 1, 0);
        return layout;
    }

    private Control BuildActionsPanel()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
        };

        numberButton.Text = "Numerar PDFs";
        numberButton.Width = 130;
        numberButton.Height = 34;

        panel.Controls.Add(numberButton);
        return panel;
    }

    private void WireEvents()
    {
        addFilesButton.Click += (_, _) => AddFilesFromDialog();
        addFolderButton.Click += (_, _) => AddFolderFromDialog();
        removeFileButton.Click += (_, _) => RemoveSelectedFiles();
        clearFilesButton.Click += (_, _) => ClearFiles();
        browseDestinationButton.Click += (_, _) => BrowseDestination();
        numberButton.Click += async (_, _) => await NumberAsync();

        DragEnter += OnPdfDragEnter;
        DragDrop += OnPdfDragDrop;
        filesList.DragEnter += OnPdfDragEnter;
        filesList.DragDrop += OnPdfDragDrop;

        prefixInput.TextChanged += (_, _) => lastPlan = null;
        partMarkerInput.TextChanged += (_, _) => lastPlan = null;
        destinationInput.TextChanged += (_, _) => lastPlan = null;
    }

    private void AddFilesFromDialog()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Seleccionar PDFs",
            Filter = "PDF (*.pdf)|*.pdf",
            Multiselect = true,
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            AddFiles(dialog.FileNames);
        }
    }

    private void AddFolderFromDialog()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Seleccionar carpeta con PDFs",
            UseDescriptionForTitle = true,
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            AddFiles(EnumeratePdfFiles(dialog.SelectedPath));
            if (string.IsNullOrWhiteSpace(destinationInput.Text))
            {
                destinationInput.Text = dialog.SelectedPath;
            }
        }
    }

    private void AddFiles(IEnumerable<string> paths)
    {
        var existing = filesList.Items.Cast<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);
        var firstFolder = string.Empty;

        foreach (var path in paths.SelectMany(ExpandPath).Where(File.Exists))
        {
            if (!Path.GetExtension(path).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (existing.Add(path))
            {
                filesList.Items.Add(path);
                firstFolder = string.IsNullOrEmpty(firstFolder) ? Path.GetDirectoryName(path) ?? string.Empty : firstFolder;
            }
        }

        if (string.IsNullOrWhiteSpace(destinationInput.Text) && !string.IsNullOrWhiteSpace(firstFolder))
        {
            destinationInput.Text = firstFolder;
        }

        lastPlan = null;
    }

    private static IEnumerable<string> ExpandPath(string path)
    {
        if (File.Exists(path))
        {
            yield return path;
            yield break;
        }

        if (!Directory.Exists(path))
        {
            yield break;
        }

        foreach (var file in EnumeratePdfFiles(path))
        {
            yield return file;
        }
    }

    private static IEnumerable<string> EnumeratePdfFiles(string folder)
    {
        return Directory.EnumerateFiles(folder, "*.pdf", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.CurrentCultureIgnoreCase);
    }

    private void RemoveSelectedFiles()
    {
        while (filesList.SelectedIndices.Count > 0)
        {
            filesList.Items.RemoveAt(filesList.SelectedIndices[0]);
        }

        lastPlan = null;
    }

    private void ClearFiles()
    {
        filesList.Items.Clear();
        lastPlan = null;
    }

    private void BrowseDestination()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Seleccionar carpeta destino",
            UseDescriptionForTitle = true,
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            destinationInput.Text = dialog.SelectedPath;
        }
    }

    private static void OnPdfDragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void OnPdfDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths)
        {
            AddFiles(paths);
        }
    }

    private async Task NumberAsync()
    {
        if (!TryReadSettings(out var settings))
        {
            return;
        }

        var plan = lastPlan;
        if (plan is null || !SettingsMatch(plan.Settings, settings))
        {
            await RunBusyAsync("Preparando numeración...", async token =>
            {
                progressBar.Style = ProgressBarStyle.Marquee;
                plan = await Task.Run(() => NumberingPlanner.Build(settings, token), token);
                lastPlan = plan;
            });
        }

        if (plan is null)
        {
            return;
        }

        await RunBusyAsync("Numerando PDFs...", async token =>
        {
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = 0;
            var progress = new Progress<NumberingProgress>(UpdateNumberingProgress);
            var result = await Task.Run(() => PdfNumberer.Execute(plan, progress, token), token);

            using var dialog = new PreviewDialog("Proceso terminado", NumberingResultFormatter.Format(result), confirmMode: false);
            dialog.ShowDialog(this);
        });
    }

    private bool TryReadSettings(out NumberingSettings settings)
    {
        settings = new NumberingSettings([], string.Empty, string.Empty, string.Empty);

        var files = filesList.Items.Cast<string>().ToArray();
        if (files.Length == 0)
        {
            ShowValidation("Debes indicar al menos un PDF.");
            return false;
        }

        var missingFile = files.FirstOrDefault(file => !File.Exists(file));
        if (missingFile is not null)
        {
            ShowValidation($"No existe el fichero:\n{missingFile}");
            return false;
        }

        var destination = destinationInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(destination))
        {
            ShowValidation("Debes indicar la ruta destino.");
            return false;
        }

        var partMarker = partMarkerInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(partMarker))
        {
            ShowValidation("Debes indicar la nomenclatura de serie. Por defecto: _part.");
            return false;
        }

        try
        {
            _ = Path.GetFullPath(destination);
        }
        catch (Exception ex)
        {
            ShowValidation($"La ruta destino no es válida:\n{ex.Message}");
            return false;
        }

        settings = new NumberingSettings(files, destination, prefixInput.Text.Trim(), partMarker);
        return true;
    }

    private static bool SettingsMatch(NumberingSettings left, NumberingSettings right)
    {
        return string.Equals(left.DestinationFolder, right.DestinationFolder, StringComparison.OrdinalIgnoreCase)
            && left.TextPrefix == right.TextPrefix
            && left.PartMarker == right.PartMarker
            && left.Files.SequenceEqual(right.Files, StringComparer.OrdinalIgnoreCase);
    }

    private void ShowValidation(string message)
    {
        MessageBox.Show(this, message, "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private async Task RunBusyAsync(string message, Func<CancellationToken, Task> work)
    {
        SetBusy(true, message);
        using var cancellation = new CancellationTokenSource();
        try
        {
            await work(cancellation.Token);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            statusLabel.Text = "Error.";
        }
        finally
        {
            progressBar.Style = ProgressBarStyle.Continuous;
            SetBusy(false, statusLabel.Text);
        }
    }

    private void SetBusy(bool busy, string message)
    {
        numberButton.Enabled = !busy;
        addFilesButton.Enabled = !busy;
        addFolderButton.Enabled = !busy;
        removeFileButton.Enabled = !busy;
        clearFilesButton.Enabled = !busy;
        browseDestinationButton.Enabled = !busy;
        prefixInput.Enabled = !busy;
        partMarkerInput.Enabled = !busy;
        destinationInput.Enabled = !busy;
        filesList.Enabled = !busy;
        statusLabel.Text = message;
    }

    private void UpdateNumberingProgress(NumberingProgress progress)
    {
        progressBar.Value = Math.Clamp(progress.Percent, progressBar.Minimum, progressBar.Maximum);
        statusLabel.Text = progress.EstimatedRemaining is null
            ? progress.Message
            : $"{progress.Message} | restante aprox. {FormatDuration(progress.EstimatedRemaining.Value)}";
    }

    private static string FormatDuration(TimeSpan value)
    {
        if (value.TotalHours >= 1)
        {
            return $"{(int)value.TotalHours}h {value.Minutes}m";
        }

        if (value.TotalMinutes >= 1)
        {
            return $"{(int)value.TotalMinutes}m {value.Seconds}s";
        }

        return $"{Math.Max(1, value.Seconds)}s";
    }
}

internal sealed class PreviewDialog : Form
{
    public PreviewDialog(string title, string content, bool confirmMode)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = true;
        Width = 980;
        Height = 720;
        MinimumSize = new Size(760, 420);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12),
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        Controls.Add(root);

        var textBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Font = new Font("Consolas", 10),
            Text = content,
        };
        root.Controls.Add(textBox, 0, 0);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
        };
        root.Controls.Add(buttons, 0, 1);

        var primaryButton = new Button
        {
            Text = confirmMode ? "Empezar" : "Cerrar",
            DialogResult = DialogResult.OK,
            Width = 110,
            Height = 32,
        };
        buttons.Controls.Add(primaryButton);
        AcceptButton = primaryButton;

        if (confirmMode)
        {
            var cancelButton = new Button
            {
                Text = "Cancelar",
                DialogResult = DialogResult.Cancel,
                Width = 110,
                Height = 32,
            };
            buttons.Controls.Add(cancelButton);
            CancelButton = cancelButton;
        }
    }
}

internal sealed record NumberingSettings(
    IReadOnlyList<string> Files,
    string DestinationFolder,
    string TextPrefix,
    string PartMarker);

internal sealed record NumberingPlan(NumberingSettings Settings, IReadOnlyList<DocumentNumberingPlan> Documents)
{
    public int TotalPages => Documents.Sum(document => document.PageCount);
}

internal sealed record DocumentNumberingPlan(
    string SourcePath,
    string OutputPath,
    string DisplayGroup,
    int? PartNumber,
    int StartNumber,
    int PageCount,
    long OriginalBytes,
    IReadOnlyList<string> Warnings)
{
    public int EndNumber => StartNumber + PageCount - 1;
}

internal sealed record NumberingProgress(int Percent, string Message, TimeSpan? EstimatedRemaining);

internal sealed record DocumentNumberingResult(DocumentNumberingPlan Plan, long OutputBytes);

internal sealed record NumberingResult(IReadOnlyList<DocumentNumberingResult> Documents)
{
    public int TotalPages => Documents.Sum(document => document.Plan.PageCount);
}

internal static class NumberingPlanner
{
    public static NumberingPlan Build(NumberingSettings settings, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(settings.DestinationFolder);

        var entries = settings.Files
            .Select((path, index) => BuildEntry(path, index, settings.PartMarker))
            .ToArray();

        var orderedEntries = OrderEntries(entries);
        var nextBySeries = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var documents = new List<DocumentNumberingPlan>();

        foreach (var entry in orderedEntries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var document = PdfReader.Open(entry.Path, PdfDocumentOpenMode.Import);
            var pageCount = document.PageCount;
            var seriesKey = entry.SeriesKey ?? entry.Path;
            var startNumber = nextBySeries.TryGetValue(seriesKey, out var nextNumber) ? nextNumber : 1;
            nextBySeries[seriesKey] = startNumber + pageCount;

            var outputPath = BuildOutputPath(entry.Path, settings.DestinationFolder);
            var warnings = new List<string>();
            if (Path.GetFullPath(entry.Path).Equals(Path.GetFullPath(outputPath), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"La ruta destino no puede ser la misma que el origen para:\n{entry.Path}");
            }

            if (File.Exists(outputPath))
            {
                warnings.Add($"Ya existe {Path.GetFileName(outputPath)} en destino y se sobrescribirá.");
            }

            documents.Add(new DocumentNumberingPlan(
                entry.Path,
                outputPath,
                entry.SeriesKey ?? Path.GetFileNameWithoutExtension(entry.Path),
                entry.PartNumber,
                startNumber,
                pageCount,
                new FileInfo(entry.Path).Length,
                warnings));
        }

        return new NumberingPlan(settings, documents);
    }

    private static SourceEntry BuildEntry(string path, int originalIndex, string partMarker)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var match = Regex.Match(name, $"{Regex.Escape(partMarker)}(?<number>\\d+)$", RegexOptions.IgnoreCase);
        if (!match.Success || !int.TryParse(match.Groups["number"].Value, out var partNumber))
        {
            return new SourceEntry(path, originalIndex, null, null);
        }

        var seriesKey = name[..match.Index];
        return new SourceEntry(path, originalIndex, seriesKey, partNumber);
    }

    private static IReadOnlyList<SourceEntry> OrderEntries(IReadOnlyList<SourceEntry> entries)
    {
        var firstIndexBySeries = entries
            .Where(entry => entry.SeriesKey is not null)
            .GroupBy(entry => entry.SeriesKey!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Min(entry => entry.OriginalIndex), StringComparer.OrdinalIgnoreCase);

        return entries
            .OrderBy(entry => entry.SeriesKey is null ? entry.OriginalIndex : firstIndexBySeries[entry.SeriesKey])
            .ThenBy(entry => entry.SeriesKey is null ? 0 : 1)
            .ThenBy(entry => entry.SeriesKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.PartNumber ?? entry.OriginalIndex)
            .ThenBy(entry => entry.OriginalIndex)
            .ToArray();
    }

    private static string BuildOutputPath(string sourcePath, string destinationFolder)
    {
        return Path.Combine(destinationFolder, Path.GetFileName(sourcePath));
    }

    private sealed record SourceEntry(string Path, int OriginalIndex, string? SeriesKey, int? PartNumber);
}

internal static class PdfNumberer
{
    public static NumberingResult Execute(NumberingPlan plan, IProgress<NumberingProgress> progress, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(plan.Settings.DestinationFolder);

        var stopwatch = Stopwatch.StartNew();
        var completedPages = 0;
        var results = new List<DocumentNumberingResult>();
        var totalPages = Math.Max(1, plan.TotalPages);

        foreach (var documentPlan in plan.Documents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress.Report(BuildProgress(completedPages, totalPages, stopwatch, $"Procesando {Path.GetFileName(documentPlan.SourcePath)}"));

            var tempPath = documentPlan.OutputPath + ".tmp";
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            File.Copy(documentPlan.SourcePath, tempPath, overwrite: true);
            using (var document = PdfReader.Open(tempPath, PdfDocumentOpenMode.Modify))
            {
                for (var pageIndex = 0; pageIndex < document.Pages.Count; pageIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var value = documentPlan.StartNumber + pageIndex;
                    StampPageNumber(document.Pages[pageIndex], FormatPageText(plan.Settings.TextPrefix, value));

                    completedPages++;
                    progress.Report(BuildProgress(completedPages, totalPages, stopwatch, $"Numerando {Path.GetFileName(documentPlan.SourcePath)}"));
                }

                document.Save(tempPath);
            }

            File.Move(tempPath, documentPlan.OutputPath, overwrite: true);
            results.Add(new DocumentNumberingResult(documentPlan, new FileInfo(documentPlan.OutputPath).Length));
        }

        progress.Report(new NumberingProgress(100, "Proceso terminado.", TimeSpan.Zero));
        return new NumberingResult(results);
    }

    private static void StampPageNumber(PdfPage page, string text)
    {
        using var graphics = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
        var font = new XFont("Arial", 7.5, XFontStyleEx.Regular);

        var textSize = graphics.MeasureString(text, font);
        const double margin = 7;
        const double paddingX = 3.5;
        const double paddingY = 2;
        var boxWidth = textSize.Width + paddingX * 2;
        var boxHeight = textSize.Height + paddingY * 2;
        var x = Math.Max(margin, page.Width.Point - boxWidth - margin);
        var y = Math.Max(margin, page.Height.Point - boxHeight - margin);
        var rectangle = new XRect(x, y, boxWidth, boxHeight);

        graphics.DrawRectangle(XBrushes.White, rectangle);
        graphics.DrawRectangle(new XPen(XColors.Black, 0.35), rectangle);
        graphics.DrawString(text, font, XBrushes.Black, rectangle, XStringFormats.Center);
    }

    private static string FormatPageText(string prefix, int number)
    {
        return string.IsNullOrWhiteSpace(prefix)
            ? number.ToString()
            : $"{prefix} {number}";
    }

    private static NumberingProgress BuildProgress(int completedPages, int totalPages, Stopwatch stopwatch, string message)
    {
        var percent = (int)Math.Round((double)completedPages / totalPages * 100);
        TimeSpan? estimatedRemaining = null;

        if (completedPages > 0)
        {
            var secondsPerPage = stopwatch.Elapsed.TotalSeconds / completedPages;
            var remainingPages = Math.Max(0, totalPages - completedPages);
            estimatedRemaining = TimeSpan.FromSeconds(secondsPerPage * remainingPages);
        }

        return new NumberingProgress(Math.Clamp(percent, 0, 100), message, estimatedRemaining);
    }
}

internal static class NumberingPlanFormatter
{
    public static string Format(NumberingPlan plan)
    {
        var lines = new List<string>
        {
            $"Destino: {plan.Settings.DestinationFolder}",
            $"Texto antes del número: {FormatEmpty(plan.Settings.TextPrefix)}",
            $"Nomenclatura serie: {plan.Settings.PartMarker}",
            string.Empty,
        };

        foreach (var group in plan.Documents.GroupBy(document => document.DisplayGroup))
        {
            var isSeries = group.Any(document => document.PartNumber is not null);
            lines.Add(isSeries ? $"Serie: {group.Key}" : $"Documento: {group.Key}");

            foreach (var document in group)
            {
                var part = document.PartNumber is null ? string.Empty : $" part{document.PartNumber}:";
                lines.Add($"  - {Path.GetFileName(document.SourcePath)}{part}");
                lines.Add($"    Páginas: {document.PageCount}. Numeración: {document.StartNumber}-{document.EndNumber}");
                lines.Add($"    Salida: {Path.GetFileName(document.OutputPath)}");

                foreach (var warning in document.Warnings)
                {
                    lines.Add($"    Aviso: {warning}");
                }
            }

            lines.Add(string.Empty);
        }

        lines.Add($"Total: {plan.Documents.Count} PDF(s), {plan.TotalPages} pág.");
        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatEmpty(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "(en blanco)" : value;
    }
}

internal static class NumberingResultFormatter
{
    public static string Format(NumberingResult result)
    {
        var lines = new List<string>
        {
            "Proceso terminado.",
            string.Empty,
        };

        foreach (var document in result.Documents)
        {
            lines.Add($"{Path.GetFileName(document.Plan.OutputPath)}:");
            lines.Add($"  Páginas numeradas: {document.Plan.PageCount} ({document.Plan.StartNumber}-{document.Plan.EndNumber})");
            lines.Add($"  Tamaño: {FormatBytes(document.OutputBytes)}");
            lines.Add($"  Ruta: {document.Plan.OutputPath}");
            lines.Add(string.Empty);
        }

        lines.Add($"Total: {result.Documents.Count} PDF(s), {result.TotalPages} pág. numeradas.");
        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatBytes(long bytes)
    {
        var mb = bytes / 1024D / 1024D;
        return $"{mb:0.##} MB";
    }
}
