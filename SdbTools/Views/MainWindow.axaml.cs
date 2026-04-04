using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using DbcTools.CrossPlatform.Models;
using DbcTools.CrossPlatform.Parsers;
using DbcTools.CrossPlatform.Writers;
using System.Linq;
using System.Text.Json;
using System.Diagnostics;

namespace DbcTools.CrossPlatform.Views;

public partial class MainWindow : Window
{
    private string? _dbcFilePath;
    private string? _lastStatusText;
    private string? _lastStatusPath;
    private List<DbcSignal> _signals = [];
    private readonly DataGrid _dataGrid;
    private int _currentLang = 0;

    private readonly Dictionary<string, string[]> _lang = new()
    {
        ["zh"] = ["SdbTools", "打开文件", "生成", "就绪", "报文名称", "报文ID", "信号名", "起始位", "长度", "精度", "偏移量", "单位", "字节顺序", "值类型", "选择DBC文件", "DBC文件", "所有文件", "保存SDBC文件", "SDBC文件", "错误", "解析DBC文件失败", "完成", "成功生成", "生成SDBC文件失败", "个信号", "检查升级", "无效", "发现新版本", "是否前往下载", "当前版本", "已是最新版本", "检查更新失败", "请检查网络连接", "下载", "取消", "版本更新", "提示"],
        ["en"] = ["SdbTools", "Open", "Generate", "Ready", "Message Name", "Message ID", "Signal Name", "Start Bit", "Length", "Factor", "Offset", "Unit", "Byte Order", "Value Type", "Select DBC File", "DBC Files", "All Files", "Save SDBC File", "SDBC Files", "Error", "Failed to parse DBC file", "Success", "Successfully generated", "Failed to generate SDBC file", "signals", "Check Update", "invalid", "New version available", "Download now?", "Current version", "Already up to date", "Check failed", "Please check network", "Download", "Cancel", "Version Update", "Notice"]
    };

    private readonly string _version = "v1.0.0";
    private const string REPO_OWNER = "AaronLin1124";
    private const string REPO_NAME = "SdbTools";

    public MainWindow()
    {
        InitializeComponent();

        _dataGrid = new DataGrid
        {
            IsReadOnly = true,
            AutoGenerateColumns = false,
            Background = Avalonia.Media.Brushes.White,
            ItemsSource = _signals,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            Width = double.NaN,
            Height = double.NaN,
            CanUserResizeColumns = true,
            CanUserReorderColumns = true,
            GridLinesVisibility = DataGridGridLinesVisibility.All,
            HeadersVisibility = DataGridHeadersVisibility.Column
        };
        
        _dataGrid.Columns.Add(new DataGridTextColumn 
        { 
            Header = "报文名称", 
            Binding = new Binding("MessageName"),
            Width = new DataGridLength(2, DataGridLengthUnitType.Star),
            MinWidth = 120
        });
        _dataGrid.Columns.Add(new DataGridTextColumn 
        { 
            Header = "报文ID", 
            Binding = new Binding("MessageIdHex"),
            Width = new DataGridLength(1, DataGridLengthUnitType.Star),
            MinWidth = 100
        });
        _dataGrid.Columns.Add(new DataGridTextColumn 
        { 
            Header = "信号名", 
            Binding = new Binding("Name"),
            Width = new DataGridLength(2, DataGridLengthUnitType.Star),
            MinWidth = 120
        });
        _dataGrid.Columns.Add(new DataGridTextColumn 
        { 
            Header = "起始位", 
            Binding = new Binding("StartBit"),
            Width = new DataGridLength(0.8, DataGridLengthUnitType.Star),
            MinWidth = 80
        });
        _dataGrid.Columns.Add(new DataGridTextColumn 
        { 
            Header = "长度", 
            Binding = new Binding("Length"),
            Width = new DataGridLength(0.7, DataGridLengthUnitType.Star),
            MinWidth = 70
        });
        _dataGrid.Columns.Add(new DataGridTextColumn 
        { 
            Header = "精度", 
            Binding = new Binding("Factor"),
            Width = new DataGridLength(0.8, DataGridLengthUnitType.Star),
            MinWidth = 80
        });
        _dataGrid.Columns.Add(new DataGridTextColumn 
        { 
            Header = "偏移量", 
            Binding = new Binding("Offset"),
            Width = new DataGridLength(0.8, DataGridLengthUnitType.Star),
            MinWidth = 80
        });
        _dataGrid.Columns.Add(new DataGridTextColumn 
        { 
            Header = "单位", 
            Binding = new Binding("Unit"),
            Width = new DataGridLength(0.7, DataGridLengthUnitType.Star),
            MinWidth = 70
        });
        _dataGrid.Columns.Add(new DataGridTextColumn 
        { 
            Header = "字节顺序", 
            Binding = new Binding("ByteOrder"),
            Width = new DataGridLength(0.9, DataGridLengthUnitType.Star),
            MinWidth = 90
        });
        _dataGrid.Columns.Add(new DataGridTextColumn 
        { 
            Header = "值类型", 
            Binding = new Binding("ValueType"),
            Width = new DataGridLength(0.9, DataGridLengthUnitType.Star),
            MinWidth = 90
        });
        ContentArea.Child = _dataGrid;

        BtnOpen.Click += BtnOpen_Click;
        BtnGenerate.Click += BtnGenerate_Click;
        BtnCheckUpdate.Click += BtnCheckUpdate_Click;
        CmbLanguage.SelectionChanged += CmbLanguage_SelectionChanged;
    }

