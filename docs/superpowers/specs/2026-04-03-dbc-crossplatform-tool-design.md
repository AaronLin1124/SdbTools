# DbcTools.CrossPlatform 设计文档

## 概述

在现有 DbcTools 解决方案中添加一个新的跨平台工程，使用 Avalonia UI 框架，支持 Windows/Linux/macOS 三大平台。功能与现有 WinForms 版本一致：打开 DBC 文件、解析信号参数、生成 BIN 文件。

## 需求

### 功能需求

1. UI 界面包含：
   - 顶部按钮区：`打开文件`、`生成` 两个按钮
   - 中部 DataGrid：显示解析后的信号列表（报文名称、报文ID、信号名、起始位、长度、精度、偏移量、单位、字节顺序、值类型）
   - 底部状态栏：显示当前文件路径和操作状态

2. 打开文件功能：
   - 点击 `打开文件` 弹出文件选择对话框
   - 只允许选择 `.dbc` 文件
   - 解析 DBC 文件内容，提取信号参数
   - 在 DataGrid 中显示所有信号
   - 状态栏显示文件路径和信号数量

3. 生成功能：
   - 点击 `生成` 弹出保存对话框
   - 默认文件名为原 DBC 文件名，扩展名改为 `.bin`
   - 将信号数据写入二进制文件
   - 状态栏显示生成成功路径

### 非功能需求

- 跨平台支持：Windows、Linux、macOS
- 独立维护：新工程代码与现有 WinForms 工程独立，复制核心代码而非共享类库
- 工程名称：`DbcTools.CrossPlatform`
- 与现有工程并行存在于同一解决方案

## 架构设计

### 项目结构

```
dbc_Tools/
├── DbcTools.sln
├── DbcTools/                       (现有 WinForms 工程，保持不变)
├── DbcTools.CrossPlatform/         (新工程)
│   ├── DbcTools.CrossPlatform.csproj
│   ├── Program.cs                  (应用入口)
│   ├── App.axaml                   (应用资源)
│   ├── App.axaml.cs
│   ├── Views/
│   │   ├── MainWindow.axaml        (主窗口 UI)
│   │   └── MainWindow.axaml.cs     (主窗口逻辑)
│   ├── Models/
│   │   └── DbcSignal.cs            (从 DbcTools 复制)
│   ├── Parsers/
│   │   └── DbcParser.cs            (从 DbcTools 复制)
│   └── Writers/
│       └── BinWriter.cs            (从 DbcTools 复制)
└── doc/
```

### 技术栈

| 组件 | 选型 | 说明 |
|------|------|------|
| UI 框架 | Avalonia UI 11.x | 跨平台 XAML 框架 |
| 目标框架 | .NET 9.0 | 无 `-windows` 后缀 |
| UI 标记 | AXAML | Avalonia 的 XAML 变体 |
| 文件对话框 | IStorageProvider | Avalonia 内置原生对话框 API |
| 消息对话框 | ContentDialog | Avalonia 内置 ContentControl |
| DataGrid | Avalonia.Controls.DataGrid | Avalonia 11 内置 |

### 核心模块

#### Models/DbcSignal.cs

从现有工程复制，保持结构不变：

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

#### Parsers/DbcParser.cs

从现有工程复制，调整命名空间：

```csharp
namespace DbcTools.CrossPlatform.Parsers;

public static class DbcParser
{
    public static List<DbcSignal> Parse(string filePath) { ... }
}
```

解析逻辑保持不变：
- 支持 UTF-8 和 GBK 编码自动检测
- 解析 `BO_` 报文定义行
- 解析 `SG_` 信号定义行
- 解析 `VALTYPE_` 值类型定义行

#### Writers/BinWriter.cs

从现有工程复制，调整命名空间：

```csharp
namespace DbcTools.CrossPlatform.Writers;

public static class BinWriter
{
    public static void Write(string outputPath, List<DbcSignal> signals) { ... }
}
```

二进制格式保持不变：
- 9 字节魔数 "ScudPower"
- 19 字节保留
- 2 字节 CRC16 校验（初始化为 0）
- 2 字节信号数量
- 每个信号固定长度结构

#### Views/MainWindow.axaml

