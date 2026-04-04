# DbcTools.CrossPlatform 实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 创建一个基于 Avalonia UI 的跨平台 DBC 转换工具，支持 Windows/Linux/macOS。

**Architecture:** 在现有 DbcTools 解决方案中新建 DbcTools.CrossPlatform 工程，使用 Avalonia UI 11.x 框架。从现有 WinForms 工程复制核心代码（Parser/Model/Writer），调整命名空间后独立维护。UI 使用 AXAML 声明式布局。

**Tech Stack:** .NET 9.0, Avalonia UI 11.x, AXAML, IStorageProvider (文件对话框), ContentDialog (消息框)

---

### Task 1: 创建 Avalonia 工程并添加到解决方案

**Files:**
- Create: `DbcTools.CrossPlatform/DbcTools.CrossPlatform.csproj`
- Create: `DbcTools.CrossPlatform/Program.cs`
- Create: `DbcTools.CrossPlatform/App.axaml`
- Create: `DbcTools.CrossPlatform/App.axaml.cs`
- Modify: `DbcTools.sln` (添加新工程引用)

- [ ] **Step 1: 使用 dotnet new 创建 Avalonia 工程**

Run:
```bash
dotnet new avalonia.app -n DbcTools.CrossPlatform -o DbcTools.CrossPlatform --framework net9.0
```

- [ ] **Step 2: 将新工程添加到解决方案**

Run:
```bash
dotnet sln DbcTools.sln add DbcTools.CrossPlatform/DbcTools.CrossPlatform.csproj
```

- [ ] **Step 3: 验证解决方案能编译**

Run:
```bash
dotnet build DbcTools.sln
```
Expected: Build succeeded, 0 errors

- [ ] **Step 4: 调整 Program.cs — 注册编码提供程序**

将 `DbcTools.CrossPlatform/Program.cs` 替换为：

```csharp
using Avalonia;
using System.Text;

namespace DbcTools.CrossPlatform;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
```

- [ ] **Step 5: 验证编译通过**

Run:
```bash
dotnet build DbcTools.CrossPlatform/DbcTools.CrossPlatform.csproj
```
Expected: Build succeeded

---

### Task 2: 复制并调整核心代码（Models/Parsers/Writers）

**Files:**
- Create: `DbcTools.CrossPlatform/Models/DbcSignal.cs`
- Create: `DbcTools.CrossPlatform/Parsers/DbcParser.cs`
- Create: `DbcTools.CrossPlatform/Writers/BinWriter.cs`

- [ ] **Step 1: 创建 Models/DbcSignal.cs**

```csharp
namespace DbcTools.CrossPlatform.Models;

public class DbcSignal
{
    public string Name { get; set; } = string.Empty;
    public int StartBit { get; set; }
    public int Length { get; set; }
    public double Factor { get; set; }
    public double Offset { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string ByteOrder { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
    public string MessageName { get; set; } = string.Empty;
    public uint MessageId { get; set; }
    public string MessageIdHex => $"0x{(MessageId & 0x1FFFFFFF):X}";
    public int MessageDlc { get; set; }
    public bool IsExtendedFrame { get; set; }
}
```

- [ ] **Step 2: 创建 Parsers/DbcParser.cs**

从 `DbcTools/Parsers/DbcParser.cs` 复制全部代码，仅修改命名空间为 `DbcTools.CrossPlatform.Parsers`，`using` 中 Model 的引用改为 `DbcTools.CrossPlatform.Models`。

