// SPDX-License-Identifier: GPL-2.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SharpSevenZip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace SevenZipApp.Pages;

public sealed partial class ArchivePage : Page
{
    private string? _archivePath;

    public ArchivePage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string path && File.Exists(path))
            LoadArchive(path);
    }

    private async void Load_Click(object sender, RoutedEventArgs e)
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

        var file = await picker.PickSingleFileAsync();
        if (file != null)
            LoadArchive(file.Path);
    }

    private void LoadArchive(string path)
    {
        _archivePath = path;
        ArchivePathText.Text = path;

        try
        {
            using var extractor = new SharpSevenZipExtractor(path);
            var items = extractor.ArchiveFileData
                .Select(f => new ArchiveEntry
                {
                    Name = f.FileName,
                    Size = f.Size,
                    Modified = f.LastWriteTime,
                    IsDirectory = f.IsDirectory,
                })
                .ToList();

            ArchiveList.ItemsSource = items;
            Logger.Info($"已加载 {path}，共 {items.Count} 项", "ArchivePage");
        }
        catch (Exception ex)
        {
            Logger.Error($"加载压缩包失败：{ex.Message}", "ArchivePage");
        }
    }

    private async void ArchiveList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (_archivePath == null) return;
        if (e.Items.Count == 0) return;

        var entries = e.Items.OfType<ArchiveEntry>().Where(x => !x.IsDirectory).ToList();
        if (entries.Count == 0) return;

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
                        extractor.ExtractFile(entry.Name, fs);
                    }
                    if (File.Exists(outPath))
                    {
                        var sf = await StorageFile.GetFileFromPathAsync(outPath);
                        files.Add(sf);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"拖出 {entry.Name} 失败：{ex.Message}", "ArchivePage");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"拖出解压失败：{ex.Message}", "ArchivePage");
        }

        if (files.Count > 0)
        {
            e.Data.SetStorageItems(files);
            e.Data.RequestedOperation = DataPackageOperation.Copy;
        }
        else
        {
            e.Cancel = true;
        }
    }
}

