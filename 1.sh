cd /workspace/SevenZipWinUI

# ═══════════════════════════════════════════════════════
# 文件 1: ViewMode.cs
# ═══════════════════════════════════════════════════════
cat > SevenZipApp/Services/ViewMode.cs <<'EOF'
// SPDX-License-Identifier: GPL-2.0-or-later

namespace SevenZipApp.Services;

public enum ViewMode
{
    Details,
    LargeIcons,
    SmallIcons,
}

public enum BrowseMode
{
    ScanArchives,
    ThisPC,
}
EOF

# ═══════════════════════════════════════════════════════
# 文件 2: ArchiveItem.cs
# ═══════════════════════════════════════════════════════
cat > SevenZipApp/Services/ArchiveItem.cs <<'EOF'
// SPDX-License-Identifier: GPL-2.0-or-later

using System;
using System.IO;

namespace SevenZipApp;

public class ArchiveItem
{
    public string Name { get; set; } = "";
    public string? Path { get; set; }
    public uint Size { get; set; }
    public int Count { get; set; }
    public bool IsDirectory { get; set; }
    public DateTime Modified { get; set; } = DateTime.MinValue;

    public string SizeText => IsDirectory
        ? "<文件夹>"
        : Size >= 1024 * 1024 ? $"{Size / 1024.0 / 1024.0:F1} MB"
        : Size >= 1024 ? $"{Size / 1024.0:F0} KB"
        : $"{Size} B";

    public string ModifiedText => Modified == DateTime.MinValue
        ? ""
        : Modified.ToString("yyyy-MM-dd HH:mm");

    public string IconGlyph => IsDirectory ? "\uE8B7" : "\uE8A5";
}
EOF

# ═══════════════════════════════════════════════════════
# 文件 3: ArchiveEntry.cs
# ═══════════════════════════════════════════════════════
cat > SevenZipApp/Pages/ArchiveEntry.cs <<'EOF'
// SPDX-License-Identifier: GPL-2.0-or-later

using System;

namespace SevenZipApp.Pages;

public class ArchiveEntry
{
    public string Name { get; set; } = "";
    public string FullPath { get; set; } = "";
    public ulong Size { get; set; }
    public DateTime Modified { get; set; }
    public bool IsDirectory { get; set; }

    public string SizeText => IsDirectory ? "<文件夹>" : $"{Size:N0} B";
    public string ModifiedText => Modified == DateTime.MinValue
        ? ""
        : Modified.ToString("yyyy-MM-dd HH:mm");

    public string IconGlyph => IsDirectory ? "\uE8B7" : "\uE8A5";
}
EOF

# ═══════════════════════════════════════════════════════
# 文件 4: HomePage.xaml
# ═══════════════════════════════════════════════════════
cat > SevenZipApp/Pages/HomePage.xaml <<'XAMLEOF'
<!--
SPDX-License-Identifier: GPL-2.0-or-later
-->
<Page
    x:Class="SevenZipApp.Pages.HomePage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:SevenZipApp"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d"
    Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

    <Page.Resources>
        <DataTemplate x:Key="LargeIconTemplate" x:DataType="local:ArchiveItem">
            <StackPanel Width="120" Padding="8" HorizontalAlignment="Center">
                <FontIcon Glyph="{x:Bind IconGlyph}" FontSize="48" HorizontalAlignment="Center"/>
                <TextBlock Text="{x:Bind Name}" TextWrapping="Wrap" FontSize="12"
                           MaxLines="2" TextTrimming="CharacterEllipsis"
                           HorizontalAlignment="Center" Margin="0,4,0,0"/>
            </StackPanel>
        </DataTemplate>

        <DataTemplate x:Key="SmallIconTemplate" x:DataType="local:ArchiveItem">
            <StackPanel Orientation="Horizontal" Padding="8">
                <FontIcon Glyph="{x:Bind IconGlyph}" FontSize="16"/>
                <TextBlock Text="{x:Bind Name}" Margin="8,0,0,0" FontSize="12"
                           VerticalAlignment="Center"/>
            </StackPanel>
        </DataTemplate>

        <DataTemplate x:Key="DetailsTemplate" x:DataType="local:ArchiveItem">
            <Grid Padding="8,4" MinWidth="600">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="120"/>
                    <ColumnDefinition Width="160"/>
                </Grid.ColumnDefinitions>
                <StackPanel Grid.Column="0" Orientation="Horizontal" Spacing="8">
                    <FontIcon Glyph="{x:Bind IconGlyph}" FontSize="16" VerticalAlignment="Center"/>
                    <TextBlock Text="{x:Bind Name}" VerticalAlignment="Center"/>
                </StackPanel>
                <TextBlock Grid.Column="1" Text="{x:Bind SizeText}" VerticalAlignment="Center"/>
                <TextBlock Grid.Column="2" Text="{x:Bind ModifiedText}" VerticalAlignment="Center"/>
            </Grid>
        </DataTemplate>
    </Page.Resources>

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- 工具栏 -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Spacing="8" Padding="12,8">
            <Button x:Name="BackButton" Content="←" Click="Back_Click" Visibility="Collapsed" ToolTipService.ToolTip="后退"/>
            <Button x:Name="ForwardButton" Content="→" Click="Forward_Click" Visibility="Collapsed" ToolTipService.ToolTip="前进"/>
            <Button x:Name="UpButton" Content="↑" Click="Up_Click" Visibility="Collapsed" ToolTipService.ToolTip="上级"/>
            <Button Content="刷新" Click="Refresh_Click"/>
            <AutoSuggestBox x:Name="PathBox" Width="400" QuerySubmitted="PathBox_QuerySubmitted" PlaceholderText="输入路径或搜索"/>
            <TextBlock x:Name="StatusText"
                       Text="{x:Bind ViewModel.StatusMessage, Mode=OneWay}"
                       Foreground="Gray" VerticalAlignment="Center" Margin="12,0,0,0"/>
        </StackPanel>

        <!-- 面包屑 -->
        <StackPanel x:Name="BreadcrumbPanel" Grid.Row="1" Orientation="Horizontal"
                    Spacing="4" Padding="12,0,12,8" Visibility="Collapsed"/>

        <GridView x:Name="ArchiveGridView"
                  Grid.Row="2"
                  Padding="12"
                  IsItemClickEnabled="True"
                  ItemClick="ArchiveGridView_ItemClick"
                  DoubleTapped="ArchiveGridView_DoubleTapped"
                  ItemsSource="{x:Bind ViewModel.ArchiveItems, Mode=OneWay}"
                  SelectionMode="Extended"
                  ItemTemplate="{StaticResource LargeIconTemplate}">

            <GridView.ContextFlyout>
                <MenuFlyout>
                    <MenuFlyoutSubItem Text="视图">
                        <MenuFlyoutItem Text="详细信息" Click="ViewMode_Click" Tag="Details"/>
                        <MenuFlyoutItem Text="大图标" Click="ViewMode_Click" Tag="LargeIcons"/>
                        <MenuFlyoutItem Text="小图标" Click="ViewMode_Click" Tag="SmallIcons"/>
                    </MenuFlyoutSubItem>
                    <MenuFlyoutSeparator/>
                    <MenuFlyoutItem Text="打开" Click="Open_Click"/>
                    <MenuFlyoutItem Text="重命名" Click="Rename_Click"/>
                    <MenuFlyoutItem Text="复制" Click="Copy_Click"/>
                    <MenuFlyoutItem Text="剪切" Click="Cut_Click"/>
                    <MenuFlyoutItem Text="粘贴" Click="Paste_Click"/>
                    <MenuFlyoutItem Text="删除" Click="Delete_Click"/>
                    <MenuFlyoutSeparator/>
                    <MenuFlyoutItem Text="扫描压缩包" Click="BrowseMode_Click" Tag="ScanArchives"/>
                    <MenuFlyoutItem Text="浏览此电脑" Click="BrowseMode_Click" Tag="ThisPC"/>
                </MenuFlyout>
            </GridView.ContextFlyout>
        </GridView>
    </Grid>