```csharp
using System.Text;
using System.Text.RegularExpressions;
using DbcTools.CrossPlatform.Models;

namespace DbcTools.CrossPlatform.Parsers;

public static class DbcParser
{
    private static uint ParseMessageId(string raw)
    {
        if (raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return Convert.ToUInt32(raw[2..], 16);
        return uint.Parse(raw);
    }

    private static string[] ReadLinesWithEncoding(string filePath)
    {
        var rawBytes = File.ReadAllBytes(filePath);
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        try
        {
            var text = utf8.GetString(rawBytes);
            return text.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        }
        catch (DecoderFallbackException)
        {
            var gbk = Encoding.GetEncoding("GBK");
            var text = gbk.GetString(rawBytes);
            return text.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        }
    }

    public static List<DbcSignal> Parse(string filePath)
    {
        var signals = new List<DbcSignal>();
        var lines = ReadLinesWithEncoding(filePath);

        uint currentMsgId = 0;
        string currentMsgName = string.Empty;
        int currentMsgDlc = 0;
        bool currentIsExtended = false;

        var boPattern = new Regex(@"^BO_\s+([0-9a-fA-FxX]+)\s+(\w+)\s*:\s*(\d+)\s+\w+");
        var sgPattern = new Regex(
            @"^\s*SG_\s+(\w+)\s*:\s*(\d+)\|(\d+)@([01])([+-])\s*\(\s*([^,]+)\s*,\s*([^)]+)\s*\)\s*\[[^\]]*\]\s*""([^""]*)""");

        foreach (var line in lines)
        {
            var boMatch = boPattern.Match(line);
            if (boMatch.Success)
            {
                currentMsgId = ParseMessageId(boMatch.Groups[1].Value.Trim());
                currentIsExtended = (currentMsgId & 0x80000000) != 0;
                currentMsgId &= 0x1FFFFFFF;
                currentMsgName = boMatch.Groups[2].Value;
                currentMsgDlc = int.Parse(boMatch.Groups[3].Value);
                continue;
            }

            var sgMatch = sgPattern.Match(line);
            if (sgMatch.Success)
            {
                var signal = new DbcSignal
                {
                    Name = sgMatch.Groups[1].Value,
                    StartBit = int.Parse(sgMatch.Groups[2].Value),
                    Length = int.Parse(sgMatch.Groups[3].Value),
                    ByteOrder = sgMatch.Groups[4].Value == "0" ? "Motorola" : "Intel",
                    ValueType = sgMatch.Groups[5].Value == "+" ? "unsigned" : "signed",
                    Factor = double.Parse(sgMatch.Groups[6].Value.Trim(), System.Globalization.CultureInfo.InvariantCulture),
                    Offset = double.Parse(sgMatch.Groups[7].Value.Trim(), System.Globalization.CultureInfo.InvariantCulture),
                    Unit = sgMatch.Groups[8].Value,
                    MessageId = currentMsgId,
                    MessageName = currentMsgName,
                    MessageDlc = currentMsgDlc,
                    IsExtendedFrame = currentIsExtended
                };
                signals.Add(signal);
                continue;
            }

            var vtMatch = Regex.Match(line, @"^VALTYPE_\s+\d+\s+(\w+)\s*:\s*(\d)");
            if (vtMatch.Success)
            {
                string sigName = vtMatch.Groups[1].Value;
                int vtCode = int.Parse(vtMatch.Groups[2].Value);
                string vtName = vtCode switch
                {
                    1 => "signed",
                    2 => "IEEE float",
                    3 => "IEEE double",
                    _ => "unsigned"
                };
                var target = signals.FindLast(s => s.Name == sigName);
                if (target != null) target.ValueType = vtName;
            }
        }

        return signals;
    }
}
```

- [ ] **Step 3: 创建 Writers/BinWriter.cs**

从 `DbcTools/Writers/BinWriter.cs` 复制全部代码，仅修改命名空间为 `DbcTools.CrossPlatform.Writers`，`using` 中 Model 的引用改为 `DbcTools.CrossPlatform.Models`。

```csharp
using System.Text;
using DbcTools.CrossPlatform.Models;

namespace DbcTools.CrossPlatform.Writers;

public static class BinWriter
{
    private const string MagicNumber = "ScudPower";

    public static void Write(string outputPath, List<DbcSignal> signals)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteString(bw, MagicNumber, 9);
        var reserved = new byte[19];
        bw.Write(reserved);
        bw.Write((ushort)0);
        bw.Write((ushort)signals.Count);

        foreach (var sig in signals)
        {
            bw.Write(sig.MessageId);
            bw.Write((byte)sig.MessageDlc);
            WriteString(bw, sig.Name, 32);
            ushort packed = (ushort)((sig.Length << 9) | (sig.StartBit & 0x1FF));
            if (sig.ByteOrder == "Intel") packed |= 0x8000;
            bw.Write(packed);
            bw.Write(sig.Factor);
            bw.Write(sig.Offset);
            WriteString(bw, sig.Unit, 16);
            byte valueType = sig.ValueType switch
            {
                "signed" => 1,
                "IEEE float" => 2,
                "IEEE double" => 3,
                _ => 0
            };
            byte flags = valueType;
            if (sig.IsExtendedFrame) flags |= 0x04;
            bw.Write(flags);
            bw.Write((byte)0);
        }

        bw.Flush();
        var data = ms.ToArray();

        ushort crc = Crc16(data, 30, data.Length - 30);
        data[28] = (byte)(crc & 0xFF);
        data[29] = (byte)((crc >> 8) & 0xFF);

        File.WriteAllBytes(outputPath, data);
    }

    private static ushort Crc16(byte[] data, int offset, int length)
    {
        ushort crc = 0xFFFF;
        for (int i = offset; i < offset + length; i++)
        {
            crc ^= data[i];
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 0x0001) != 0)
                    crc = (ushort)((crc >> 1) ^ 0xA001);
                else
                    crc >>= 1;
            }
        }
        return crc;
    }

    private static void WriteString(BinaryWriter bw, string value, int fixedLength)
    {
        var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
        var buffer = new byte[fixedLength];
        int copyLen = Math.Min(bytes.Length, fixedLength);
        Array.Copy(bytes, buffer, copyLen);
        bw.Write(buffer);
    }
}
```

