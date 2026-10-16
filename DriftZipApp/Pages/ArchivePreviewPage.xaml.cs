// SPDX-License-Identifier: GPL-2.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using SharpSevenZip;
using SharpCompress.Archives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace DriftZipApp.Pages;

public sealed partial class ArchivePreviewPage : Page
{
    private string? _archivePath;
    private List<ArchiveEntry> _allEntries = new();
    private string _currentPath = "";
    private string _sortKey = "Name";
    private bool _sortAsc = true;
    private string _filter = "";

    public ArchivePreviewPage()
    {
        this.InitializeComponent();
        this.AllowDrop = true;
        this.DragOver += OnDragOver;
        this.Drop += OnDrop;
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
                    RawName = f.FileName,
                    Name = Path.GetFileName(f.FileName.TrimEnd('/', '\\')),
                    Size = f.Size,
                    CompressedSize = 0,
                    Crc = f.Crc,
                    Modified = f.LastWriteTime,
                    IsDirectory = f.IsDirectory || f.FileName.EndsWith("/") || f.FileName.EndsWith("\\"),
                })
                .Where(x => !string.IsNullOrEmpty(x.FullPath))
                .ToList();

            // ── SharpCompress 补压缩率 ──
            try
            {
                using var scArchive = ArchiveFactory.OpenArchive(path);
                var scMap = new System.Collections.Generic.Dictionary<string, long>(System.StringComparer.OrdinalIgnoreCase);
                foreach (var scEntry in scArchive.Entries)
                {
                    if (scEntry.IsDirectory) continue;
                    if (scEntry.CompressedSize <= 0) continue;
                    var key = scEntry.Key?.Replace('\\', '/').TrimStart('/') ?? "";
                    if (!string.IsNullOrEmpty(key)) scMap[key] = scEntry.CompressedSize;
                }
                foreach (var e in _allEntries)
                    if (scMap.TryGetValue(e.FullPath, out long cs))
                        e.SharpCompressedSize = cs;
            }
            catch (Exception scEx)
            {
                Logger.Warn($"SharpCompress 读压缩率失败：{scEx.Message}", "ArchivePreviewPage");
            }

            _currentPath = "";
            RefreshView();

            // ── 整体压缩率（物理大小 vs 解压后总大小） ──
            try
            {
                long uncompressedTotal = _allEntries.Where(e => !e.IsDirectory).Sum(e => (long)e.Size);
                long physicalSize = new FileInfo(path).Length;
                if (uncompressedTotal > 0 && physicalSize > 0)
                {
                    double overall = 100.0 - (physicalSize * 100.0 / uncompressedTotal);
                    OverallRatioText.Text = $"整体压缩率：{overall:F1}%（{physicalSize / 1024.0 / 1024.0:F2} MB → {uncompressedTotal / 1024.0 / 1024.0:F2} MB）";
                }
                else
                {
                    OverallRatioText.Text = "";
                }
            }
            catch
            {
                OverallRatioText.Text = "";
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"预览失败：{ex.Message}", "ArchivePreviewPage");
        }
    }

    private void RefreshView()
    {
        var currentLevels = _currentPath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var result = new List<ArchiveEntry>();

        foreach (var entry in _allEntries)
        {
            var parts = entry.FullPath.Replace('\\', '/').Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length <= currentLevels.Length) continue;

            bool match = true;
            for (int i = 0; i < currentLevels.Length; i++)
            {
                if (!string.Equals(parts[i], currentLevels[i], StringComparison.OrdinalIgnoreCase)) { match = false; break; }
            }
            if (!match) continue;

            int depth = parts.Length - currentLevels.Length;
            if (depth == 1)
            {
                if (!result.Any(x => x.FullPath == entry.FullPath)) result.Add(entry);
            }
            else
            {
                var dirName = parts[currentLevels.Length];
                var dirPath = string.Join("/", parts.Take(currentLevels.Length + 1)) + "/";
                if (!result.Any(x => x.FullPath == dirPath))
                {
                    result.Add(new ArchiveEntry
                    {
                        Name = dirName, FullPath = dirPath, IsDirectory = true,
                        Size = 0, Modified = entry.Modified,
                    });
                }
            }
        }

        if (!string.IsNullOrEmpty(_filter))
            result = result.Where(x => x.Name.Contains(_filter, StringComparison.OrdinalIgnoreCase)).ToList();

        result = _sortKey switch
        {
            "Size"           => _sortAsc ? result.OrderBy(x => x.Size).ToList()           : result.OrderByDescending(x => x.Size).ToList(),
            "CompressedSize" => _sortAsc ? result.OrderBy(x => x.CompressedSize).ToList() : result.OrderByDescending(x => x.CompressedSize).ToList(),
            "Crc"            => _sortAsc ? result.OrderBy(x => x.Crc).ToList()            : result.OrderByDescending(x => x.Crc).ToList(),
            "Modified"       => _sortAsc ? result.OrderBy(x => x.Modified).ToList()       : result.OrderByDescending(x => x.Modified).ToList(),
            "Ratio"          => _sortAsc ? result.OrderBy(x => x.RatioValue).ToList()     : result.OrderByDescending(x => x.RatioValue).ToList(),
            _                => _sortAsc ? result.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList()
                                        : result.OrderByDescending(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList(),
        };

        result = result.OrderBy(x => x.IsDirectory ? 0 : 1).ThenBy(x => 0).ToList();

        EntryList.ItemsSource = result;
        CurrentPathText.Text = string.IsNullOrEmpty(_currentPath) ? "根目录" : $"/ {_currentPath}";
        GoUpButton.Visibility = string.IsNullOrEmpty(_currentPath) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Sort_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button b || b.Tag is not string key) return;
        if (_sortKey == key) _sortAsc = !_sortAsc;
        else { _sortKey = key; _sortAsc = true; }
        RefreshView();
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        _filter = sender.Text ?? "";
        RefreshView();
    }

    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        _filter = args.QueryText ?? "";
        RefreshView();
    }

    private async void EntryList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (EntryList.SelectedItem is not ArchiveEntry entry) return;

        if (entry.IsDirectory)
        {
            _currentPath = entry.FullPath.TrimEnd('/');
            RefreshView();
            return;
        }

        if (_archivePath == null) return;

        var tempDir = Path.Combine(Path.GetTempPath(), "DriftZipApp_Open", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var outPath = Path.Combine(tempDir, Path.GetFileName(entry.Name));

        try
        {
            using var extractor = new SharpSevenZipExtractor(_archivePath);
            using (var fs = File.Create(outPath))
                extractor.ExtractFile(entry.RawName, fs);

            Process.Start(new ProcessStartInfo(outPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            await ShowDialog("打开失败", ex.Message);
        }
    }
    private void GoUp_Click(object sender, RoutedEventArgs e)
    {
        var current = _currentPath.Trim('/');
        if (string.IsNullOrEmpty(current)) return;
        var idx = current.LastIndexOf('/');
        _currentPath = idx >= 0 ? current.Substring(0, idx) : "";
        RefreshView();
    }

    // ─────────────────────────────────────────────
    //  测试 / 属性
    // ─────────────────────────────────────────────
    private async void TestArchive_Click(object sender, RoutedEventArgs e)
    {
        if (_archivePath == null) return;
        OperationProgress.Visibility = Visibility.Visible;
        OperationProgress.IsIndeterminate = true;
        try
        {
            var engine = new DriftZipEngine();
            bool ok = await engine.TestArchiveAsync(_archivePath, null);
            await ShowDialog(ok ? "测试通过" : "测试失败", ok ? "压缩包完整。" : "压缩包损坏或无法读取。");
        }
        finally { OperationProgress.Visibility = Visibility.Collapsed; }
    }

    private async void ShowProperties_Click(object sender, RoutedEventArgs e)
    {
        if (_archivePath == null) return;
        try
        {
            var engine = new DriftZipEngine();
            var items = engine.ListArchiveWithDetails(_archivePath, null);
            long totalSize = items.Sum(x => (long)x.Size);
            long totalComp = items.Sum(x => x.CompressedSize);
            double ratio = totalSize > 0 ? 100.0 - (totalComp * 100.0 / totalSize) : 0;
            var info = new FileInfo(_archivePath);

            var text =
                $"文件：{info.Name}\n" +
                $"物理大小：{info.Length / 1024.0 / 1024.0:F2} MB\n" +
                $"条目数：{items.Count}\n" +
                $"原始总大小：{totalSize / 1024.0 / 1024.0:F2} MB\n" +
                $"压缩后总大小：{totalComp / 1024.0 / 1024.0:F2} MB\n" +
                $"压缩率：{ratio:F1}%\n" +
                $"修改时间：{info.LastWriteTime:yyyy-MM-dd HH:mm:ss}";

            await ShowDialog("压缩包属性", text);
        }
        catch (Exception ex) { await ShowDialog("错误", ex.Message); }
    }

    private async Task ShowDialog(string title, string content)
    {
        var dlg = new ContentDialog
        {
            Title = title, Content = content,
            CloseButtonText = "确定", XamlRoot = this.XamlRoot,
        };
        await dlg.ShowAsync();
    }

    // ─────────────────────────────────────────────
    //  重命名 / 删除
    // ─────────────────────────────────────────────
    private async void RenameEntry_Click(object sender, RoutedEventArgs e)
    {
        if (_archivePath == null) return;
        var entries = EntryList.SelectedItems.OfType<ArchiveEntry>().Where(x => !x.IsDirectory).ToList();
        if (entries.Count != 1) { await ShowDialog("提示", "请只选一个文件重命名。"); return; }

        var entry = entries[0];
        var box = new TextBox { Text = entry.Name };
        var dlg = new ContentDialog
        {
            Title = "重命名",
            Content = box,
            PrimaryButtonText = "确定", CloseButtonText = "取消",
            XamlRoot = this.XamlRoot,
        };
        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

        var newName = box.Text?.Trim();
        if (string.IsNullOrEmpty(newName) || newName == entry.Name) return;

        try
        {
            var engine = new DriftZipEngine();
            await engine.RenameEntryAsync(_archivePath, entry.RawName, newName, null);
            LoadArchive(_archivePath);
        }
        catch (Exception ex) { await ShowDialog("重命名失败", ex.Message); }
    }

    private async void DeleteEntry_Click(object sender, RoutedEventArgs e)
    {
        if (_archivePath == null) return;
        var entries = EntryList.SelectedItems.OfType<ArchiveEntry>().Where(x => !x.IsDirectory).ToList();
        if (entries.Count == 0) return;

        var dlg = new ContentDialog
        {
            Title = "删除",
            Content = $"确认删除 {entries.Count} 项？",
            PrimaryButtonText = "删除", CloseButtonText = "取消",
            XamlRoot = this.XamlRoot,
        };
        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

        try
        {
            var engine = new DriftZipEngine();
            await engine.DeleteEntriesAsync(_archivePath, entries.Select(x => x.RawName), null);
            LoadArchive(_archivePath);
        }
        catch (Exception ex) { await ShowDialog("删除失败", ex.Message); }
    }

    // ─────────────────────────────────────────────
    //  添加文件到压缩包（右键菜单）
    // ─────────────────────────────────────────────
    private async void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        if (_archivePath == null) return;

        var picker = new FileOpenPicker();
        picker.ViewMode = PickerViewMode.List;
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add("*");
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var files = await picker.PickMultipleFilesAsync();
        if (files == null || files.Count == 0) return;

        // 选更新模式
        var modeBox = new ComboBox
        {
            Items =
            {
                new ComboBoxItem { Content = "添加（跳过重复）", Tag = UpdateMode.Add },
                new ComboBoxItem { Content = "更新（替换重复）", Tag = UpdateMode.Update, IsSelected = true },
                new ComboBoxItem { Content = "仅更新（只替换已存在）", Tag = UpdateMode.Freshen },
                new ComboBoxItem { Content = "同步（仅保留新文件）", Tag = UpdateMode.Sync },
            }
        };
        var dlg = new ContentDialog
        {
            Title = $"添加 {files.Count} 个文件",
            Content = modeBox,
            PrimaryButtonText = "开始", CloseButtonText = "取消",
            XamlRoot = this.XamlRoot,
        };
        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

        var mode = (UpdateMode)((ComboBoxItem)modeBox.SelectedItem).Tag;

        OperationProgress.Visibility = Visibility.Visible;
        OperationProgress.IsIndeterminate = true;
        try
        {
            var engine = new DriftZipEngine();
            await engine.UpdateArchiveAsync(_archivePath, files.Select(f => f.Path), null, mode);
            LoadArchive(_archivePath);
            await ShowDialog("完成", $"已添加 {files.Count} 个文件。");
        }
        catch (Exception ex) { await ShowDialog("添加失败", ex.Message); }
        finally { OperationProgress.Visibility = Visibility.Collapsed; }
    }

    // ─────────────────────────────────────────────
    //  解压（带冲突策略）
    // ─────────────────────────────────────────────
    private async void ExtractEntryTo_Click(object sender, RoutedEventArgs e)
    {
        var entries = EntryList.SelectedItems.OfType<ArchiveEntry>().Where(x => !x.IsDirectory).ToList();
        if (entries.Count == 0 || _archivePath == null) return;

        var folder = new FolderPicker();
        folder.FileTypeFilter.Add("*");
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(folder, hwnd);
        var target = await folder.PickSingleFolderAsync();
        if (target == null) return;

        var policy = await AskConflictPolicy();
        if (policy == null) return;

        OperationProgress.Visibility = Visibility.Visible;
        try
        {
            var engine = new DriftZipEngine();
            var progress = new Progress<int>(pct => OperationProgress.Value = pct);
            await engine.ExtractSelectedAsync(_archivePath, entries.Select(x => x.RawName),
                target.Path, null, progress, policy.Value);
        }
        catch (Exception ex) { await ShowDialog("解压失败", ex.Message); }
        finally { OperationProgress.Visibility = Visibility.Collapsed; }
    }

    private async void ExtractEntryHere_Click(object sender, RoutedEventArgs e)
    {
        var entries = EntryList.SelectedItems.OfType<ArchiveEntry>().Where(x => !x.IsDirectory).ToList();
        if (entries.Count == 0 || _archivePath == null) return;

        var targetDir = Path.GetDirectoryName(_archivePath);
        if (string.IsNullOrEmpty(targetDir)) return;

        var policy = await AskConflictPolicy();
        if (policy == null) return;

        try
        {
            var engine = new DriftZipEngine();
            await engine.ExtractSelectedAsync(_archivePath, entries.Select(x => x.RawName),
                targetDir, null, null, policy.Value);
        }
        catch (Exception ex) { await ShowDialog("解压失败", ex.Message); }
    }

    private async Task<ConflictPolicy?> AskConflictPolicy()
    {
        var combo = new ComboBox
        {
            Items =
            {
                new ComboBoxItem { Content = "覆盖", Tag = ConflictPolicy.Overwrite, IsSelected = true },
                new ComboBoxItem { Content = "跳过", Tag = ConflictPolicy.Skip },
                new ComboBoxItem { Content = "重命名", Tag = ConflictPolicy.Rename },
            }
        };
        var dlg = new ContentDialog
        {
            Title = "文件冲突时",
            Content = combo,
            PrimaryButtonText = "确定", CloseButtonText = "取消",
            XamlRoot = this.XamlRoot,
        };
        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return null;
        return (ConflictPolicy)((ComboBoxItem)combo.SelectedItem).Tag;
    }

    private void CopyEntryPath_Click(object sender, RoutedEventArgs e)
    {
        var entries = EntryList.SelectedItems.OfType<ArchiveEntry>().ToList();
        if (entries.Count == 0) return;
        var dp = new DataPackage();
        dp.SetText(string.Join(Environment.NewLine, entries.Select(x => x.FullPath)));
        Clipboard.SetContent(dp);
    }

    // ─────────────────────────────────────────────
    //  拖出
    // ─────────────────────────────────────────────
    private async void EntryList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        Logger.Info($"拖出开始：_archivePath={_archivePath ?? "null"}, items={e.Items.Count}", "ArchivePreviewPage");

        if (_archivePath == null) return;
        var entries = e.Items.OfType<ArchiveEntry>().Where(x => !x.IsDirectory).ToList();
        if (entries.Count == 0) { e.Cancel = true; return; }

        var tempDir = Path.Combine(Path.GetTempPath(), "DriftZipApp_Drag", Guid.NewGuid().ToString("N"));
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
                    Logger.Info($"拖出：开始 ExtractFile {entry.RawName}", "ArchivePreviewPage");
                    using (var fs = File.Create(outPath)) extractor.ExtractFile(entry.RawName, fs);
                    if (File.Exists(outPath)) files.Add(await StorageFile.GetFileFromPathAsync(outPath));
                    Logger.Info($"拖出：ExtractFile 完成", "ArchivePreviewPage");
                }
                catch (Exception ex) { Logger.Warn($"拖出单项失败：{ex.Message}", "ArchivePreviewPage"); }
            }
        }
        catch (Exception ex) { Logger.Warn($"拖出解压失败：{ex.Message}", "ArchivePreviewPage"); }

        Logger.Info($"拖出：entries={entries.Count}, files={files.Count}", "ArchivePreviewPage");

        if (files.Count > 0)
        {
            e.Data.SetStorageItems(files);
            e.Data.RequestedOperation = DataPackageOperation.Copy;
        }
        else e.Cancel = true;
    }

    // ─────────────────────────────────────────────
    //  拖入（拖文件到此页 → 添加到压缩包）
    // ─────────────────────────────────────────────
    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "添加到压缩包";
        }
    }


    // ─────────────────────────────────────────────
    //  拖入（拖文件到此页 → 添加到压缩包）
    // ─────────────────────────────────────────────
    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (_archivePath == null) return;
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;

        var items = await e.DataView.GetStorageItemsAsync();
        var files = items.OfType<StorageFile>().Select(f => f.Path).ToList();
        if (files.Count == 0) return;

        var policy = await AskConflictPolicy();
        if (policy == null) return;

        try
        {
            var engine = new DriftZipEngine();
            var mode = policy == ConflictPolicy.Overwrite ? UpdateMode.Update : UpdateMode.Add;
            await engine.UpdateArchiveAsync(_archivePath, files, null, mode);
            LoadArchive(_archivePath);
            await ShowDialog("完成", $"已添加 {files.Count} 个文件。");
        }
        catch (Exception ex) { await ShowDialog("添加失败", ex.Message); }
    }

    // ─────────────────────────────────────────────
    //  页面离开时清理临时文件
    // ─────────────────────────────────────────────
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        var tempRoot = Path.Combine(Path.GetTempPath(), "DriftZipApp_Open");
        if (!Directory.Exists(tempRoot)) return;

        try
        {
            Directory.Delete(tempRoot, true);
            Logger.Info("已清理临时文件", "ArchivePreviewPage");
        }
        catch (Exception ex)
        {
            Logger.Warn($"清理临时文件失败：{ex.Message}", "ArchivePreviewPage");
        }
    }
}
