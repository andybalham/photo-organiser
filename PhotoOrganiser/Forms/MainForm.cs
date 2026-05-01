using PhotoOrganiser.Models;
using PhotoOrganiser.Services;

namespace PhotoOrganiser.Forms;

public partial class MainForm : Form
{
    private readonly IFileScanner _scanner = new FileScanner();
    private readonly ICopyEngine _copyEngine = new CopyEngine();
    private ScanResult? _lastScan;
    private IReadOnlyList<ConflictResolution>? _conflictResolutions;
    private CancellationTokenSource? _cts;

    public MainForm()
    {
        InitializeComponent();
    }

    private void BtnBrowseSource_Click(object sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog { Description = "Select source folder" };
        if (dlg.ShowDialog(this) == DialogResult.OK)
            _txtSource.Text = dlg.SelectedPath;
    }

    private void BtnBrowseDest_Click(object sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog { Description = "Select destination folder" };
        if (dlg.ShowDialog(this) == DialogResult.OK)
            _txtDest.Text = dlg.SelectedPath;
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
        _conflictResolutions = null;
        _lblSummary.Text = "Scanning…";
        _progressBar.Value = 0;
        _lblProgress.Text = string.Empty;

        _cts = new CancellationTokenSource();

        try
        {
            var result = await Task.Run(() => _scanner.ScanAsync(source, dest, _cts.Token), _cts.Token);
            _lastScan = result;

            var conflicts = result.ToCopy.Count(f => f.ConflictExists);
            var toCopy    = result.ToCopy.Count(f => !f.ConflictExists);

            _lblSummary.Text =
                $"Found {result.ToCopy.Count + result.ToSkip.Count + result.Undated.Count} files.  " +
                $"→  {toCopy} to copy  |  {result.ToSkip.Count} already exist (will be skipped)  " +
                $"|  {result.Undated.Count} undated  |  {conflicts} conflicts need review";

            foreach (var f in result.ToSkip)
                AppendLog($"[SKIP] {f.FileName} — already exists", Color.Gray);

            foreach (var f in result.Undated)
                AppendLog($"[UNDATED] {f.FileName} — will copy to Undated\\", Color.DarkOrange);

            foreach (var f in result.ToCopy.Where(f => f.ConflictExists))
                AppendLog($"[CONFLICT] {f.FileName} — same name, different size", Color.Red);

            if (toCopy == 0 && conflicts == 0)
            {
                _lblSummary.Text = "Nothing to copy.";
            }
            else
            {
                if (conflicts > 0)
                    ShowConflictDialog();
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
            SetControlsEnabled(true);
            _cts.Dispose();
            _cts = null;
        }
    }

    private void BtnReviewConflicts_Click(object sender, EventArgs e)
    {
        ShowConflictDialog();
    }

    private void ShowConflictDialog()
    {
        if (_lastScan == null) return;

        var conflictFiles = _lastScan.ToCopy.Where(f => f.ConflictExists).ToList();
        if (conflictFiles.Count == 0) return;

        using var dlg = new ConflictResolutionForm(conflictFiles);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _conflictResolutions = dlg.Resolutions;
            UpdateConflictLog();
        }
        // Cancel: leave _conflictResolutions as-is (no copy starts without resolution)
        UpdateStartCopyState();
    }

    private void UpdateConflictLog()
    {
        if (_conflictResolutions == null) return;
        foreach (var r in _conflictResolutions)
        {
            var label = r.Action == ConflictAction.Rename
                ? $"[CONFLICT→RENAME] {r.Candidate.FileName} → {Path.GetFileName(r.Candidate.DestinationPath)}"
                : $"[CONFLICT→SKIP]   {r.Candidate.FileName}";
            var colour = r.Action == ConflictAction.Rename ? Color.DarkBlue : Color.Gray;
            AppendLog(label, colour);
        }
    }

    private async void BtnStartCopy_Click(object sender, EventArgs e)
    {
        if (_lastScan == null) return;

        // Build final file list: clean files + resolved conflicts (skipped conflicts excluded)
        var filesToCopy = _lastScan.ToCopy
            .Where(f => !f.ConflictExists)
            .ToList();

        if (_conflictResolutions != null)
        {
            foreach (var r in _conflictResolutions.Where(r => r.Action == ConflictAction.Rename))
                filesToCopy.Add(r.Candidate);
        }

        if (filesToCopy.Count == 0)
        {
            _lblSummary.Text = "Nothing to copy.";
            return;
        }

        SetControlsEnabled(false);
        _rtbLog.Clear();
        _progressBar.Maximum = filesToCopy.Count;
        _progressBar.Value   = 0;
        _lblProgress.Text    = string.Empty;

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

            var skippedConflicts = _conflictResolutions?.Count(r => r.Action == ConflictAction.Skip) ?? 0;

            _lblSummary.Text =
                $"Copy complete.  {result.Copied} copied  |  " +
                $"{result.Failed} errors  |  {_lastScan.ToSkip.Count + skippedConflicts} skipped";

            foreach (var err in result.Errors)
                AppendLog($"[ERROR] {err}", Color.Red);

            AppendLog(
                $"[DONE] {result.Copied} copied, {result.Failed} failed, " +
                $"{_lastScan.ToSkip.Count + skippedConflicts} skipped",
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

        var hasConflicts = _lastScan?.ToCopy.Any(f => f.ConflictExists) ?? false;
        _btnReviewConflicts.Enabled = enabled && hasConflicts;
    }

    private void UpdateStartCopyState()
    {
        if (_lastScan == null)
        {
            _btnStartCopy.Enabled = false;
            return;
        }

        var cleanCopyCount  = _lastScan.ToCopy.Count(f => !f.ConflictExists);
        var conflictCount   = _lastScan.ToCopy.Count(f => f.ConflictExists);
        var resolvedCount   = _conflictResolutions?.Count ?? 0;
        var allResolved     = conflictCount == 0 || resolvedCount == conflictCount;

        // Start Copy enabled when: clean files exist OR all conflicts have been resolved
        _btnStartCopy.Enabled = (cleanCopyCount > 0 || resolvedCount > 0) && allResolved;
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
}
