// SPDX-License-Identifier: GPL-2.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
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

        // 追加（不去重）
        foreach (var f in files)
            if (!_files.Contains(f.Path)) _files.Add(f.Path);

        FilesList.ItemsSource = null;
        FilesList.ItemsSource = _files.ToList();
        Logger.Info($"选择文件：加了 {files.Count} 个，共 {_files.Count} 个", "CompressPage");
    }

    private async void SelectFolder_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        var folder = await picker.PickSingleFolderAsync();
        if (folder == null) return;

        if (!_files.Contains(folder.Path)) _files.Add(folder.Path);
        FilesList.ItemsSource = null;
        FilesList.ItemsSource = _files.ToList();
        Logger.Info($"选择文件夹：{folder.Path}，共 {_files.Count} 个", "CompressPage");
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
        FilesList.ItemsSource = null;
        StatusTextBlock.Text = "已取消";

        HideProgress();

    private void ClearFiles_Click(object sender, RoutedEventArgs e)
    {
        _files.Clear();
        FilesList.ItemsSource = null;
        FilesList.ItemsSource = _files.ToList();
        Logger.Info("清空文件列表", "CompressPage");
    }
    }

    private void RemoveFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string path) return;
        _files.Remove(path);
        FilesList.ItemsSource = null;
        FilesList.ItemsSource = _files.ToList();
        Logger.Info($"移除：{path}，剩 {_files.Count} 个", "CompressPage");
    }

    // ─────────────────────────────────────────────────────
    //  拖入（多文件 + 多文件夹）
    // ─────────────────────────────────────────────────────
    private void CompressPage_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "拖到此处添加";
        }
    }

    private async void CompressPage_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;

        var items = await e.DataView.GetStorageItemsAsync();
        if (items.Count == 0) return;

        int added = 0;
        foreach (var item in items)
        {
            var path = item.Path;
            if (string.IsNullOrEmpty(path)) continue;
            if (_files.Contains(path)) continue;
            _files.Add(path);
            added++;
        }

        FilesList.ItemsSource = null;
        FilesList.ItemsSource = _files.ToList();
        Logger.Info($"拖入：加了 {added} 个，共 {_files.Count} 个", "CompressPage");
    }
}
