// SPDX-License-Identifier: GPL-2.0-or-later

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using DriftZipApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace DriftZipApp.Pages;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel { get; } = new();

    private ArchiveItem? _rightClickedItem;

    public HomePage()
    {
        this.InitializeComponent();
        this.DataContext = ViewModel;
        this.Loaded += (s, e) => ViewModel.Refresh();
    }

    private void ArchiveGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not ArchiveItem item) return;
        ViewModel.SelectedArchive = item;
        ViewModel.StatusMessage = $"已选择：{item.Name}";
    }

    private void ArchiveGridView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        var item = _rightClickedItem ?? ArchiveGridView.SelectedItem as ArchiveItem;
        if (item == null) return;
        if (string.IsNullOrEmpty(item.Path)) return;
        OpenItem(item);
    }

    private void ArchiveGridView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement fe)
        {
            var container = FindParent<GridViewItem>(fe);
            _rightClickedItem = container?.Content as ArchiveItem;
        }

        if (_rightClickedItem == null)
            _rightClickedItem = ArchiveGridView.SelectedItem as ArchiveItem;

        var flyout = BuildContextMenu();
        flyout.ShowAt(ArchiveGridView, new FlyoutShowOptions
        {
            Position = e.GetPosition(ArchiveGridView),
        });
        e.Handled = true;
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(child);
        while (parent != null)
        {
            if (parent is T typed) return typed;
            parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(parent);
        }
        return null;
    }

    private MenuFlyout BuildContextMenu()
    {
        var menu = new MenuFlyout();

        bool isDiskRoot = ViewModel.BrowseMode == BrowseMode.ThisPC
                          && string.IsNullOrEmpty(ViewModel.CurrentPath);
        bool isExplorer = ViewModel.BrowseMode == BrowseMode.ThisPC
                          && !string.IsNullOrEmpty(ViewModel.CurrentPath);

        // ── A. 磁盘页：只有视图 ──
        if (isDiskRoot)
        {
            AddViewMenu(menu);
            return menu;
        }

        // ── 打开（仅资源管理器） ──
        if (isExplorer)
        {
            var open = new MenuFlyoutItem { Text = "打开" };
            open.Click += Open_Click;
            menu.Items.Add(open);
        }

        // ── 新建（仅资源管理器） ──
        if (isExplorer)
        {
            var newFolder = new MenuFlyoutItem { Text = "新建文件夹" };
            newFolder.Click += NewFolder_Click;
            menu.Items.Add(newFolder);
        }

        // ── 重命名 / 删除 ──
        var rename = new MenuFlyoutItem { Text = "重命名" };
        rename.Click += Rename_Click;
        menu.Items.Add(rename);

        var delete = new MenuFlyoutItem { Text = "删除" };
        delete.Click += Delete_Click;
        menu.Items.Add(delete);

        // ── 复制/剪切/粘贴（仅资源管理器） ──
        if (isExplorer)
        {
            var copy = new MenuFlyoutItem { Text = "复制" };
            copy.Click += Copy_Click;
            menu.Items.Add(copy);

            var cut = new MenuFlyoutItem { Text = "剪切" };
            cut.Click += Cut_Click;
            menu.Items.Add(cut);

            var paste = new MenuFlyoutItem { Text = "粘贴" };
            paste.Click += Paste_Click;
            menu.Items.Add(paste);
        }

        menu.Items.Add(new MenuFlyoutSeparator());

        // ── 压缩/解压（对压缩包文件） ──
        var item = _rightClickedItem;
        bool targetIsArchive = item != null
            && !item.IsDirectory
            && item.Path != null
            && IsArchiveFile(item.Path);

        if (targetIsArchive)
        {
            var extractTo = new MenuFlyoutItem { Text = "解压到…" };
            extractTo.Click += ExtractTo_Click;
            menu.Items.Add(extractTo);

            var extractHere = new MenuFlyoutItem { Text = "解压到此目录" };
            extractHere.Click += ExtractHere_Click;
            menu.Items.Add(extractHere);

            var batchSame = new MenuFlyoutItem { Text = "批量解压到同一目录" };
            batchSame.Click += BatchExtractSame_Click;
            menu.Items.Add(batchSame);

            var batchSeparate = new MenuFlyoutItem { Text = "批量解压到各自文件夹" };
            batchSeparate.Click += BatchExtractSeparate_Click;
            menu.Items.Add(batchSeparate);

            var testArchive = new MenuFlyoutItem { Text = "测试压缩包" };
            testArchive.Click += TestArchive_Click;
            menu.Items.Add(testArchive);

            menu.Items.Add(new MenuFlyoutSeparator());
        }

        if (item != null && !item.IsDirectory)
        {
            var compressTo = new MenuFlyoutItem { Text = "压缩到…" };
            compressTo.Click += CompressTo_Click;
            menu.Items.Add(compressTo);

            var compressHere = new MenuFlyoutItem { Text = "压缩到此目录" };
            compressHere.Click += CompressHere_Click;
            menu.Items.Add(compressHere);

            menu.Items.Add(new MenuFlyoutSeparator());
        }

        menu.Items.Add(new MenuFlyoutSeparator());

        // ── 浏览模式切换 ──
        if (isExplorer)
        {
            var switchScan = new MenuFlyoutItem { Text = "扫描压缩包" };
            switchScan.Click += BrowseMode_Click;
            switchScan.Tag = "ScanArchives";
            menu.Items.Add(switchScan);
        }

        if (ViewModel.BrowseMode == BrowseMode.ScanArchives)
        {
            var switchPC = new MenuFlyoutItem { Text = "浏览此电脑" };
            switchPC.Click += BrowseMode_Click;
            switchPC.Tag = "ThisPC";
            menu.Items.Add(switchPC);
        }

        AddViewMenu(menu);
        return menu;
    }

    private void AddViewMenu(MenuFlyout menu)
    {
        var view = new MenuFlyoutSubItem { Text = "视图" };

        var details = new MenuFlyoutItem { Text = "详细信息", Tag = "Details" };
        details.Click += ViewMode_Click;
        view.Items.Add(details);

        var large = new MenuFlyoutItem { Text = "大图标", Tag = "LargeIcons" };
        large.Click += ViewMode_Click;
        view.Items.Add(large);

        var small = new MenuFlyoutItem { Text = "小图标", Tag = "SmallIcons" };
        small.Click += ViewMode_Click;
        view.Items.Add(small);

        menu.Items.Add(view);
    }

    private static bool IsArchiveFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".7z" or ".zip" or ".rar" or ".tar" or ".gz" or ".bz2" or ".xz";
    }

    private List<ArchiveItem> GetTargetItems()
    {
        if (_rightClickedItem != null)
        {
            var selected = ArchiveGridView.SelectedItems.OfType<ArchiveItem>().ToList();
            if (selected.Contains(_rightClickedItem) && selected.Count > 1)
                return selected;
            return new List<ArchiveItem> { _rightClickedItem };
        }

        var items = ArchiveGridView.SelectedItems.OfType<ArchiveItem>().ToList();
        if (items.Count > 0) return items;

        if (ArchiveGridView.SelectedItem is ArchiveItem single)
            items.Add(single);

        return items;
    }

    private void OpenItem(ArchiveItem item)
    {
        if (ViewModel.BrowseMode == BrowseMode.ThisPC)
        {
            if (item.IsDirectory)
            {
                ViewModel.NavigateTo(item.Path!);
                UpdateToolbar();
            }
            else
            {
                try { Process.Start(new ProcessStartInfo(item.Path!) { UseShellExecute = true }); }
                catch (Exception ex) { ViewModel.StatusMessage = $"打开失败：{ex.Message}"; }
            }
        }
        else
        {
            this.Frame.Navigate(typeof(ArchivePreviewPage), item.Path);
        }
    }

    private void ShowProgress(string status)
    {
        OperationProgress.Visibility = Visibility.Visible;
        OperationProgress.IsIndeterminate = false;
        OperationProgress.Value = 0;
        ViewModel.StatusMessage = status;
    }

    private void HideProgress()
    {
        OperationProgress.Visibility = Visibility.Collapsed;
        OperationProgress.Value = 0;
    }

    private IProgress<int> MakeProgress()
    {
        return new Progress<int>(pct => OperationProgress.Value = pct);
    }

    private void Back_Click(object sender, RoutedEventArgs e) { ViewModel.GoBack(); UpdateToolbar(); }
    private void Forward_Click(object sender, RoutedEventArgs e) { ViewModel.GoForward(); UpdateToolbar(); }
    private void Up_Click(object sender, RoutedEventArgs e) { ViewModel.GoUp(); UpdateToolbar(); }
    private void Refresh_Click(object sender, RoutedEventArgs e) { ViewModel.Refresh(); UpdateToolbar(); }

    private void PathBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var q = args.QueryText?.Trim();
        if (string.IsNullOrEmpty(q)) return;

        if (Directory.Exists(q)) ViewModel.NavigateTo(q);
        else if (File.Exists(q)) this.Frame.Navigate(typeof(ArchivePreviewPage), q);
        else ViewModel.Search(q);
        UpdateToolbar();
    }

    private async void ExtractTo_Click(object sender, RoutedEventArgs e)
    {
        var items = GetTargetItems();
        if (items.Count == 0) return;

        var folder = new FolderPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(folder, hwnd);
        folder.FileTypeFilter.Add("*");
        var target = await folder.PickSingleFolderAsync();
        if (target == null) return;

        var engine = new DriftZipEngine();
        int ok = 0;

        ShowProgress("正在解压...");
        try
        {
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.Path) || item.IsDirectory) continue;
                await Task.Run(() => engine.Extract(item.Path!, target.Path, null, MakeProgress(), CancellationToken.None));
                ok++;
            }
            ViewModel.StatusMessage = $"解压完成：{ok} 项";
        }
        catch (Exception ex) { ViewModel.StatusMessage = $"解压失败: {ex.Message}"; }
        finally { HideProgress(); ViewModel.Refresh(); }
    }

    private async void BatchExtractSame_Click(object sender, RoutedEventArgs e)
    {
        var items = GetTargetItems();
        if (items.Count == 0) return;

        var folder = new FolderPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(folder, hwnd);
        folder.FileTypeFilter.Add("*");
        var target = await folder.PickSingleFolderAsync();
        if (target == null) return;

        var engine = new DriftZipEngine();
        int ok = 0, fail = 0;
        ShowProgress("正在批量解压...");
        try
        {
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.Path) || item.IsDirectory) continue;
                try
                {
                    await Task.Run(() => engine.Extract(item.Path!, target.Path, null, MakeProgress(), CancellationToken.None));
                    ok++;
                }
                catch { fail++; }
            }
            ViewModel.StatusMessage = $"批量解压完成：{ok} 成功，{fail} 失败";
        }
        finally { HideProgress(); ViewModel.Refresh(); }
    }

    private async void BatchExtractSeparate_Click(object sender, RoutedEventArgs e)
    {
        var items = GetTargetItems();
        if (items.Count == 0) return;

        var folder = new FolderPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(folder, hwnd);
        folder.FileTypeFilter.Add("*");
        var target = await folder.PickSingleFolderAsync();
        if (target == null) return;

        var engine = new DriftZipEngine();
        int ok = 0, fail = 0;
        ShowProgress("正在批量解压...");
        try
        {
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.Path) || item.IsDirectory) continue;
                var dest = Path.Combine(target.Path, Path.GetFileNameWithoutExtension(item.Path));
                try
                {
                    await Task.Run(() => engine.Extract(item.Path!, dest, null, MakeProgress(), CancellationToken.None));
                    ok++;
                }
                catch { fail++; }
            }
            ViewModel.StatusMessage = $"批量解压完成：{ok} 成功，{fail} 失败";
        }
        finally { HideProgress(); ViewModel.Refresh(); }
    }

    private async void CompressTo_Click(object sender, RoutedEventArgs e)
    {
        var items = GetTargetItems();
        if (items.Count == 0) return;

        var save = new FileSavePicker();
        save.SuggestedFileName = "archive";
        save.FileTypeChoices.Add("7z", new[] { ".7z" });
        save.FileTypeChoices.Add("zip", new[] { ".zip" });
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(save, hwnd);
        var file = await save.PickSaveFileAsync();
        if (file == null) return;

        var options = new CompressionOptions
        {
            Format = file.Path.EndsWith(".zip") ? OutputFormat.Zip : OutputFormat.SevenZip,
            Level = CompressionLevel.Normal,
            SourceFiles = items.Where(x => !string.IsNullOrEmpty(x.Path)).Select(x => x.Path!).ToList(),
        };

        ShowProgress("正在压缩...");
        try
        {
            var engine = new DriftZipEngine();
            await Task.Run(() => engine.Compress(options, file.Path, MakeProgress(), CancellationToken.None));
            ViewModel.StatusMessage = $"压缩完成：{file.Path}";
        }
        catch (Exception ex) { ViewModel.StatusMessage = $"压缩失败: {ex.Message}"; }
        finally { HideProgress(); ViewModel.Refresh(); }
    }

    private async void ExtractHere_Click(object sender, RoutedEventArgs e)
    {
        var items = GetTargetItems();
        if (items.Count == 0) return;

        var engine = new DriftZipEngine();
        ShowProgress("正在解压...");
        try
        {
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.Path) || item.IsDirectory) continue;
                var dir = Path.GetDirectoryName(item.Path)!;
                await Task.Run(() => engine.Extract(item.Path!, dir, null, MakeProgress(), CancellationToken.None));
            }
            ViewModel.StatusMessage = "已解压到当前目录";
        }
        catch (Exception ex) { ViewModel.StatusMessage = $"解压失败: {ex.Message}"; }
        finally { HideProgress(); ViewModel.Refresh(); }
    }

    private async void CompressHere_Click(object sender, RoutedEventArgs e)
    {
        var items = GetTargetItems();
        if (items.Count == 0) return;

        var first = items.First().Path;
        if (string.IsNullOrEmpty(first)) return;
        var dir = Path.GetDirectoryName(first)!;
        var name = items.Count == 1 ? Path.GetFileNameWithoutExtension(first) : "archive";
        var output = Path.Combine(dir, name + ".7z");

        var options = new CompressionOptions
        {
            Format = OutputFormat.SevenZip,
            Level = CompressionLevel.Normal,
            SourceFiles = items.Where(x => !string.IsNullOrEmpty(x.Path)).Select(x => x.Path!).ToList(),
        };

        ShowProgress("正在压缩...");
        try
        {
            var engine = new DriftZipEngine();
            await Task.Run(() => engine.Compress(options, output, MakeProgress(), CancellationToken.None));
            ViewModel.StatusMessage = $"压缩完成：{output}";
        }
        catch (Exception ex) { ViewModel.StatusMessage = $"压缩失败: {ex.Message}"; }
        finally { HideProgress(); ViewModel.Refresh(); }
    }

    private async void TestArchive_Click(object sender, RoutedEventArgs e)
    {
        var items = GetTargetItems();
        if (items.Count == 0) return;

        ShowProgress("正在测试...");
        OperationProgress.IsIndeterminate = true;
        try
        {
            var engine = new DriftZipEngine();
            int ok = 0, fail = 0;
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.Path) || item.IsDirectory) continue;
                bool passed = await engine.TestArchiveAsync(item.Path, null);
                if (passed) ok++; else fail++;
            }
            ViewModel.StatusMessage = $"测试完成：{ok} 通过，{fail} 失败";
        }
        catch (Exception ex) { ViewModel.StatusMessage = $"测试失败：{ex.Message}"; }
        finally { OperationProgress.IsIndeterminate = false; HideProgress(); }
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var items = GetTargetItems();
        if (items.Count > 0) OpenItem(items[0]);
    }

    private void NewFolder_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.BrowseMode != BrowseMode.ThisPC) return;
        if (string.IsNullOrEmpty(ViewModel.CurrentPath)) return;

        var baseName = "新建文件夹";
        var target = Path.Combine(ViewModel.CurrentPath, baseName);
        int i = 1;
        while (Directory.Exists(target) || File.Exists(target))
        {
            target = Path.Combine(ViewModel.CurrentPath, $"{baseName} ({i})");
            i++;
        }

        try
        {
            Directory.CreateDirectory(target);
            ViewModel.StatusMessage = "已创建文件夹";
            ViewModel.Refresh();
        }
        catch (Exception ex) { ViewModel.StatusMessage = $"新建失败：{ex.Message}"; }
    }

    private async void Rename_Click(object sender, RoutedEventArgs e)
    {
        var items = GetTargetItems();
        if (items.Count == 0) return;
        var item = items[0];
        if (string.IsNullOrEmpty(item.Path)) return;

        var box = new TextBox { Text = item.Name };
        var dialog = new ContentDialog
        {
            Title = "重命名", Content = box,
            PrimaryButtonText = "确定", CloseButtonText = "取消",
            XamlRoot = this.XamlRoot,
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        var newName = box.Text?.Trim();
        if (string.IsNullOrEmpty(newName) || newName == item.Name) return;

        try
        {
            var dir = Path.GetDirectoryName(item.Path)!;
            var dest = Path.Combine(dir, newName);
            if (item.IsDirectory) Directory.Move(item.Path, dest);
            else File.Move(item.Path, dest);
            ViewModel.StatusMessage = "重命名完成";
            ViewModel.Refresh();
        }
        catch (Exception ex) { ViewModel.StatusMessage = $"重命名失败：{ex.Message}"; }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        var items = GetTargetItems();
        if (items.Count == 0) return;

        var dialog = new ContentDialog
        {
            Title = "删除", Content = $"确认删除 {items.Count} 项？",
            PrimaryButtonText = "删除", CloseButtonText = "取消",
            XamlRoot = this.XamlRoot,
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        try
        {
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.Path)) continue;
                if (item.IsDirectory) Directory.Delete(item.Path, true);
                else File.Delete(item.Path);
            }
            ViewModel.StatusMessage = $"已删除 {items.Count} 项";
            ViewModel.Refresh();
        }
        catch (Exception ex) { ViewModel.StatusMessage = $"删除失败：{ex.Message}"; }
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        var paths = GetTargetItems().Where(x => !string.IsNullOrEmpty(x.Path)).Select(x => x.Path!).ToList();
        ViewModel.SetClipboard(paths, cut: false);
        ViewModel.StatusMessage = $"已复制 {paths.Count} 项";
    }

    private void Cut_Click(object sender, RoutedEventArgs e)
    {
        var paths = GetTargetItems().Where(x => !string.IsNullOrEmpty(x.Path)).Select(x => x.Path!).ToList();
        ViewModel.SetClipboard(paths, cut: true);
        ViewModel.StatusMessage = $"已剪切 {paths.Count} 项";
    }

    private void Paste_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.Paste();
        ViewModel.Refresh();
    }

    private void ViewMode_Click(object sender, RoutedEventArgs e)
    {
        string? tag = (sender as MenuFlyoutItem)?.Tag?.ToString();
        switch (tag)
        {
            case "Details":
                ArchiveGridView.Visibility = Visibility.Collapsed;
                ArchiveListView.Visibility = Visibility.Visible;
                break;
            case "LargeIcons":
                ArchiveGridView.Visibility = Visibility.Visible;
                ArchiveListView.Visibility = Visibility.Collapsed;
                ArchiveGridView.ItemTemplate = (DataTemplate)Resources["LargeIconTemplate"];
                break;
            case "SmallIcons":
                ArchiveGridView.Visibility = Visibility.Visible;
                ArchiveListView.Visibility = Visibility.Collapsed;
                ArchiveGridView.ItemTemplate = (DataTemplate)Resources["SmallIconTemplate"];
                break;
        }
    }

    private void BrowseMode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem item) return;
        var mode = item.Tag?.ToString() == "ThisPC" ? BrowseMode.ThisPC : BrowseMode.ScanArchives;
        ViewModel.SwitchBrowseMode(mode);
        UpdateToolbar();
    }

    private void UpdateToolbar()
    {
        bool isPC = ViewModel.BrowseMode == BrowseMode.ThisPC;
        BackButton.Visibility = isPC && ViewModel.CanGoBack ? Visibility.Visible : Visibility.Collapsed;
        ForwardButton.Visibility = isPC && ViewModel.CanGoForward ? Visibility.Visible : Visibility.Collapsed;
        UpButton.Visibility = isPC && !string.IsNullOrEmpty(ViewModel.CurrentPath) ? Visibility.Visible : Visibility.Collapsed;
        BreadcrumbPanel.Visibility = isPC ? Visibility.Visible : Visibility.Collapsed;

        if (isPC)
        {
            PathBox.Text = string.IsNullOrEmpty(ViewModel.CurrentPath) ? "此电脑" : ViewModel.CurrentPath;
            BuildBreadcrumb();
        }
        else { PathBox.Text = ""; }
    }

    private void BuildBreadcrumb()
    {
        BreadcrumbPanel.Children.Clear();

        var root = new HyperlinkButton { Content = "此电脑" };
        root.Click += (s, e) => { ViewModel.NavigateTo(""); UpdateToolbar(); };
        BreadcrumbPanel.Children.Add(root);

        if (string.IsNullOrEmpty(ViewModel.CurrentPath)) return;

        var parts = ViewModel.CurrentPath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
        var acc = "";
        foreach (var part in parts)
        {
            acc = string.IsNullOrEmpty(acc) ? part : $"{acc}\\{part}";
            BreadcrumbPanel.Children.Add(new TextBlock { Text = ">", VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center });

            var capturedPath = acc;
            var btn = new HyperlinkButton { Content = part };
            btn.Click += (s, e) => { ViewModel.NavigateTo(capturedPath); UpdateToolbar(); };
            BreadcrumbPanel.Children.Add(btn);
        }
    }

    private void HomePage_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "拖到此处压缩或添加";
        }
    }

    private async void HomePage_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;

        var items = await e.DataView.GetStorageItemsAsync();
        var paths = items.Select(i => i.Path).Where(p => !string.IsNullOrEmpty(p)).ToList();
        if (paths.Count == 0) return;

        var target = _rightClickedItem;
        if (target != null && !target.IsDirectory && !string.IsNullOrEmpty(target.Path) && IsArchiveFile(target.Path))
        {
            var dlg = new ContentDialog
            {
                Title = "添加到压缩包",
                Content = $"将 {paths.Count} 个文件添加到 {target.Name}？",
                PrimaryButtonText = "添加", CloseButtonText = "取消",
                XamlRoot = this.XamlRoot,
            };
            if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

            try
            {
                var engine = new DriftZipEngine();
                await engine.UpdateArchiveAsync(target.Path!, paths, null);
                ViewModel.StatusMessage = "已添加";
                ViewModel.Refresh();
            }
            catch (Exception ex) { ViewModel.StatusMessage = $"添加失败：{ex.Message}"; }
            return;
        }

        if (string.IsNullOrEmpty(ViewModel.CurrentPath)) return;
        var output = Path.Combine(ViewModel.CurrentPath, "archive.7z");
        int i = 1;
        while (File.Exists(output))
            output = Path.Combine(ViewModel.CurrentPath, $"archive ({i++}).7z");

        ShowProgress("正在压缩...");
        try
        {
            var engine = new DriftZipEngine();
            await Task.Run(() => engine.Compress(new CompressionOptions
            {
                Format = OutputFormat.SevenZip,
                Level = CompressionLevel.Normal,
                SourceFiles = paths,
            }, output, MakeProgress(), CancellationToken.None));
            ViewModel.StatusMessage = $"已压缩：{output}";
        }
        catch (Exception ex) { ViewModel.StatusMessage = $"压缩失败：{ex.Message}"; }
        finally { HideProgress(); ViewModel.Refresh(); }
    }

    private async void ArchiveGridView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        var items = e.Items.OfType<ArchiveItem>()
            .Where(x => !x.IsDirectory && !string.IsNullOrEmpty(x.Path))
            .ToList();

        if (items.Count == 0) { e.Cancel = true; return; }

        var tempDir = Path.Combine(Path.GetTempPath(), "DriftZipApp_Drag", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var files = new List<StorageFile>();

        foreach (var item in items)
        {
            try
            {
                var dest = Path.Combine(tempDir, item.Name);
                File.Copy(item.Path!, dest, true);
                if (File.Exists(dest))
                    files.Add(await StorageFile.GetFileFromPathAsync(dest));
            }
            catch { }
        }

        if (files.Count > 0)
        {
            e.Data.SetStorageItems(files);
            e.Data.RequestedOperation = DataPackageOperation.Copy;
        }
        else e.Cancel = true;
    }
}

