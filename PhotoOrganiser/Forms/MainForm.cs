namespace PhotoOrganiser.Forms;

public partial class MainForm : Form
{
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

    private void BtnAnalyse_Click(object sender, EventArgs e)
    {
        // Phase 4: implement scan logic here
    }

    private void BtnStartCopy_Click(object sender, EventArgs e)
    {
        // Phase 6: implement copy logic here
    }

    private void BtnCancel_Click(object sender, EventArgs e)
    {
        // Phase 6: implement cancellation here
    }
}
