// SPDX-License-Identifier: GPL-2.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace DriftZipApp.Pages;

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

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var files = await picker.PickMultipleFilesAsync();
        if (files == null || files.Count == 0) return;

        _files.Clear();
        foreach (var f in files) _files.Add(f.Path);

        FilesText.Text = string.Join(Environment.NewLine, _files);
    }

    private async void SelectOutput_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileSavePicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
        picker.SuggestedFileName = "archive";
        picker.FileTypeChoices.Add("7z", new[] { ".7z" });
        picker.FileTypeChoices.Add("zip", new[] { ".zip" });
        picker.FileTypeChoices.Add("tar", new[] { ".tar" });
        picker.FileTypeChoices.Add("gz", new[] { ".gz" });
        picker.FileTypeChoices.Add("bz2", new[] { ".bz2" });
        picker.FileTypeChoices.Add("xz", new[] { ".xz" });

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSaveFileAsync();
        if (file != null) OutputPath.Text = file.Path;
    }

    private async void Compress_Click(object sender, RoutedEventArgs e)
    {
        if (_files.Count == 0) { StatusTextBlock.Text = "请先选择要压缩的文件"; return; }

        var outputPath = OutputPath.Text;
        if (string.IsNullOrWhiteSpace(outputPath)) { StatusTextBlock.Text = "请指定输出路径"; return; }

        var options = BuildOptions();

        StatusTextBlock.Text = "正在压缩...";
        ShowProgress();
        try
        {
            var engine = new DriftZipEngine();
            var progress = new Progress<int>(pct => OperationProgress.Value = pct);
            await Task.Run(() => engine.Compress(options, outputPath, progress, CancellationToken.None));
            StatusTextBlock.Text = $"压缩完成：{Path.GetFileName(outputPath)}";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"压缩失败：{ex.Message}";
            Logger.Error($"压缩失败：{ex.Message}", "CompressPage");
        }
        finally { HideProgress(); }
    }

    private void ShowProgress() { OperationProgress.Visibility = Visibility.Visible; OperationProgress.Value = 0; }
    private void HideProgress() { OperationProgress.Visibility = Visibility.Collapsed; OperationProgress.Value = 0; }

    private CompressionOptions BuildOptions()
    {
        string formatContent = FormatCombo.SelectedItem is ComboBoxItem f ? f.Content?.ToString() ?? "7z" : "7z";
        var format = formatContent.ToLowerInvariant() switch
        {
            "zip" => OutputFormat.Zip,
            "tar" => OutputFormat.Tar,
            "gz"  => OutputFormat.GZip,
            "bz2" => OutputFormat.BZip2,
            "xz"  => OutputFormat.XZ,
            _     => OutputFormat.SevenZip,
        };

        string levelContent = LevelCombo.SelectedItem is ComboBoxItem l ? l.Content?.ToString() ?? "5 - 正常" : "5 - 正常";
        var level = ParseCompressionLevel(levelContent);

        long.TryParse(VolumeSizeBox.Text, out long volMb);
        long.TryParse(DictSizeBox.Text, out long dictKb);
        long.TryParse(SolidBlockBox.Text, out long solidKb);

        return new CompressionOptions
        {
            Format = format,
            Level  = level,
            Solid          = SolidCheck.IsChecked == true,
            MultiThreaded  = MultiThreadCheck.IsChecked == true,
            Encrypt        = EncryptCheck.IsChecked == true,
            Password       = EncryptCheck.IsChecked == true ? PasswordBox.Password : null,
            EncryptHeaders = EncryptHeadersCheck.IsChecked == true,
            VolumeSize     = volMb > 0 ? volMb * 1024 * 1024 : 0,
            DictionarySize = dictKb > 0 ? dictKb * 1024 : 0,
            SolidBlockSize = solidKb > 0 ? solidKb * 1024 : 0,
            CreateSfx      = SfxCheck.IsChecked == true,
            Comment        = string.IsNullOrWhiteSpace(CommentBox.Text) ? null : CommentBox.Text,
            SourceFiles    = _files.ToList(),
        };
    }

    private static CompressionLevel ParseCompressionLevel(string? text)
    {
        if (string.IsNullOrEmpty(text)) return CompressionLevel.Normal;
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
        _files.Clear();
        FilesText.Text = string.Empty;
        StatusTextBlock.Text = "已取消";
        HideProgress();
    }

    // ─────────────────────────────────────────────────────
    //  拖入文件
    // ─────────────────────────────────────────────────────
    private void CompressPage_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "拖到此处添加";
        }
    }

    private async void CompressPage_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems)) return;

        var items = await e.DataView.GetStorageItemsAsync();
        var paths = items.Select(i => i.Path).Where(p => !string.IsNullOrEmpty(p)).ToList();
        if (paths.Count == 0) return;

        foreach (var p in paths)
            if (!_files.Contains(p)) _files.Add(p);

        FilesText.Text = string.Join(Environment.NewLine, _files);
    }
}