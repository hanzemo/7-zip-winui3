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
                    Name = Path.GetFileName(f.FileName.TrimEnd('/')),
                    FullPath = f.FileName,
                    Size = f.Size,
                    Modified = f.LastWriteTime,
                    IsDirectory = f.IsDirectory,
                })
                .ToList();

            _currentPath = "";
            RefreshView();
            Logger.Info($"预览 {path}，共 {_allEntries.Count} 项", "ArchivePreviewPage");
        }
        catch (Exception ex)
        {
            Logger.Error($"预览失败：{ex.Message}", "ArchivePreviewPage");
        }
    }

    private void RefreshView()
    {
        var prefix = string.IsNullOrEmpty(_currentPath) ? "" : _currentPath + "/";

        var items = _allEntries
            .Where(x => x.FullPath.StartsWith(prefix))
            .Where(x => x.FullPath.Length > prefix.Length)
            .Select(x =>
            {
                var relative = x.FullPath.Substring(prefix.Length);
                var slashIdx = relative.IndexOf('/');
                if (slashIdx >= 0)
                {
                    var dirName = relative.Substring(0, slashIdx);
                    return new ArchiveEntry
                    {
                        Name = dirName,
                        FullPath = prefix + dirName + "/",
                        IsDirectory = true,
                        Size = 0,
                        Modified = x.Modified,
                    };
                }
                return x;
            })
            .GroupBy(x => x.FullPath)
            .Select(g => g.First())
            .OrderBy(x => x.IsDirectory ? 0 : 1)
            .ThenBy(x => x.Name)
            .ToList();

        EntryList.ItemsSource = items;
        CurrentPathText.Text = string.IsNullOrEmpty(_currentPath) ? "根目录" : $"/ {_currentPath}";
        GoUpButton.Visibility = string.IsNullOrEmpty(_currentPath)
            ? Visibility.Collapsed : Visibility.Visible;
    }

    private void EntryList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (EntryList.SelectedItem is ArchiveEntry entry && entry.IsDirectory)
        {
            _currentPath = entry.FullPath.TrimEnd('/');
            RefreshView();
        }
    }

    private void GoUp_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentPath)) return;
        var idx = _currentPath.LastIndexOf('/');
        _currentPath = idx >= 0 ? _currentPath.Substring(0, idx) : "";
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
