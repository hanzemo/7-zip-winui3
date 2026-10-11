// SPDX-License-Identifier: GPL-2.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using SharpSevenZip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace SevenZipApp.Pages;

public sealed partial class ArchivePreviewPage : Page
{
    private string? _archivePath;
    private List<ArchiveEntry> _allEntries = new();
    private string _currentPath = "";

    public ArchivePreviewPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string path && File.Exists(path))
            LoadArchive(path);
    }

    private void LoadArchive(string path)
    {
        _archivePath = path;
        ArchivePathText.Text = Path.GetFileName(path);

        try
        {
            using var extractor = new SharpSevenZipExtractor(path);
            _allEntries = extractor.ArchiveFileData
                .Select(f => new ArchiveEntry
                {
                    FullPath = f.FileName.Replace('\\', '/').TrimStart('/'),
                    Name = Path.GetFileName(f.FileName.TrimEnd('/', '\\')),
                    Size = f.Size,
                    Modified = f.LastWriteTime,
                    IsDirectory = f.IsDirectory
                        || f.FileName.EndsWith("/")
                        || f.FileName.EndsWith("\\"),
                })
                .Where(x => !string.IsNullOrEmpty(x.FullPath))
                .ToList();

            _currentPath = "";
            RefreshView();

            // 前 30 条日志（定位用）
            foreach (var e in _allEntries.Take(30))
                Logger.Info($"[Load] FullPath='{e.FullPath}' IsDir={e.IsDirectory}", "ArchivePreviewPage");
        }
        catch (Exception ex)
        {
            Logger.Error($"预览失败：{ex.Message}", "ArchivePreviewPage");
        }
    }

    private void RefreshView()
    {
        var currentLevels = _currentPath
            .Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        var result = new List<ArchiveEntry>();

        foreach (var entry in _allEntries)
        {
            var parts = entry.FullPath
                .Replace('\\', '/')
                .Trim('/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length <= currentLevels.Length) continue;

            bool match = true;
            for (int i = 0; i < currentLevels.Length; i++)
            {
                if (!string.Equals(parts[i], currentLevels[i], StringComparison.OrdinalIgnoreCase))
                {
                    match = false;
                    break;
                }
            }
            if (!match) continue;

            int depth = parts.Length - currentLevels.Length;

            if (depth == 1)
            {
                if (!result.Any(x => x.FullPath == entry.FullPath))
                    result.Add(entry);
            }
            else
            {
                var dirName = parts[currentLevels.Length];
                var dirPath = string.Join("/", parts.Take(currentLevels.Length + 1)) + "/";

                if (!result.Any(x => x.FullPath == dirPath))
                {
                    result.Add(new ArchiveEntry
                    {
                        Name = dirName,
                        FullPath = dirPath,
                        IsDirectory = true,
                        Size = 0,
                        Modified = entry.Modified,
                    });
                }
            }
        }

        var sorted = result
            .OrderBy(x => x.IsDirectory ? 0 : 1)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Logger.Info($"[Refresh] path='{_currentPath}' 显示 {sorted.Count} 项 / 共 {_allEntries.Count}", "ArchivePreviewPage");

        EntryList.ItemsSource = sorted;
        CurrentPathText.Text = string.IsNullOrEmpty(_currentPath) ? "根目录" : $"/ {_currentPath}";
        GoUpButton.Visibility = string.IsNullOrEmpty(_currentPath)
            ? Visibility.Collapsed : Visibility.Visible;
    }

    private void EntryList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (EntryList.SelectedItem is ArchiveEntry entry && entry.IsDirectory)
        {
            _currentPath = entry.FullPath.TrimEnd('/');
            Logger.Info($"[DoubleTap] 进入 '{_currentPath}'", "ArchivePreviewPage");
            RefreshView();
        }
    }

    private async void ExtractEntryTo_Click(object sender, RoutedEventArgs e)
    {
        var entries = EntryList.SelectedItems.OfType<ArchiveEntry>().Where(x => !x.IsDirectory).ToList();
        if (entries.Count == 0) return;
        if (_archivePath == null) return;

        var folder = new Windows.Storage.Pickers.FolderPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(folder, hwnd);
        folder.FileTypeFilter.Add("*");
        var target = await folder.PickSingleFolderAsync();
        if (target == null) return;

        try
        {
            using var extractor = new SharpSevenZipExtractor(_archivePath);
            foreach (var entry in entries)
            {
                var outPath = System.IO.Path.Combine(target.Path, System.IO.Path.GetFileName(entry.Name));
                using (var fs = System.IO.File.Create(outPath))
                {
                    extractor.ExtractFile(entry.FullPath, fs);
                }
            }
            Logger.Info($"解压 {entries.Count} 项到 {target.Path}", "ArchivePreviewPage");
        }
        catch (Exception ex) { Logger.Error($"解压失败: {ex.Message}", "ArchivePreviewPage"); }
    }

    private void ExtractEntryHere_Click(object sender, RoutedEventArgs e)
    {
        var entries = EntryList.SelectedItems.OfType<ArchiveEntry>().Where(x => !x.IsDirectory).ToList();
        if (entries.Count == 0) return;
        if (_archivePath == null) return;

        var targetDir = System.IO.Path.GetDirectoryName(_archivePath);
        if (string.IsNullOrEmpty(targetDir)) return;

        try
        {
            using var extractor = new SharpSevenZipExtractor(_archivePath);
            foreach (var entry in entries)
            {
                var outPath = System.IO.Path.Combine(targetDir, System.IO.Path.GetFileName(entry.Name));
                using (var fs = System.IO.File.Create(outPath))
                {
                    extractor.ExtractFile(entry.FullPath, fs);
                }
            }
        }
        catch (Exception ex) { Logger.Error($"解压失败: {ex.Message}", "ArchivePreviewPage"); }
    }

    private void CopyEntryPath_Click(object sender, RoutedEventArgs e)
    {
        var entries = EntryList.SelectedItems.OfType<ArchiveEntry>().ToList();
        if (entries.Count == 0) return;
        var text = string.Join(System.Environment.NewLine, entries.Select(x => x.FullPath));
        var dp = new Windows.ApplicationModel.DataTransfer.DataPackage();
        dp.SetText(text);
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);
    }

    private void GoUp_Click(object sender, RoutedEventArgs e)
    {
        var current = _currentPath.Trim('/');
        if (string.IsNullOrEmpty(current)) return;
        var idx = current.LastIndexOf('/');
        _currentPath = idx >= 0 ? current.Substring(0, idx) : "";
        RefreshView();
    }

    private async void EntryList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (_archivePath == null) return;

        var entries = e.Items.OfType<ArchiveEntry>().Where(x => !x.IsDirectory).ToList();
        if (entries.Count == 0) { e.Cancel = true; return; }

        var tempDir = Path.Combine(Path.GetTempPath(), "SevenZipApp_Drag", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var files = new List<StorageFile>();

        try
        {
            using var extractor = new SharpSevenZipExtractor(_archivePath);
            foreach (var entry in entries)
            {
                try
                {
                    var outPath = Path.Combine(tempDir, Path.GetFileName(entry.Name));
                    using (var fs = File.Create(outPath))
                    {
                        extractor.ExtractFile(entry.FullPath, fs);
                    }
                    if (File.Exists(outPath))
                    {
                        var sf = await StorageFile.GetFileFromPathAsync(outPath);
                        files.Add(sf);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"拖出 {entry.Name} 失败：{ex.Message}", "ArchivePreviewPage");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"拖出解压失败：{ex.Message}", "ArchivePreviewPage");
        }

        if (files.Count > 0)
        {
            e.Data.SetStorageItems(files);
            e.Data.RequestedOperation = DataPackageOperation.Copy;
        }
        else { e.Cancel = true; }
    }
}