UI 布局：

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="ScudPower DBC 转换工具 (跨平台版) 1.0.0"
        Width="920" Height="520"
        MinWidth="640" MinHeight="400"
        WindowStartupLocation="CenterScreen">
    
    <Grid RowDefinitions="Auto,*,Auto">
        <!-- 顶部按钮栏 -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="10">
            <Button Name="BtnOpen" Content="打开文件" Width="100" Height="32" Margin="0,0,10,0"/>
            <Button Name="BtnGenerate" Content="生成" Width="100" Height="32" IsEnabled="False"/>
        </StackPanel>
        
        <!-- DataGrid -->
        <DataGrid Grid.Row="1" Name="DgvSignals" Margin="10,0"
                  IsReadOnly="True" AutoGenerateColumns="False"
                  SelectionMode="FullRowSelect">
            <DataGrid.Columns>
                <DataGridTextColumn Header="报文名称" Binding="{Binding MessageName}"/>
                <DataGridTextColumn Header="报文ID" Binding="{Binding MessageIdHex}"/>
                <DataGridTextColumn Header="信号名" Binding="{Binding Name}"/>
                <DataGridTextColumn Header="起始位" Binding="{Binding StartBit}"/>
                <DataGridTextColumn Header="长度" Binding="{Binding Length}"/>
                <DataGridTextColumn Header="精度" Binding="{Binding Factor}"/>
                <DataGridTextColumn Header="偏移量" Binding="{Binding Offset}"/>
                <DataGridTextColumn Header="单位" Binding="{Binding Unit}"/>
                <DataGridTextColumn Header="字节顺序" Binding="{Binding ByteOrder}"/>
                <DataGridTextColumn Header="值类型" Binding="{Binding ValueType}"/>
            </DataGrid.Columns>
        </DataGrid>
        
        <!-- 状态栏 -->
        <Border Grid.Row="2" Background="{DynamicResource SystemControlBackgroundAltHighBrush}"
                Padding="10,6" BorderThickness="0,1,0,0"
                BorderBrush="{DynamicResource SystemControlForegroundBaseMediumLowBrush}">
            <TextBlock Name="LblStatus" Text="就绪" VerticalAlignment="Center"/>
        </Border>
    </Grid>
</Window>
```

#### Views/MainWindow.axaml.cs

主窗口逻辑：

```csharp
namespace DbcTools.CrossPlatform.Views;

public partial class MainWindow : Window
{
    private string? _dbcFilePath;
    private List<DbcSignal> _signals = new();

    public MainWindow()
    {
        InitializeComponent();
        BtnOpen.Click += BtnOpen_Click;
        BtnGenerate.Click += BtnGenerate_Click;
    }

    private async void BtnOpen_Click(object? sender, RoutedEventArgs e)
    {
        var storage = StorageProvider;
        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择DBC文件",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("DBC文件") { Patterns = new[] { "*.dbc" } },
                new FilePickerFileType("所有文件") { Patterns = new[] { "*.*" } }
            }
        });

        var file = files.FirstOrDefault();
        if (file == null) return;

        _dbcFilePath = file.Path.LocalPath;
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
            await dialog.ShowAsync(this);
        }
    }

    private async void BtnGenerate_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_dbcFilePath) || _signals.Count == 0) return;

        var defaultName = Path.ChangeExtension(Path.GetFileName(_dbcFilePath), ".bin");
        var storage = StorageProvider;
        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "保存BIN文件",
            SuggestedFileName = defaultName,
            FileTypeChoices = new[]
            {
                new FilePickerFileType("二进制文件") { Patterns = new[] { "*.bin" } }
            }
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
            await dialog.ShowAsync(this);
        }
        catch (Exception ex)
        {
            var dialog = new ContentDialog
            {
                Title = "错误",
                Content = $"生成BIN文件失败:\n{ex.Message}",
                CloseButtonText = "确定"
            };
            await dialog.ShowAsync(this);
        }
    }
}
```

## 数据流

```
[用户点击打开文件]
       ↓
[StorageProvider.OpenFilePickerAsync]
       ↓
[DbcParser.Parse(file_path)]
       ↓
[List<DbcSignal>]
       ↓
[DataGrid.ItemsSource = signals]
       ↓
[状态栏显示路径和信号数]

[用户点击生成]
       ↓
[StorageProvider.SaveFilePickerAsync]
       ↓
[BinWriter.Write(output_path, signals)]
       ↓
[状态栏显示生成成功]
```

## 错误处理

1. **DBC 文件解析失败**：捕获异常，弹出错误对话框，状态栏保持不变
2. **BIN 文件写入失败**：捕获异常，弹出错误对话框，状态栏保持不变
3. **文件选择取消**：不做任何操作，保持当前状态

## 发布策略

使用 `dotnet publish` 按平台分别打包：

```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained

# Linux
dotnet publish -c Release -r linux-x64 --self-contained

# macOS
dotnet publish -c Release -r osx-x64 --self-contained
dotnet publish -c Release -r osx-arm64 --self-contained
```

## 与现有工程的关系

- **并行存在**：`DbcTools` (WinForms) 和 `DbcTools.CrossPlatform` (Avalonia) 在同一解决方案中独立存在
- **独立维护**：核心代码复制到新工程，命名空间调整，不共享类库
- **功能一致**：两个工程提供完全相同的功能，只是 UI 技术栈不同

## 成功标准

1. 工程能在 Windows/Linux/macOS 上成功编译和运行
2. 能打开 DBC 文件并正确解析所有信号参数
3. DataGrid 正确显示所有信号信息
4. 能生成符合现有格式规范的 BIN 文件
5. 状态栏正确显示文件路径和操作状态
6. 添加到现有解决方案并能成功编译
