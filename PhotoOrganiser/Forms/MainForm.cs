using PhotoOrganiser.Models;
using PhotoOrganiser.Properties;
using PhotoOrganiser.Services;

namespace PhotoOrganiser.Forms;

public partial class MainForm : Form
{
    private readonly ISpecialDateService _specialDateService = new SpecialDateService();
    private IFileScanner _scanner;
    private readonly ICopyEngine _copyEngine = new CopyEngine();
    private ScanResult? _lastScan;
    private CancellationTokenSource? _cts;

    public MainForm()
    {
        InitializeComponent();
        _scanner = new FileScanner(_specialDateService);
        LoadSettings();
        LoadSpecialDates();
    }

    private void LoadSettings()
    {
        _txtSource.Text = Settings.Default.SourceFolder;
        _txtDest.Text   = Settings.Default.DestinationFolder;
    }

    private void SaveSettings()
    {
        Settings.Default.SourceFolder      = _txtSource.Text;
        Settings.Default.DestinationFolder = _txtDest.Text;
        Settings.Default.Save();
    }

    private void BtnBrowseSource_Click(object sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog { Description = "Select source folder" };
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _txtSource.Text = dlg.SelectedPath;
            SaveSettings();
        }
    }

    private void BtnBrowseDest_Click(object sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog { Description = "Select destination folder" };
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _txtDest.Text = dlg.SelectedPath;
            SaveSettings();
        }
    }

    private async void BtnAnalyse_Click(object sender, EventArgs e)
    {
        var source = _txtSource.Text.Trim();
        var dest   = _txtDest.Text.Trim();

        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(dest))
        {
            MessageBox.Show("Select both source and destination folders first.", "Missing folders",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.Equals(source, dest, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Source and destination cannot be the same folder.", "Invalid selection",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (dest.StartsWith(source.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Destination cannot be inside the source folder.", "Invalid selection",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SetControlsEnabled(false);
        _rtbLog.Clear();
        _lastScan = null;
        _lblSummary.Text = "Scanning…";
        _progressBar.Style = ProgressBarStyle.Marquee;
        _progressBar.Value = 0;
        _lblProgress.Text = string.Empty;
        Cursor = Cursors.WaitCursor;

        _cts = new CancellationTokenSource();

        var scanProgress = new Progress<int>(n =>
        {
            _lblProgress.Text = $"Scanning… {n} files found";
        });

        try
        {
            var result = await Task.Run(() => _scanner.ScanAsync(source, dest, _cts.Token, scanProgress), _cts.Token);
            _lastScan = result;

            var duplicates = result.ToCopy.Count(f => f.IsDuplicate);
            var toCopy     = result.ToCopy.Count;
            var total      = result.ToCopy.Count + result.ToSkip.Count;

            foreach (var f in result.ToSkip)
            {
                var kb = new FileInfo(f.SourcePath).Length / 1024.0;
                AppendLog($"[SKIP] {f.FileName} — already exists, not copied ({kb:F0} KB)", Color.Gray);
            }

            foreach (var f in result.Undated)
                AppendLog($"[UNDATED] {f.FileName} — will copy to Undated\\", Color.DarkOrange);

            foreach (var f in result.ToCopy.Where(f => f.IsDuplicate))
                AppendLog($"[DUPLICATE] {f.FileName} — will copy to {f.DestinationPath}", Color.DarkBlue);

            foreach (var folder in result.InaccessibleFolders)
                AppendLog($"[ACCESS DENIED] {folder}", Color.DarkOrange);

            if (total == 0)
            {
                _lblSummary.Text = "No supported files found in the selected folder.";
            }
            else if (toCopy == 0)
            {
                _lblSummary.Text =
                    $"Found {total} files.  →  {result.ToSkip.Count} already exist (nothing new to copy)" +
                    (result.InaccessibleFolders.Count > 0 ? $"  |  {result.InaccessibleFolders.Count} folder(s) inaccessible" : "");
            }
            else
            {
                _lblSummary.Text =
                    $"Found {total} files.  " +
                    $"→  {toCopy} to copy  |  {result.ToSkip.Count} already exist (will be skipped)  " +
                    $"|  {result.Undated.Count} undated  |  {duplicates} duplicates (will copy to Duplicates subfolder)" +
                    (result.InaccessibleFolders.Count > 0 ? $"  |  {result.InaccessibleFolders.Count} folder(s) inaccessible" : "");
            }
        }
        catch (OperationCanceledException)
        {
            _lblSummary.Text = "Scan cancelled.";
        }
        catch (Exception ex)
        {
            _lblSummary.Text = "Scan failed.";
            AppendLog($"[ERROR] {ex.Message}", Color.Red);
        }
        finally
        {
            _progressBar.Style = ProgressBarStyle.Blocks;
            _progressBar.Value = 0;
            Cursor = Cursors.Default;
            SetControlsEnabled(true);
            _cts.Dispose();
            _cts = null;
        }
    }

    private async void BtnStartCopy_Click(object sender, EventArgs e)
    {
        if (_lastScan == null) return;

        var filesToCopy = _lastScan.ToCopy.ToList();

        if (filesToCopy.Count == 0)
        {
            _lblSummary.Text = "Nothing to copy.";
            return;
        }

        SetControlsEnabled(false);
        _rtbLog.Clear();
        _progressBar.Style   = ProgressBarStyle.Blocks;
        _progressBar.Maximum = filesToCopy.Count;
        _progressBar.Value   = 0;
        _lblProgress.Text    = string.Empty;
        Cursor = Cursors.WaitCursor;

        _cts = new CancellationTokenSource();

        int lastCompleted = 0;
        var progress = new Progress<CopyProgress>(p =>
        {
            lastCompleted = p.Completed;
            _progressBar.Value = Math.Min(p.Completed, _progressBar.Maximum);
            _lblProgress.Text  = p.Total > 0 && p.Completed < p.Total
                ? $"Copying {p.Completed + 1} of {p.Total} — {p.CurrentFile}"
                : $"Done — {p.Total} files processed";
        });

        var dest = _txtDest.Text.Trim();

        try
        {
            var result = await Task.Run(
                () => _copyEngine.CopyAsync(filesToCopy, progress, _cts.Token),
                _cts.Token);

            _lblSummary.Text =
                $"Copy complete.  {result.Copied} copied  |  " +
                $"{result.Failed} errors  |  {_lastScan.ToSkip.Count} skipped";

            foreach (var err in result.Errors)
                AppendLog($"[ERROR] {err}", Color.Red);

            AppendLog(
                $"[DONE] {result.Copied} copied, {result.Failed} failed, {_lastScan.ToSkip.Count} skipped",
                Color.DarkGreen);

            if (result.Failed > 0)
                OfferErrorLog(dest, result.Errors);
        }
        catch (OperationCanceledException)
        {
            _lblSummary.Text = $"Cancelled after {lastCompleted} of {filesToCopy.Count} files.  {lastCompleted} copied";
            AppendLog($"[CANCELLED] Stopped after {lastCompleted} of {filesToCopy.Count} files.", Color.DarkOrange);
        }
        catch (Exception ex)
        {
            _lblSummary.Text = "Copy failed.";
            AppendLog($"[ERROR] {ex.Message}", Color.Red);
        }
        finally
        {
            Cursor = Cursors.Default;
            SetControlsEnabled(true);
            _cts.Dispose();
            _cts = null;
        }
    }

    private void OfferErrorLog(string destFolder, IReadOnlyList<string> errors)
    {
        var logPath = Path.Combine(destFolder, "copy_log.txt");
        try
        {
            var lines = new List<string> { $"Copy log — {DateTime.Now:yyyy-MM-dd HH:mm:ss}", string.Empty };
            lines.AddRange(errors.Select(e => $"[ERROR] {e}"));
            File.WriteAllLines(logPath, lines);
        }
        catch
        {
            return;
        }

        var open = MessageBox.Show(
            $"{errors.Count} error(s) occurred. Open error log?\n{logPath}",
            "Copy errors",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (open == DialogResult.Yes)
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(logPath) { UseShellExecute = true });
    }

    private void BtnCancel_Click(object sender, EventArgs e)
    {
        _cts?.Cancel();
    }

    private void SetControlsEnabled(bool enabled)
    {
        _btnBrowseSource.Enabled    = enabled;
        _btnBrowseDest.Enabled      = enabled;
        _btnAnalyse.Enabled         = enabled;
        _btnCancel.Enabled          = !enabled;

        UpdateStartCopyState();
    }

    private void UpdateStartCopyState()
    {
        _btnStartCopy.Enabled = _lastScan?.ToCopy.Count > 0;
    }

    private void AppendLog(string text, Color color)
    {
        _rtbLog.SelectionStart  = _rtbLog.TextLength;
        _rtbLog.SelectionLength = 0;
        _rtbLog.SelectionColor  = color;
        _rtbLog.AppendText(text + Environment.NewLine);
        _rtbLog.SelectionColor  = _rtbLog.ForeColor;
        _rtbLog.ScrollToCaret();
    }

    private void LoadSpecialDates()
    {
        _gridSpecialDates.CellEndEdit -= GridSpecialDates_Changed;
        _gridSpecialDates.RowsRemoved -= GridSpecialDates_Changed;

        _gridSpecialDates.Rows.Clear();
        foreach (var sd in _specialDateService.GetAll())
            _gridSpecialDates.Rows.Add(sd.Name, sd.Month, sd.Day, sd.Year?.ToString() ?? string.Empty);

        _gridSpecialDates.CellEndEdit += GridSpecialDates_Changed;
        _gridSpecialDates.RowsRemoved += GridSpecialDates_Changed;
    }

    private void BtnAddSpecialDate_Click(object sender, EventArgs e)
    {
        _gridSpecialDates.Rows.Add(string.Empty, string.Empty, string.Empty, string.Empty);
        var newRow = _gridSpecialDates.Rows[_gridSpecialDates.Rows.Count - 1];
        _gridSpecialDates.CurrentCell = newRow.Cells[0];
        _gridSpecialDates.BeginEdit(true);
    }

    private void BtnDeleteSpecialDate_Click(object sender, EventArgs e)
    {
        var selected = _gridSpecialDates.SelectedRows.Cast<DataGridViewRow>()
            .Where(r => !r.IsNewRow).ToList();
        foreach (var row in selected)
            _gridSpecialDates.Rows.Remove(row);
    }

    private void GridSpecialDates_Changed(object? sender, EventArgs e)
    {
        SaveSpecialDates();
    }

    private void SaveSpecialDates()
    {
        var dates = new List<SpecialDate>();
        foreach (DataGridViewRow row in _gridSpecialDates.Rows)
        {
            if (row.IsNewRow) continue;
            var name = row.Cells[0].Value?.ToString()?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(name)) continue;

            if (!int.TryParse(row.Cells[1].Value?.ToString(), out int month) || month < 1 || month > 12)
            {
                row.ErrorText = "Month must be 1–12";
                continue;
            }
            if (!int.TryParse(row.Cells[2].Value?.ToString(), out int day) || day < 1 || day > 31)
            {
                row.ErrorText = "Day must be 1–31";
                continue;
            }
            row.ErrorText = string.Empty;

            int? year = null;
            var yearStr = row.Cells[3].Value?.ToString()?.Trim();
            if (!string.IsNullOrEmpty(yearStr))
            {
                if (!int.TryParse(yearStr, out int y))
                {
                    row.ErrorText = "Year must be a number";
                    continue;
                }
                year = y;
            }

            dates.Add(new SpecialDate { Name = name, Month = month, Day = day, Year = year });
        }
        _specialDateService.Save(dates);
    }
}
