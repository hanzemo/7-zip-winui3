// SPDX-License-Identifier: GPL-2.0-or-later
//
// CompressPage.xaml.cs

using WinRT.Interop;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace SevenZipApp.Pages;

public sealed partial class CompressPage : Page
{
    private readonly List<string> _files = new();

    public CompressPage()
    {
        this.InitializeComponent();

        Logger.Info("CompressPage 初始化完成", "CompressPage");
    }

    private async void SelectFiles_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker
        {
            ViewMode = PickerViewMode.List,
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
        };
        picker.FileTypeFilter.Add("*");

        // ✅ 绑定当前窗口
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var files = await picker.PickMultipleFilesAsync();
        if (files == null || files.Count == 0) return;

        _files.Clear();
        foreach (var f in files)
            _files.Add(f.Path);

        FilesText.Text = string.Join(Environment.NewLine, _files);
        Logger.Info($"选择了 {files.Count} 个文件", "CompressPage");
    }

    private async void SelectOutput_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
        };
        picker.SuggestedFileName = "archive";
        picker.FileTypeChoices.Add("7z", new[] { ".7z" });
        picker.FileTypeChoices.Add("zip", new[] { ".zip" });
        picker.FileTypeChoices.Add("tar", new[] { ".tar" });
        picker.FileTypeChoices.Add("gz", new[] { ".gz" });
        picker.FileTypeChoices.Add("bz2", new[] { ".bz2" });
        picker.FileTypeChoices.Add("xz", new[] { ".xz" });

        // ✅ 绑定当前窗口
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSaveFileAsync();
        if (file != null)
        {
            OutputPath.Text = file.Path;
        }
    }

    private async void Compress_Click(object sender, RoutedEventArgs e)
    {
        if (_files.Count == 0)
        {
            StatusTextBlock.Text = "请先选择要压缩的文件";
            return;
        }

        var outputPath = OutputPath.Text;
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            StatusTextBlock.Text = "请指定输出路径";
            return;
        }

        var options = BuildOptions();

        StatusTextBlock.Text = "正在压缩...";
        Logger.Info($"开始压缩 {_files.Count} 个文件 -> {outputPath}", "CompressPage");

        try
        {
            var engine = new SevenZipEngine();
            await Task.Run(() => engine.Compress(options, outputPath, null, CancellationToken.None));
            StatusTextBlock.Text = $"压缩完成：{Path.GetFileName(outputPath)}";
            Logger.Info("压缩操作完成", "CompressPage");
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"压缩失败：{ex.Message}";
            Logger.Error($"压缩失败：{ex.Message}", "CompressPage");
        }
    }

    private CompressionOptions BuildOptions()
    {
        string formatContent = FormatCombo.SelectedItem is ComboBoxItem fmtItem ? fmtItem.Content?.ToString() ?? "7z" : "7z";
        var format = formatContent?.ToLowerInvariant() switch
        {
            "zip" => OutputFormat.Zip,
            "tar" => OutputFormat.Tar,
            "gz"  => OutputFormat.GZip,
            "bz2" => OutputFormat.BZip2,
            "xz"  => OutputFormat.XZ,
            _     => OutputFormat.SevenZip,
        };

        string levelContent = LevelCombo.SelectedItem is ComboBoxItem lvItem
            ? lvItem.Content?.ToString() ?? "5 - 正常"
            : "5 - 正常";
        var level = ParseCompressionLevel(levelContent);

        return new CompressionOptions
        {
            Format = format,
            Level  = level,
            Solid          = SolidCheck.IsChecked == true,
            MultiThreaded  = MultiThreadCheck.IsChecked == true,
            Encrypt        = EncryptCheck.IsChecked == true,
            Password       = EncryptCheck.IsChecked == true ? PasswordBox.Password : null,
            SourceFiles    = _files.ToList(),
        };
    }

    private static CompressionLevel ParseCompressionLevel(string? text)
    {
        // ComboBox 项内容形如 "5 - 正常"，取第一段数字。
        if (string.IsNullOrEmpty(text))
            return CompressionLevel.Normal;

        var parts = text.Split('-', 2);
        if (parts.Length > 0 && int.TryParse(parts[0].Trim(), out int n))
        {
            return n switch
            {
                0 => CompressionLevel.Store,
                1 => CompressionLevel.Fastest,
                3 => CompressionLevel.Fast,
                5 => CompressionLevel.Normal,
                7 => CompressionLevel.Maximum,
                9 => CompressionLevel.Ultra,
                _ => CompressionLevel.Normal,
            };
        }
        return CompressionLevel.Normal;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Logger.Info("取消压缩", "CompressPage");
        _files.Clear();
        FilesText.Text = string.Empty;
        StatusTextBlock.Text = "已取消";
    }
}