    private void SetupColumns()
    {
        string[] cols = _currentLang == 0 
            ? ["报文名称", "报文ID", "信号名", "起始位", "长度", "精度", "偏移量", "单位", "字节顺序", "值类型"]
            : ["Message Name", "Message ID", "Signal Name", "Start Bit", "Length", "Factor", "Offset", "Unit", "Byte Order", "Value Type"];
        
        for (int i = 0; i < cols.Length; i++)
        {
            if (_dataGrid.Columns[i] is DataGridTextColumn col)
            {
                col.Header = cols[i];
            }
        }
    }

    private void UpdateUI()
    {
        var t = _lang[_currentLang == 0 ? "zh" : "en"];
        Title = t[0];
        BtnOpen.Content = t[1];
        BtnGenerate.Content = t[2];
        BtnCheckUpdate.Content = t[25];
        
        if (!string.IsNullOrEmpty(_lastStatusPath))
        {
            var signalCount = _signals.Count;
            var invalidCount = _signals.Count(s => s.IsInvalid);
            var validCount = signalCount - invalidCount;
            var validText = _currentLang == 0 ? "有效" : "valid";
            if (invalidCount > 0)
                LblStatus.Text = $"{t[3]}: {_lastStatusPath} | {signalCount} {t[24]} ({validCount} {validText})";
            else
                LblStatus.Text = $"{t[3]}: {_lastStatusPath} | {signalCount} {t[24]}";
            _lastStatusText = LblStatus.Text;
        }
        else
        {
            LblStatus.Text = t[3];
            _lastStatusText = LblStatus.Text;
            _lastStatusPath = null;
        }
        
        SetupColumns();
        if (_signals.Count > 0)
        {
            _dataGrid.ItemsSource = null;
            _dataGrid.ItemsSource = _signals;
        }
    }