- [ ] **Step 4: 验证编译通过**

Run:
```bash
dotnet build DbcTools.CrossPlatform/DbcTools.CrossPlatform.csproj
```
Expected: Build succeeded

---

### Task 3: 创建主窗口 UI（MainWindow.axaml）

**Files:**
- Create: `DbcTools.CrossPlatform/Views/MainWindow.axaml`
- Create: `DbcTools.CrossPlatform/Views/MainWindow.axaml.cs`

- [ ] **Step 1: 创建 Views 目录**

Run:
```bash
mkdir DbcTools.CrossPlatform\Views
```

- [ ] **Step 2: 创建 MainWindow.axaml**

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="DbcTools.CrossPlatform.Views.MainWindow"
        Title="ScudPower DBC 转换工具 (跨平台版) 1.0.0"
        Width="920" Height="520"
        MinWidth="640" MinHeight="400"
        WindowStartupLocation="CenterScreen">

    <Grid RowDefinitions="Auto,*,Auto">
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="10,10,10,5">
            <Button Name="BtnOpen" Content="打开文件" Width="100" Height="32" Margin="0,0,10,0"/>
            <Button Name="BtnGenerate" Content="生成" Width="100" Height="32" IsEnabled="False"/>
        </StackPanel>

        <DataGrid Grid.Row="1" Name="DgvSignals" Margin="10,5,10,5"
                  IsReadOnly="True" AutoGenerateColumns="False"
                  SelectionMode="FullRowSelect"
                  BorderThickness="1"
                  GridLinesVisibility="All">
            <DataGrid.Columns>
                <DataGridTextColumn Header="报文名称" Binding="{Binding MessageName}" Width="*"/>
                <DataGridTextColumn Header="报文ID" Binding="{Binding MessageIdHex}" Width="80"/>
                <DataGridTextColumn Header="信号名" Binding="{Binding Name}" Width="*"/>
                <DataGridTextColumn Header="起始位" Binding="{Binding StartBit}" Width="60"/>
                <DataGridTextColumn Header="长度" Binding="{Binding Length}" Width="50"/>
                <DataGridTextColumn Header="精度" Binding="{Binding Factor}" Width="60"/>
                <DataGridTextColumn Header="偏移量" Binding="{Binding Offset}" Width="60"/>
                <DataGridTextColumn Header="单位" Binding="{Binding Unit}" Width="60"/>
                <DataGridTextColumn Header="字节顺序" Binding="{Binding ByteOrder}" Width="80"/>
                <DataGridTextColumn Header="值类型" Binding="{Binding ValueType}" Width="80"/>
            </DataGrid.Columns>
        </DataGrid>

        <Border Grid.Row="2" Background="{DynamicResource SystemControlBackgroundAltHighBrush}"
                Padding="10,6"
                BorderThickness="0,1,0,0"
                BorderBrush="{DynamicResource SystemControlForegroundBaseMediumLowBrush}">
            <TextBlock Name="LblStatus" Text="就绪" VerticalAlignment="Center"/>
        </Border>
    </Grid>
</Window>
```

- [ ] **Step 3: 创建 MainWindow.axaml.cs**

```csharp
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using DbcTools.CrossPlatform.Models;
using DbcTools.CrossPlatform.Parsers;
using DbcTools.CrossPlatform.Writers;

namespace DbcTools.CrossPlatform.Views;

