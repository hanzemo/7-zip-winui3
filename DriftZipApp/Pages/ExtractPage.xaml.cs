// SPDX-License-Identifier: GPL-2.0-or-later
//
// ExtractPage.xaml.cs

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

public sealed partial class ExtractPage : Page
{
    private readonly List<string> _archives = new();
    private string _outputPath = string.Empty;

    public ExtractPage()
    {
        this.InitializeComponent();
        Logger.Info("ExtractPage 初始化完成", "ExtractPage");
    }

    private async void SelectArchive_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker
        {
            ViewMode = PickerViewMode.List,
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
        };
        foreach (var ext in new[] { ".7z", ".zip", ".rar", ".tar", ".gz", ".bz2", ".xz" })
            picker.FileTypeFilter.Add(ext);

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var files = await picker.PickMultipleFilesAsync();
        if (files == null || files.Count == 0) return;

        foreach (var f in files)
            if (!_archives.Contains(f.Path)) _archives.Add(f.Path);

        RefreshList();
        Logger.Info($"选择压缩包：加了 {files.Count} 个，共 {_archives.Count} 个", "ExtractPage");
    }

    private async void SelectOutput_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        var folder = await picker.PickSingleFolderAsync();
        if (folder != null)
        {
            _outputPath = folder.Path;
            ExtractToBox.Text = _outputPath;
        }
    }

    private void RemoveArchive_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string path) return;
        _archives.Remove(path);
        RefreshList();
    }

    private void ClearArchives_Click(object sender, RoutedEventArgs e)
    {
        _archives.Clear();
        RefreshList();
        Logger.Info("清空压缩包列表", "ExtractPage");
    }

    private void RefreshList()
    {
        ArchivesList.ItemsSource = null;
        ArchivesList.ItemsSource = _archives.ToList();
    }

    private async void Extract_Click(object sender, RoutedEventArgs e)
    {
        if (_archives.Count == 0) { StatusTextBlock.Text = "请先选择压缩包"; return; }
        if (string.IsNullOrEmpty(_outputPath)) { StatusTextBlock.Text = "请选择解压目录"; return; }

        var password = string.IsNullOrEmpty(PasswordBox.Password) ? null : PasswordBox.Password;
        var separate = (ModeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() == "Separate";

        StatusTextBlock.Text = "正在解压...";
        ShowProgress();

        var engine = new DriftZipEngine();
        int ok = 0, fail = 0;

        try
        {
            foreach (var archive in _archives.ToList())
            {
                try
                {
                    var dest = separate
                        ? Path.Combine(_outputPath, Path.GetFileNameWithoutExtension(archive))
                        : _outputPath;

                    Logger.Info($"解压：{archive} → {dest}", "ExtractPage");

                    await Task.Run(() => engine.Extract(archive, dest, password, null, CancellationToken.None));
                    ok++;
                }
                catch (Exception ex)
                {
                    fail++;
                    Logger.Warn($"解压 {archive} 失败：{ex.Message}", "ExtractPage");
                }
            }
            StatusTextBlock.Text = $"解压完成：{ok} 成功，{fail} 失败";
        }
        finally { HideProgress(); }
    }

    private void ShowProgress() { OperationProgress.Visibility = Visibility.Visible; OperationProgress.IsIndeterminate = true; }
    private void HideProgress() { OperationProgress.Visibility = Visibility.Collapsed; OperationProgress.IsIndeterminate = false; }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _archives.Clear();
        _outputPath = string.Empty;
        RefreshList();
        ExtractToBox.Text = string.Empty;
        PasswordBox.Password = string.Empty;
        StatusTextBlock.Text = "已取消";
        HideProgress();
    }

    private void ExtractPage_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "拖到此处添加压缩包";
        }
    }

    private async void ExtractPage_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;

        var items = await e.DataView.GetStorageItemsAsync();
        int added = 0;
        foreach (var item in items)
        {
            var path = item.Path;
            if (string.IsNullOrEmpty(path)) continue;
            if (_archives.Contains(path)) continue;
            _archives.Add(path);
            added++;
        }
        RefreshList();
        Logger.Info($"拖入：加了 {added} 个，共 {_archives.Count} 个", "ExtractPage");
    }
}