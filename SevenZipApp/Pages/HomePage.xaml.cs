// SPDX-License-Identifier: GPL-2.0-or-later

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SevenZipApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace SevenZipApp.Pages;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel { get; } = new();

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
        if (ArchiveGridView.SelectedItem is not ArchiveItem item) return;
        if (string.IsNullOrEmpty(item.Path)) return;
        OpenItem(item);
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

    // 工具栏按钮
    private void Back_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.GoBack();
        UpdateToolbar();
    }

    private void Forward_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.GoForward();
        UpdateToolbar();
    }

    private void Up_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.GoUp();
        UpdateToolbar();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.Refresh();
        UpdateToolbar();
    }

    private void PathBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var q = args.QueryText?.Trim();
        if (string.IsNullOrEmpty(q)) return;

        // 如果是路径，导航；否则在当前位置搜索
        if (Directory.Exists(q))
        {
            ViewModel.NavigateTo(q);
        }
        else if (File.Exists(q))
        {
            this.Frame.Navigate(typeof(ArchivePreviewPage), q);
        }
        else
        {
            ViewModel.Search(q);
        }
        UpdateToolbar();
    }

    // 右键菜单
    private void Open_Click(object sender, RoutedEventArgs e)
    {
        if (ArchiveGridView.SelectedItem is ArchiveItem item)
            OpenItem(item);
    }

    private async void Rename_Click(object sender, RoutedEventArgs e)
    {
        if (ArchiveGridView.SelectedItem is not ArchiveItem item) return;
        if (ViewModel.BrowseMode != BrowseMode.ThisPC) return;
        if (string.IsNullOrEmpty(item.Path)) return;

        var box = new TextBox { Text = item.Name };
        var dialog = new ContentDialog
        {
            Title = "重命名",
            Content = box,
            PrimaryButtonText = "确定",
            CloseButtonText = "取消",
            XamlRoot = this.XamlRoot,
        };
        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return;

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
        catch (Exception ex)
        {
            ViewModel.StatusMessage = $"重命名失败：{ex.Message}";
        }
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        var paths = ArchiveGridView.SelectedItems
            .OfType<ArchiveItem>()
            .Where(x => !string.IsNullOrEmpty(x.Path))
            .Select(x => x.Path!)
            .ToList();
        ViewModel.SetClipboard(paths, cut: false);
        ViewModel.StatusMessage = $"已复制 {paths.Count} 项";
    }

    private void Cut_Click(object sender, RoutedEventArgs e)
    {
        var paths = ArchiveGridView.SelectedItems
            .OfType<ArchiveItem>()
            .Where(x => !string.IsNullOrEmpty(x.Path))
            .Select(x => x.Path!)
            .ToList();
        ViewModel.SetClipboard(paths, cut: true);
        ViewModel.StatusMessage = $"已剪切 {paths.Count} 项";
    }

    private void Paste_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.Paste();
        ViewModel.Refresh();
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        var items = ArchiveGridView.SelectedItems.OfType<ArchiveItem>().ToList();
        if (items.Count == 0) return;

        var dialog = new ContentDialog
        {
            Title = "删除",
            Content = $"确认删除 {items.Count} 项？",
            PrimaryButtonText = "删除",
            CloseButtonText = "取消",
            XamlRoot = this.XamlRoot,
        };
        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return;

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
        catch (Exception ex)
        {
            ViewModel.StatusMessage = $"删除失败：{ex.Message}";
        }
    }

    private void ViewMode_Click(object sender, RoutedEventArgs e)
    {
        string? tag = (sender as MenuFlyoutItem)?.Tag?.ToString();
        switch (tag)
        {
            case "Details":    ArchiveGridView.ItemTemplate = (DataTemplate)Resources["DetailsTemplate"]; break;
            case "LargeIcons": ArchiveGridView.ItemTemplate = (DataTemplate)Resources["LargeIconTemplate"]; break;
            case "SmallIcons": ArchiveGridView.ItemTemplate = (DataTemplate)Resources["SmallIconTemplate"]; break;
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
        else
        {
            PathBox.Text = "";
        }
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
}

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
        if (!string.IsNullOrEmpty(CurrentPath))
            _history.Push(CurrentPath);
        _forward.Clear();
        CanGoBack = _history.Count > 0;
        CanGoForward = false;

        if (string.IsNullOrEmpty(path))
        {
            CurrentPath = "";
            LoadDrives();
        }
        else if (Directory.Exists(path))
        {
            CurrentPath = path;
            LoadDirectory(path);
        }
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
        if (string.IsNullOrEmpty(CurrentPath))
        {
            NavigateTo("");
            return;
        }

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
                        Name = name,
                        Path = entry,
                        IsDirectory = isDir,
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

    public void SetClipboard(List<string> paths, bool cut)
    {
        _clipboard = paths;
        _clipboardCut = cut;
    }

    public void Paste()
    {
        if (_clipboard.Count == 0) return;
        var dest = CurrentPath;
        if (string.IsNullOrEmpty(dest) || !Directory.Exists(dest))
        {
            StatusMessage = "无法粘贴到此位置";
            return;
        }

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
                    ArchiveItems.Add(new ArchiveItem
                    {
                        Name = info.Name, Path = dir, IsDirectory = true,
                        Modified = info.LastWriteTime,
                    });
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
