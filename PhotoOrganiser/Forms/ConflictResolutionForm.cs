using PhotoOrganiser.Helpers;
using PhotoOrganiser.Models;

namespace PhotoOrganiser.Forms;

public sealed class ConflictResolutionForm : Form
{
    private readonly IReadOnlyList<FileCandidate> _conflicts;

    private DataGridView _grid = null!;
    private ComboBox _cmbApplyAll = null!;
    private Button _btnOk = null!;
    private Button _btnCancel = null!;

    public IReadOnlyList<ConflictResolution> Resolutions { get; private set; } = [];

    public ConflictResolutionForm(IReadOnlyList<FileCandidate> conflicts)
    {
        _conflicts = conflicts;
        Build();
        Populate();
    }

    private void Build()
    {
        Text = "Resolve Conflicts";
        Size = new Size(960, 480);
        MinimumSize = new Size(700, 380);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(8),
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));

        // ── Grid ──────────────────────────────────────────────────────────────────
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EditMode = DataGridViewEditMode.EditOnEnter,
        };
        AddColumns();
        layout.Controls.Add(_grid, 0, 0);

        // ── Apply-all row ─────────────────────────────────────────────────────────
        var applyRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
        };
        applyRow.Controls.Add(new Label
        {
            Text = "Apply to all:",
            AutoSize = true,
            Margin = new Padding(0, 7, 6, 0),
        });
        _cmbApplyAll = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 140,
            Margin = new Padding(0, 4, 0, 0),
        };
        _cmbApplyAll.Items.AddRange(["(select)", "Skip All", "Rename All"]);
        _cmbApplyAll.SelectedIndex = 0;
        _cmbApplyAll.SelectedIndexChanged += CmbApplyAll_Changed;
        applyRow.Controls.Add(_cmbApplyAll);
        layout.Controls.Add(applyRow, 0, 1);

        // ── Buttons row ───────────────────────────────────────────────────────────
        var btnRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
        };
        _btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 80, Margin = new Padding(4, 8, 0, 0) };
        _btnOk = new Button { Text = "OK", Width = 80, Margin = new Padding(4, 8, 0, 0) };
        _btnOk.Click += BtnOk_Click;
        btnRow.Controls.Add(_btnCancel);
        btnRow.Controls.Add(_btnOk);
        layout.Controls.Add(btnRow, 0, 2);

        Controls.Add(layout);
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;
    }

    private void AddColumns()
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColFileName",   HeaderText = "File Name",         ReadOnly = true, FillWeight = 18 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColSourcePath", HeaderText = "Source Path",       ReadOnly = true, FillWeight = 30 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColDestPath",   HeaderText = "Destination Path",  ReadOnly = true, FillWeight = 30 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColSourceSize", HeaderText = "Source Size",       ReadOnly = true, FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColDestSize",   HeaderText = "Dest Size",         ReadOnly = true, FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = "ColAction",
            HeaderText = "Action",
            FillWeight = 14,
            DataSource = new[] { "Skip", "Rename copy" },
            DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox,
        });
    }

    private void Populate()
    {
        foreach (var f in _conflicts)
        {
            var srcSize  = FormatSize(new FileInfo(f.SourcePath).Length);
            var destInfo = new FileInfo(f.DestinationPath);
            var destSize = destInfo.Exists ? FormatSize(destInfo.Length) : "—";
            _grid.Rows.Add(f.FileName, f.SourcePath, f.DestinationPath, srcSize, destSize, "Skip");
        }
    }

    private void CmbApplyAll_Changed(object? sender, EventArgs e)
    {
        if (_cmbApplyAll.SelectedIndex == 0) return;
        var action = _cmbApplyAll.SelectedIndex == 1 ? "Skip" : "Rename copy";
        foreach (DataGridViewRow row in _grid.Rows)
            row.Cells["ColAction"].Value = action;
        _cmbApplyAll.SelectedIndex = 0;
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        var results = new List<ConflictResolution>(_grid.Rows.Count);
        for (int i = 0; i < _grid.Rows.Count; i++)
        {
            var action = _grid.Rows[i].Cells["ColAction"].Value?.ToString() == "Rename copy"
                ? ConflictAction.Rename
                : ConflictAction.Skip;

            var candidate = action == ConflictAction.Rename
                ? BuildRenamedCandidate(_conflicts[i])
                : _conflicts[i];

            results.Add(new ConflictResolution(candidate, action));
        }
        Resolutions = results;
        DialogResult = DialogResult.OK;
        Close();
    }

    private static FileCandidate BuildRenamedCandidate(FileCandidate f)
    {
        var uniquePath = FileNameHelper.GetUniqueDestinationPath(f.DestinationPath, File.Exists);
        return f with
        {
            DestinationPath = uniquePath,
        };
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1_048_576 => $"{bytes / 1024.0:F1} KB",
        < 1_073_741_824 => $"{bytes / 1_048_576.0:F1} MB",
        _ => $"{bytes / 1_073_741_824.0:F1} GB",
    };
}