    private void CmbLanguage_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _currentLang = CmbLanguage.SelectedIndex;
        UpdateUI();
    }

    private async void BtnOpen_Click(object? sender, RoutedEventArgs e)
    {
        var t = _lang[_currentLang == 0 ? "zh" : "en"];
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = t[14],
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType(t[15]) { Patterns = ["*.dbc"] },
                new FilePickerFileType(t[16]) { Patterns = ["*.*"] }
            ]
        });

        if (files.Count == 0) return;

        _dbcFilePath = files[0].Path.LocalPath;
        var statusReady = t[3];
        LblStatus.Text = $"{statusReady}: {_dbcFilePath}";
        _lastStatusText = LblStatus.Text;
        _lastStatusPath = _dbcFilePath;

        try
        {
            _signals = DbcParser.Parse(_dbcFilePath);
            _dataGrid.ItemsSource = _signals;
            BtnGenerate.IsEnabled = _signals.Count > 0;
            var invalidCount = _signals.Count(s => s.IsInvalid);
            var validCount = _signals.Count - invalidCount;
            var validText = _currentLang == 0 ? "有效" : "valid";
            if (invalidCount > 0)
                LblStatus.Text = $"{statusReady}: {_dbcFilePath} | {_signals.Count} {t[24]} ({validCount} {validText})";
            else
                LblStatus.Text = $"{statusReady}: {_dbcFilePath} | {_signals.Count} {t[24]}";
            _lastStatusText = LblStatus.Text;
        }
        catch (Exception ex)
        {
            await ShowMessage(t[19], $"{t[20]}:\n{ex.Message}");
        }
    }

    private async void BtnGenerate_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_dbcFilePath) || _signals.Count == 0) return;

        var t = _lang[_currentLang == 0 ? "zh" : "en"];
        var defaultName = Path.ChangeExtension(Path.GetFileName(_dbcFilePath), ".sdbc");
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = t[17],
            SuggestedFileName = defaultName,
            FileTypeChoices =
            [
                new FilePickerFileType(t[18]) { Patterns = ["*.sdbc"] }
            ]
        });

        if (file == null) return;

        try
        {
            BinWriter.Write(file.Path.LocalPath, _signals);
            LblStatus.Text = $"{t[22]}: {file.Path.LocalPath}";
            _lastStatusText = LblStatus.Text;
            _lastStatusPath = file.Path.LocalPath;
            await ShowMessage(t[21], $"{t[22]}: {file.Name}");
        }
        catch (Exception ex)
        {
            await ShowMessage(t[19], $"{t[23]}:\n{ex.Message}");
        }
    }

    private async Task ShowMessage(string title, string message)
    {
        var t = _lang[_currentLang == 0 ? "zh" : "en"];
        var dialog = new Window
        {
            Title = title,
            Width = 360,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var btn = new Button
        {
            Content = _currentLang == 0 ? "确定" : "OK",
            Width = 80,
            Height = 30,
            HorizontalAlignment = HorizontalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        btn.Click += (_, _) => dialog.Close();

        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 15,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                btn
            }
        };

        dialog.Content = panel;
        await dialog.ShowDialog(this);
    }

    private async void BtnCheckUpdate_Click(object? sender, RoutedEventArgs e)
    {
        var t = _lang[_currentLang == 0 ? "zh" : "en"];
        LblStatus.Text = $"{t[3]}: ...";
        
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SdbTools");
            var json = await client.GetStringAsync(
                $"https://api.github.com/repos/{REPO_OWNER}/{REPO_NAME}/releases/latest");
            using var doc = JsonDocument.Parse(json);
            var latestVersion = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
            
            var localVer = new Version(_version.TrimStart('v'));
            var remoteVer = new Version(latestVersion.TrimStart('v'));
            
            if (remoteVer > localVer)
            {
                var result = await ShowConfirmDialog(t[35], $"{t[27]}\n{t[29]}: {_version} -> {latestVersion}");
                if (!result) return;
                
                var assets = doc.RootElement.GetProperty("assets");
                string? downloadUrl = null;
                string? fileName = null;
                
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString() ?? "";
                    if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = asset.GetProperty("browser_download_url").GetString();
                        fileName = name;
                        break;
                    }
                }
                
                if (downloadUrl != null)
                {
                    var tempPath = Path.Combine(Path.GetTempPath(), fileName ?? "SdbTools_update.exe");
                    
                    using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength ?? -1;
                    
                    LblStatus.Text = $"{t[33]}: 0%";
                    
                    await using var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await using var stream = await response.Content.ReadAsStreamAsync();
                    
                    var buffer = new byte[8192];
                    long totalRead = 0;
                    int bytesRead;
                    while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                    {
                        await fs.WriteAsync(buffer.AsMemory(0, bytesRead));
                        totalRead += bytesRead;
                        if (totalBytes > 0)
                        {
                            var progress = (int)((totalRead * 100) / totalBytes);
                            LblStatus.Text = $"{t[33]}: {progress}%";
                        }
                    }
                    
                    var currentExe = Environment.ProcessPath ?? "";
                    var batchPath = Path.Combine(Path.GetTempPath(), "update.bat");
                    var batchContent = $@"
@echo off
timeout /t 2 /nobreak > nul
copy /y ""{tempPath}"" ""{currentExe}""
start """" ""{currentExe}""
del ""{tempPath}""
del ""%~f0""
";
                    await File.WriteAllTextAsync(batchPath, batchContent);
                    
                    await ShowMessage(t[21], $"{t[22]}");
                    
                    var psi = new ProcessStartInfo
                    {
                        FileName = batchPath,
                        UseShellExecute = true,
                        CreateNoWindow = true
                    };
                    Process.Start(psi);
                    Close();
                }
                else
                {
                    await ShowMessage(t[35], $"{t[31]}: No exe found in release");
                }
            }
            else
            {
                await ShowMessage(t[35], $"{t[29]}: {_version}\n{t[30]}");
            }
        }
        catch
        {
            await ShowMessage(t[35], $"{t[31]}\n{t[32]}");
        }
    }

    private Task<bool> ShowConfirmDialog(string title, string message)
    {
        var t = _lang[_currentLang == 0 ? "zh" : "en"];
        var tcs = new TaskCompletionSource<bool>();
        
        var dialog = new Window
        {
            Title = title,
            Width = 360,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var btnOk = new Button { Content = t[33], Width = 80, Height = 30, HorizontalAlignment = HorizontalAlignment.Center, HorizontalContentAlignment = HorizontalAlignment.Center };
        var btnCancel = new Button { Content = t[34], Width = 80, Height = 30, HorizontalAlignment = HorizontalAlignment.Center, HorizontalContentAlignment = HorizontalAlignment.Center };

        btnOk.Click += (_, _) => { dialog.Close(); tcs.TrySetResult(true); };
        btnCancel.Click += (_, _) => { dialog.Close(); tcs.TrySetResult(false); };

        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 15,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Center, Children = { btnOk, btnCancel } }
            }
        };

        dialog.Content = panel;
        dialog.ShowDialog(this);
        
        return tcs.Task;
    }
}