// ═════════════════════════════════════════════════════════
//  HomeViewModel
// ═════════════════════════════════════════════════════════
public partial class HomeViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<ArchiveItem> archiveItems = new();
    [ObservableProperty] private string statusMessage = "准备就绪";
    [ObservableProperty] private ArchiveItem? selectedArchive;
    [ObservableProperty] private BrowseMode browseMode = BrowseMode.ScanArchives;
    [ObservableProperty] private string currentPath = "";
    [ObservableProperty] private bool canGoBack = false;
    [ObservableProperty] private bool canGoForward = false;

    private readonly Stack<string> _history = new();
    private readonly Stack<string> _forward = new();
    private List<string> _clipboard = new();
    private bool _clipboardCut = false;

    private readonly string[] _recentDirs =
    {
        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
    };

    public void Refresh()
    {
        if (BrowseMode == BrowseMode.ScanArchives) LoadRecentArchives();
        else LoadDirectory(CurrentPath);
    }

    public void SwitchBrowseMode(BrowseMode mode)
    {
        BrowseMode = mode;
        CurrentPath = "";
        _history.Clear();
        _forward.Clear();
        CanGoBack = false;
        CanGoForward = false;

        if (mode == BrowseMode.ScanArchives) LoadRecentArchives();
        else LoadDrives();
    }

    public void NavigateTo(string path)
    {
        if (!string.IsNullOrEmpty(CurrentPath)) _history.Push(CurrentPath);
        _forward.Clear();
        CanGoBack = _history.Count > 0;
        CanGoForward = false;

        if (string.IsNullOrEmpty(path)) { CurrentPath = ""; LoadDrives(); }
        else if (Directory.Exists(path)) { CurrentPath = path; LoadDirectory(path); }
    }

    public void GoBack()
    {
        if (_history.Count == 0) return;
        if (!string.IsNullOrEmpty(CurrentPath)) _forward.Push(CurrentPath);
        var prev = _history.Pop();
        CurrentPath = prev;
        CanGoBack = _history.Count > 0;
        CanGoForward = _forward.Count > 0;

        if (string.IsNullOrEmpty(prev)) LoadDrives();
        else LoadDirectory(prev);
    }

    public void GoForward()
    {
        if (_forward.Count == 0) return;
        if (!string.IsNullOrEmpty(CurrentPath)) _history.Push(CurrentPath);
        var next = _forward.Pop();
        CurrentPath = next;
        CanGoBack = _history.Count > 0;
        CanGoForward = _forward.Count > 0;

        if (string.IsNullOrEmpty(next)) LoadDrives();
        else LoadDirectory(next);
    }

    public void GoUp()
    {
        if (string.IsNullOrEmpty(CurrentPath)) { NavigateTo(""); return; }
        var parent = Directory.GetParent(CurrentPath)?.FullName;
        NavigateTo(parent ?? "");
    }

    public void Search(string keyword)
    {
        if (string.IsNullOrEmpty(CurrentPath) || !Directory.Exists(CurrentPath)) return;

        ArchiveItems.Clear();
        try
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(CurrentPath))
            {
                var name = Path.GetFileName(entry);
                if (!name.Contains(keyword, StringComparison.OrdinalIgnoreCase)) continue;
                try
                {
                    var isDir = Directory.Exists(entry);
                    ArchiveItems.Add(new ArchiveItem
                    {
                        Name = name, Path = entry, IsDirectory = isDir,
                        Size = isDir ? 0 : (uint)new FileInfo(entry).Length,
                        Modified = isDir ? new DirectoryInfo(entry).LastWriteTime : new FileInfo(entry).LastWriteTime,
                    });
                }
                catch { }
            }
            StatusMessage = $"找到 {ArchiveItems.Count} 项";
        }
        catch (Exception ex) { StatusMessage = $"搜索失败：{ex.Message}"; }
    }

    public void SetClipboard(List<string> paths, bool cut) { _clipboard = paths; _clipboardCut = cut; }

    public void Paste()
    {
        if (_clipboard.Count == 0) return;
        var dest = CurrentPath;
        if (string.IsNullOrEmpty(dest) || !Directory.Exists(dest)) { StatusMessage = "无法粘贴到此位置"; return; }

        try
        {
            foreach (var src in _clipboard)
            {
                var name = Path.GetFileName(src);
                var target = Path.Combine(dest, name);
                if (File.Exists(src))
                {
                    if (_clipboardCut) File.Move(src, target);
                    else File.Copy(src, target, true);
                }
                else if (Directory.Exists(src))
                {
                    if (_clipboardCut) Directory.Move(src, target);
                    else CopyDirectory(src, target);
                }
            }
            StatusMessage = _clipboardCut ? "剪切粘贴完成" : "复制粘贴完成";
            if (_clipboardCut) _clipboard.Clear();
        }
        catch (Exception ex) { StatusMessage = $"粘贴失败：{ex.Message}"; }
    }

    private static void CopyDirectory(string src, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(src))
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), true);
        foreach (var dir in Directory.GetDirectories(src))
            CopyDirectory(dir, Path.Combine(dest, Path.GetFileName(dir)));
    }

    private void LoadDrives()
    {
        ArchiveItems.Clear();
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady) continue;
            try
            {
                ArchiveItems.Add(new ArchiveItem
                {
                    Name = $"{drive.Name} ({drive.VolumeLabel})",
                    Path = drive.RootDirectory.FullName,
                    IsDirectory = true,
                });
            }
            catch { }
        }
        StatusMessage = $"共 {ArchiveItems.Count} 个驱动器";
    }

    private void LoadDirectory(string path)
    {
        ArchiveItems.Clear();
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) { LoadDrives(); return; }

        try
        {
            foreach (var dir in Directory.EnumerateDirectories(path))
            {
                try
                {
                    var info = new DirectoryInfo(dir);
                    ArchiveItems.Add(new ArchiveItem { Name = info.Name, Path = dir, IsDirectory = true, Modified = info.LastWriteTime });
                }
                catch { }
            }
            foreach (var file in Directory.EnumerateFiles(path))
            {
                try
                {
                    var info = new FileInfo(file);
                    ArchiveItems.Add(new ArchiveItem
                    {
                        Name = info.Name, Path = file, IsDirectory = false,
                        Size = (uint)info.Length, Modified = info.LastWriteTime,
                    });
                }
                catch { }
            }
            StatusMessage = $"共 {ArchiveItems.Count} 项";
        }
        catch (Exception ex) { StatusMessage = $"读取失败：{ex.Message}"; }
    }

    private void LoadRecentArchives()
    {
        ArchiveItems.Clear();
        foreach (var dir in _recentDirs)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;
            try
            {
                foreach (var ext in new[] { "*.7z", "*.zip", "*.rar", "*.tar.gz" })
                {
                    foreach (var file in Directory.EnumerateFiles(dir, ext))
                    {
                        var info = new FileInfo(file);
                        ArchiveItems.Add(new ArchiveItem
                        {
                            Name = info.Name, Path = file, IsDirectory = false,
                            Size = (uint)info.Length, Modified = info.LastWriteTime,
                        });
                    }
                }
            }
            catch (Exception ex) { Logger.Warn($"扫描 {dir} 失败：{ex.Message}", "HomeViewModel"); }
        }
        StatusMessage = $"共发现 {ArchiveItems.Count} 个压缩包";
    }
}