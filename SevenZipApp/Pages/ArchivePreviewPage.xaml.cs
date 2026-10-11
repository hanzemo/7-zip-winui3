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

namespace SevenZipApp.Pages;

public sealed partial class ArchivePreviewPage : Page
{
    private string? _archivePath;

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
            var items = extractor.ArchiveFileData
                .Select(f => new ArchiveEntry
                {
                    Name = f.FileName,
                    Size = f.Size,
                    Modified = f.LastWriteTime,
                    IsDirectory = f.IsDirectory,
                })
                .OrderBy(x => x.IsDirectory ? 0 : 1)
                .ThenBy(x => x.Name)
                .ToList();

            EntryList.ItemsSource = items;
            Logger.Info($"预览 {path}，共 {items.Count} 项", "ArchivePreviewPage");
        }
        catch (Exception ex)
        {
            Logger.Error($"预览失败：{ex.Message}", "ArchivePreviewPage");
        }
    }

    private async void EntryList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (_archivePath == null) return;

        var entries = e.Items.OfType<ArchiveEntry>().Where(x => !x.IsDirectory).ToList();
        if (entries.Count == 0)
        {
            e.Cancel = true;
            return;
        }

        var tempDir = Path.Combine(
            Path.GetTempPath(),
            "SevenZipApp_Drag",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var files = new List<StorageFile>();

        try
        {
            using var extractor = new SharpSevenZipExtractor(_archivePath);
            foreach (var entry in entries)
            {
                try
                {
                    var safeName = Path.GetFileName(entry.Name);
                    var outPath = Path.Combine(tempDir, safeName);

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
        else
        {
            e.Cancel = true;
        }
    }
}
