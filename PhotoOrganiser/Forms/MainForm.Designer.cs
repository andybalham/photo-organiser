namespace PhotoOrganiser.Forms;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        // ── Controls ────────────────────────────────────────────────────────────
        _tableMain          = new TableLayoutPanel();
        _pnlSource          = new Panel();
        _lblSource          = new Label();
        _txtSource          = new TextBox();
        _btnBrowseSource    = new Button();
        _pnlDest            = new Panel();
        _lblDest            = new Label();
        _txtDest            = new TextBox();
        _btnBrowseDest      = new Button();
        _tabConfig          = new TabControl();
        _tabPageSpecialDates = new TabPage();
        _pnlSpecialDates    = new Panel();
        _gridSpecialDates   = new DataGridView();
        _pnlSpecialDatesBtns = new Panel();
        _btnAddSpecialDate  = new Button();
        _btnDeleteSpecialDate = new Button();
        _lblSummary         = new Label();
        _rtbLog             = new RichTextBox();
        _pnlProgress        = new Panel();
        _progressBar        = new ProgressBar();
        _lblProgress        = new Label();
        _pnlButtons         = new Panel();
        _btnAnalyse         = new Button();
        _btnStartCopy       = new Button();
        _btnCancel          = new Button();

        _tableMain.SuspendLayout();
        _pnlSource.SuspendLayout();
        _pnlDest.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_gridSpecialDates).BeginInit();
        _pnlSpecialDates.SuspendLayout();
        _pnlSpecialDatesBtns.SuspendLayout();
        _tabPageSpecialDates.SuspendLayout();
        _pnlProgress.SuspendLayout();
        _pnlButtons.SuspendLayout();
        SuspendLayout();

        // ── tableMain ───────────────────────────────────────────────────────────
        _tableMain.Dock = DockStyle.Fill;
        _tableMain.ColumnCount = 1;
        _tableMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _tableMain.RowCount = 7;
        _tableMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));  // source row
        _tableMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));  // dest row
        _tableMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 175F)); // special dates tab
        _tableMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));  // summary label
        _tableMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // log (expands)
        _tableMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));  // progress
        _tableMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));  // buttons
        _tableMain.Padding = new Padding(8);
        _tableMain.Controls.Add(_pnlSource,   0, 0);
        _tableMain.Controls.Add(_pnlDest,     0, 1);
        _tableMain.Controls.Add(_tabConfig,   0, 2);
        _tableMain.Controls.Add(_lblSummary,  0, 3);
        _tableMain.Controls.Add(_rtbLog,      0, 4);
        _tableMain.Controls.Add(_pnlProgress, 0, 5);
        _tableMain.Controls.Add(_pnlButtons,  0, 6);

        // ── Source panel ────────────────────────────────────────────────────────
        _pnlSource.Dock = DockStyle.Fill;
        _pnlSource.Controls.Add(_btnBrowseSource);
        _pnlSource.Controls.Add(_txtSource);
        _pnlSource.Controls.Add(_lblSource);

        _lblSource.Text = "Source:";
        _lblSource.Width = 60;
        _lblSource.TextAlign = ContentAlignment.MiddleLeft;
        _lblSource.Dock = DockStyle.Left;

        _txtSource.ReadOnly = true;
        _txtSource.Dock = DockStyle.Fill;
        _txtSource.TabIndex = 0;

        _btnBrowseSource.Text = "Browse…";
        _btnBrowseSource.Width = 80;
        _btnBrowseSource.Dock = DockStyle.Right;
        _btnBrowseSource.TabIndex = 1;
        _btnBrowseSource.Click += BtnBrowseSource_Click;

        // ── Dest panel ──────────────────────────────────────────────────────────
        _pnlDest.Dock = DockStyle.Fill;
        _pnlDest.Controls.Add(_btnBrowseDest);
        _pnlDest.Controls.Add(_txtDest);
        _pnlDest.Controls.Add(_lblDest);

        _lblDest.Text = "Destination:";
        _lblDest.Width = 80;
        _lblDest.TextAlign = ContentAlignment.MiddleLeft;
        _lblDest.Dock = DockStyle.Left;

        _txtDest.ReadOnly = true;
        _txtDest.Dock = DockStyle.Fill;
        _txtDest.TabIndex = 2;

        _btnBrowseDest.Text = "Browse…";
        _btnBrowseDest.Width = 80;
        _btnBrowseDest.Dock = DockStyle.Right;
        _btnBrowseDest.TabIndex = 3;
        _btnBrowseDest.Click += BtnBrowseDest_Click;

        // ── Special Dates tab ───────────────────────────────────────────────────
        _tabConfig.Dock = DockStyle.Fill;
        _tabConfig.TabPages.Add(_tabPageSpecialDates);

        _tabPageSpecialDates.Text = "Special Dates";
        _tabPageSpecialDates.Padding = new Padding(4);
        _tabPageSpecialDates.Controls.Add(_pnlSpecialDates);

        _pnlSpecialDates.Dock = DockStyle.Fill;
        _pnlSpecialDates.Controls.Add(_gridSpecialDates);
        _pnlSpecialDates.Controls.Add(_pnlSpecialDatesBtns);

        _pnlSpecialDatesBtns.Dock = DockStyle.Right;
        _pnlSpecialDatesBtns.Width = 90;
        _pnlSpecialDatesBtns.Padding = new Padding(4, 0, 0, 0);
        _pnlSpecialDatesBtns.Controls.Add(_btnDeleteSpecialDate);
        _pnlSpecialDatesBtns.Controls.Add(_btnAddSpecialDate);

        _btnAddSpecialDate.Text = "Add";
        _btnAddSpecialDate.Dock = DockStyle.Top;
        _btnAddSpecialDate.Height = 30;
        _btnAddSpecialDate.Click += BtnAddSpecialDate_Click;

        _btnDeleteSpecialDate.Text = "Delete";
        _btnDeleteSpecialDate.Dock = DockStyle.Top;
        _btnDeleteSpecialDate.Height = 30;
        _btnDeleteSpecialDate.Top = 34;
        _btnDeleteSpecialDate.Click += BtnDeleteSpecialDate_Click;

        _gridSpecialDates.Dock = DockStyle.Fill;
        _gridSpecialDates.AllowUserToAddRows = false;
        _gridSpecialDates.AllowUserToDeleteRows = false;
        _gridSpecialDates.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _gridSpecialDates.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _gridSpecialDates.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _gridSpecialDates.MultiSelect = true;
        _gridSpecialDates.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name",  HeaderText = "Name",           FillWeight = 40 });
        _gridSpecialDates.Columns.Add(new DataGridViewTextBoxColumn { Name = "Month", HeaderText = "Month (1–12)",   FillWeight = 20 });
        _gridSpecialDates.Columns.Add(new DataGridViewTextBoxColumn { Name = "Day",   HeaderText = "Day (1–31)",     FillWeight = 20 });
        _gridSpecialDates.Columns.Add(new DataGridViewTextBoxColumn { Name = "Year",  HeaderText = "Year (optional)", FillWeight = 20 });

        // ── Summary label ───────────────────────────────────────────────────────
        _lblSummary.Dock = DockStyle.Fill;
        _lblSummary.TextAlign = ContentAlignment.MiddleLeft;
        _lblSummary.Text = "Select source and destination folders, then click Analyse.";
        _lblSummary.Font = new Font(Font.FontFamily, Font.Size, FontStyle.Regular);

        // ── Log area ────────────────────────────────────────────────────────────
        _rtbLog.Dock = DockStyle.Fill;
        _rtbLog.ReadOnly = true;
        _rtbLog.BackColor = SystemColors.Window;
        _rtbLog.Font = new Font("Consolas", 9F);
        _rtbLog.ScrollBars = RichTextBoxScrollBars.Vertical;
        _rtbLog.TabIndex = 4;

        // ── Progress panel ──────────────────────────────────────────────────────
        _pnlProgress.Dock = DockStyle.Fill;
        _pnlProgress.Controls.Add(_progressBar);
        _pnlProgress.Controls.Add(_lblProgress);

        _lblProgress.Text = string.Empty;
        _lblProgress.Width = 200;
        _lblProgress.TextAlign = ContentAlignment.MiddleLeft;
        _lblProgress.Dock = DockStyle.Right;

        _progressBar.Dock = DockStyle.Fill;
        _progressBar.Minimum = 0;
        _progressBar.Value = 0;

        // ── Buttons panel ───────────────────────────────────────────────────────
        _pnlButtons.Dock = DockStyle.Fill;
        _pnlButtons.Padding = new Padding(0, 4, 0, 4);
        _pnlButtons.Controls.Add(_btnCancel);
        _pnlButtons.Controls.Add(_btnStartCopy);
        _pnlButtons.Controls.Add(_btnAnalyse);

        _btnAnalyse.Text = "Analyse";
        _btnAnalyse.Width = 90;
        _btnAnalyse.Dock = DockStyle.Left;
        _btnAnalyse.TabIndex = 5;
        _btnAnalyse.Click += BtnAnalyse_Click;

        _btnStartCopy.Text = "Start Copy";
        _btnStartCopy.Width = 90;
        _btnStartCopy.Dock = DockStyle.Right;
        _btnStartCopy.Enabled = false;
        _btnStartCopy.TabIndex = 7;
        _btnStartCopy.Click += BtnStartCopy_Click;

        _btnCancel.Text = "Cancel";
        _btnCancel.Width = 90;
        _btnCancel.Dock = DockStyle.Right;
        _btnCancel.Enabled = false;
        _btnCancel.TabIndex = 8;
        _btnCancel.Click += BtnCancel_Click;

        // ── Form ────────────────────────────────────────────────────────────────
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(700, 550);
        MinimumSize = new Size(540, 400);
        Text = "Photo Organiser";
        Controls.Add(_tableMain);

        _tableMain.ResumeLayout(false);
        _pnlSource.ResumeLayout(false);
        _pnlSource.PerformLayout();
        _pnlDest.ResumeLayout(false);
        _pnlDest.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_gridSpecialDates).EndInit();
        _pnlSpecialDates.ResumeLayout(false);
        _pnlSpecialDatesBtns.ResumeLayout(false);
        _tabPageSpecialDates.ResumeLayout(false);
        _pnlProgress.ResumeLayout(false);
        _pnlButtons.ResumeLayout(false);
        ResumeLayout(false);
    }

    // ── Fields ──────────────────────────────────────────────────────────────────
    private TableLayoutPanel _tableMain;
    private Panel _pnlSource;
    private Label _lblSource;
    private TextBox _txtSource;
    private Button _btnBrowseSource;
    private Panel _pnlDest;
    private Label _lblDest;
    private TextBox _txtDest;
    private Button _btnBrowseDest;
    private TabControl _tabConfig;
    private TabPage _tabPageSpecialDates;
    private Panel _pnlSpecialDates;
    private DataGridView _gridSpecialDates;
    private Panel _pnlSpecialDatesBtns;
    private Button _btnAddSpecialDate;
    private Button _btnDeleteSpecialDate;
    private Label _lblSummary;
    private RichTextBox _rtbLog;
    private Panel _pnlProgress;
    private ProgressBar _progressBar;
    private Label _lblProgress;
    private Panel _pnlButtons;
    private Button _btnAnalyse;
    private Button _btnStartCopy;
    private Button _btnCancel;
}
