using PhotoOrganiser.Models;
using PhotoOrganiser.Services;

namespace PhotoOrganiser.Forms;

public partial class MainForm : Form
{
    private readonly IFileScanner _scanner = new FileScanner();
    private ScanResult? _lastScan;
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
                _btnStartCopy.Enabled = false;
            }
            else
            {
                _btnStartCopy.Enabled = true;
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

    private void BtnStartCopy_Click(object sender, EventArgs e)
    {
        // Phase 6: implement copy logic here
    }

    private void BtnCancel_Click(object sender, EventArgs e)
    {
        _cts?.Cancel();
    }

    private void SetControlsEnabled(bool enabled)
    {
        _btnBrowseSource.Enabled = enabled;
        _btnBrowseDest.Enabled   = enabled;
        _btnAnalyse.Enabled      = enabled;
        _btnStartCopy.Enabled    = enabled && _lastScan != null &&
                                   (_lastScan.ToCopy.Any(f => !f.ConflictExists) ||
                                    _lastScan.ToCopy.Any(f => f.ConflictExists));
        _btnCancel.Enabled       = !enabled;
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