public partial class MainWindow : Window
{
    private string? _dbcFilePath;
    private List<DbcSignal> _signals = [];

    public MainWindow()
    {
        InitializeComponent();
        BtnOpen.Click += BtnOpen_Click;
        BtnGenerate.Click += BtnGenerate_Click;
    }

    private async void BtnOpen_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择DBC文件",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("DBC文件") { Patterns = ["*.dbc"] },
                new FilePickerFileType("所有文件") { Patterns = ["*.*"] }
            ]
        });

        if (files.Count == 0) return;

        _dbcFilePath = files[0].Path.LocalPath;
        LblStatus.Text = $"已打开: {_dbcFilePath}";

        try
        {
            _signals = DbcParser.Parse(_dbcFilePath);
            DgvSignals.ItemsSource = _signals;
            BtnGenerate.IsEnabled = _signals.Count > 0;
            LblStatus.Text = $"已打开: {_dbcFilePath} | 解析到 {_signals.Count} 个信号";
        }
        catch (Exception ex)
        {
            var dialog = new ContentDialog
            {
                Title = "错误",
                Content = $"解析DBC文件失败:\n{ex.Message}",
                CloseButtonText = "确定"
            };
            await dialog.ShowAsync();
        }
    }

    private async void BtnGenerate_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_dbcFilePath) || _signals.Count == 0) return;

        var defaultName = Path.ChangeExtension(Path.GetFileName(_dbcFilePath), ".bin");
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "保存BIN文件",
            SuggestedFileName = defaultName,
            FileTypeChoices =
            [
                new FilePickerFileType("二进制文件") { Patterns = ["*.bin"] }
            ]
        });

        if (file == null) return;

        try
        {
            BinWriter.Write(file.Path.LocalPath, _signals);
            LblStatus.Text = $"已生成: {file.Path.LocalPath}";
            var dialog = new ContentDialog
            {
                Title = "完成",
                Content = $"成功生成 {file.Path.LocalPath}",
                CloseButtonText = "确定"
            };
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            var dialog = new ContentDialog
            {
                Title = "错误",
                Content = $"生成BIN文件失败:\n{ex.Message}",
                CloseButtonText = "确定"
            };
            await dialog.ShowAsync();
        }
    }
}
```

- [ ] **Step 4: 更新 App.axaml.cs 使其指向 MainWindow**

将 `DbcTools.CrossPlatform/App.axaml.cs` 替换为：

```csharp
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DbcTools.CrossPlatform.Views;

namespace DbcTools.CrossPlatform;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
```

- [ ] **Step 5: 清理模板生成的多余文件**

删除模板生成的默认 MainWindow（如果存在）：
- 删除 `DbcTools.CrossPlatform/MainWindow.axaml`（模板生成的默认文件）
- 删除 `DbcTools.CrossPlatform/MainWindow.axaml.cs`（模板生成的默认文件）

Run:
```bash
Remove-Item -Path "DbcTools.CrossPlatform\MainWindow.axaml" -ErrorAction SilentlyContinue
Remove-Item -Path "DbcTools.CrossPlatform\MainWindow.axaml.cs" -ErrorAction SilentlyContinue
```

- [ ] **Step 6: 验证编译通过**

Run:
```bash
dotnet build DbcTools.CrossPlatform/DbcTools.CrossPlatform.csproj
```
Expected: Build succeeded, 0 errors

---

### Task 4: 运行并验证

- [ ] **Step 1: 在 Windows 上运行测试**

Run:
```bash
dotnet run --project DbcTools.CrossPlatform/DbcTools.CrossPlatform.csproj
```
Expected: 窗口正常启动，显示"打开文件"和"生成"按钮、空表格、底部"就绪"状态栏

- [ ] **Step 2: 功能验证清单**

手动验证：
1. 点击"打开文件" → 弹出文件选择对话框，默认只显示 .dbc 文件
2. 选择一个 .dbc 文件 → DataGrid 填充信号数据，状态栏显示路径和信号数量
3. 点击"生成" → 弹出保存对话框，默认文件名正确
4. 保存后 → 状态栏显示生成路径，弹出成功对话框
5. 窗口可拖拽调整大小，DataGrid 自动跟随

- [ ] **Step 3: 编译整个解决方案确认无冲突**

Run:
```bash
dotnet build DbcTools.sln
```
Expected: Build succeeded, 两个工程都编译通过，0 errors