</Page>
XAMLEOF

# ═══════════════════════════════════════════════════════
# 文件 5: HomePage.xaml.cs
# ═══════════════════════════════════════════════════════
cat > SevenZipApp/Pages/HomePage.xaml.cs <<'CSEOF'
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
CSEOF

# ═══════════════════════════════════════════════════════
# 文件 6: ArchivePreviewPage.xaml
# ═══════════════════════════════════════════════════════
cat > SevenZipApp/Pages/ArchivePreviewPage.xaml <<'XAMLEOF'
<!-- SPDX-License-Identifier: GPL-2.0-or-later -->
<Page
    x:Class="SevenZipApp.Pages.ArchivePreviewPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:SevenZipApp.Pages"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d"
    Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

    <Grid Padding="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <StackPanel Grid.Row="0" Orientation="Horizontal" Spacing="12" Margin="0,0,0,12">
            <Button x:Name="GoUpButton" Content="↑ 返回上级" Click="GoUp_Click" Visibility="Collapsed"/>
            <TextBlock x:Name="ArchivePathText" VerticalAlignment="Center"
                       Style="{StaticResource BodyStrongTextBlockStyle}" Text="未加载"/>
            <TextBlock x:Name="CurrentPathText" VerticalAlignment="Center"
                       Foreground="Gray" Text=""/>
        </StackPanel>

        <ListView x:Name="EntryList"
                  Grid.Row="1"
                  CanDragItems="True"
                  DragItemsStarting="EntryList_DragItemsStarting"
                  DoubleTapped="EntryList_DoubleTapped"
                  SelectionMode="Extended">
            <ListView.ItemTemplate>
                <DataTemplate x:DataType="local:ArchiveEntry">
                    <Grid Padding="8,4">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="100"/>
                            <ColumnDefinition Width="150"/>
                        </Grid.ColumnDefinitions>
                        <StackPanel Grid.Column="0" Orientation="Horizontal" Spacing="8">
                            <FontIcon Glyph="{x:Bind IconGlyph}" FontSize="16"/>
                            <TextBlock Text="{x:Bind Name}" VerticalAlignment="Center"/>
                        </StackPanel>
                        <TextBlock Grid.Column="1" Text="{x:Bind SizeText}"/>
                        <TextBlock Grid.Column="2" Text="{x:Bind ModifiedText}"/>
                    </Grid>
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
    </Grid>
</Page>
XAMLEOF

# ═══════════════════════════════════════════════════════
# 文件 7: ArchivePreviewPage.xaml.cs
# ═══════════════════════════════════════════════════════
cat > SevenZipApp/Pages/ArchivePreviewPage.xaml.cs <<'CSEOF'
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
CSEOF

echo "✅ 全部文件已写入"