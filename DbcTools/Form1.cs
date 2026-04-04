using DbcTools.Models;
using DbcTools.Parsers;
using DbcTools.Writers;

namespace DbcTools;

public partial class Form1 : Form
{
    private string? _dbcFilePath;
    private string? _lastDirectory;
    private List<DbcSignal> _signals = [];
    private DataGridView dgvSignals = null!;
    private Button btnOpen = null!;
    private Button btnGenerate = null!;
    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel lblStatus = null!;

    public Form1()
    {
        InitializeComponent();
        SetupLayout();
    }

    private void SetupLayout()
    {
        string version = "1.0.0";

        this.Text = "ScudPower DBC 转换工具 " + version;
        this.ClientSize = new Size(920, 520);
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MaximizeBox = true;
        this.MinimumSize = new Size(640, 400);
        this.StartPosition = FormStartPosition.CenterScreen;

        var panelTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            Padding = new Padding(10, 10, 10, 0)
        };

        btnOpen = new Button
        {
            Text = "打开文件",
            Size = new Size(100, 32),
            Location = new Point(10, 8)
        };
        btnOpen.Click += BtnOpen_Click;

        btnGenerate = new Button
        {
            Text = "生成",
            Size = new Size(100, 32),
            Location = new Point(120, 8),
            Enabled = false
        };
        btnGenerate.Click += BtnGenerate_Click;

        panelTop.Controls.Add(btnOpen);
        panelTop.Controls.Add(btnGenerate);

        dgvSignals = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D
        };

        dgvSignals.Columns.Add("ColMsgName", "报文名称");
        dgvSignals.Columns.Add("ColMsgId", "报文ID");
        dgvSignals.Columns.Add("ColName", "信号名");
        dgvSignals.Columns.Add("ColStartBit", "起始位");
        dgvSignals.Columns.Add("ColLength", "长度");
        dgvSignals.Columns.Add("ColFactor", "精度");
        dgvSignals.Columns.Add("ColOffset", "偏移量");
        dgvSignals.Columns.Add("ColUnit", "单位");
        dgvSignals.Columns.Add("ColByteOrder", "字节顺序");
        dgvSignals.Columns.Add("ColValueType", "值类型");

        statusStrip = new StatusStrip();
        lblStatus = new ToolStripStatusLabel("就绪")
        {
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft
        };
        statusStrip.Items.Add(lblStatus);

        this.Controls.Add(dgvSignals);
        this.Controls.Add(panelTop);
        this.Controls.Add(statusStrip);
    }

    private void BtnOpen_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog();
        ofd.Filter = "DBC文件 (*.dbc)|*.dbc|所有文件 (*.*)|*.*";
        ofd.Title = "选择DBC文件";
        if (!string.IsNullOrEmpty(_lastDirectory))
            ofd.InitialDirectory = _lastDirectory;

        if (ofd.ShowDialog() != DialogResult.OK) return;

        _lastDirectory = Path.GetDirectoryName(ofd.FileName);
        _dbcFilePath = ofd.FileName;
        lblStatus.Text = $"已打开: {_dbcFilePath}";

        try
        {
            _signals = DbcParser.Parse(_dbcFilePath);
            PopulateGrid();
            btnGenerate.Enabled = _signals.Count > 0;
            lblStatus.Text = $"已打开: {_dbcFilePath} | 解析到 {_signals.Count} 个信号";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"解析DBC文件失败:\n{ex.Message}", "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void PopulateGrid()
    {
        dgvSignals.Rows.Clear();
        foreach (var sig in _signals)
        {
            dgvSignals.Rows.Add(
                sig.MessageName,
                $"0x{(sig.MessageId & 0x1FFFFFFF):X}",
                sig.Name,
                sig.StartBit,
                sig.Length,
                sig.Factor,
                sig.Offset,
                sig.Unit,
                sig.ByteOrder,
                sig.ValueType
            );
        }
    }

    private void BtnGenerate_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_dbcFilePath) || _signals.Count == 0) return;

        using var sfd = new SaveFileDialog();
        sfd.Filter = "二进制文件 (*.bin)|*.bin";
        sfd.Title = "保存BIN文件";
        sfd.FileName = Path.ChangeExtension(Path.GetFileName(_dbcFilePath), ".bin");

        if (sfd.ShowDialog() != DialogResult.OK) return;

        try
        {
            BinWriter.Write(sfd.FileName, _signals);
            lblStatus.Text = $"已生成: {sfd.FileName}";
            MessageBox.Show($"成功生成 {sfd.FileName}", "完成",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"生成BIN文件失败:\n{ex.Message}", "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
