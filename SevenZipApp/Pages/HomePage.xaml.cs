// SPDX-License-Identifier: GPL-2.0-or-later
//
// HomePage.xaml.cs

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace SevenZipApp.Pages;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel { get; } = new();

    public HomePage()
    {
        this.InitializeComponent();
        this.DataContext = ViewModel;

        Logger.Info("HomePage 初始化完成", "HomePage");
    }

    private void ArchiveGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not ArchiveItem selectedItem)
            return;

        Logger.Info($"选择归档：{selectedItem.Name}", "HomePage");
        ViewModel.SelectedArchive = selectedItem;
        ViewModel.StatusMessage = $"已选择：{selectedItem.Name}";
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenCommand.Execute(null);
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.RefreshCommand.Execute(null);
    }

    private void Extract_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ExtractToFolderCommand.Execute(null);
    }
}

public class HomeViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ArchiveItem> archiveItems = new();

    [ObservableProperty]
    private string statusMessage = "准备就绪";

    [ObservableProperty]
    private ArchiveItem? selectedArchive;

    private readonly string[] _recentDirs =
    {
        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        Environment.GetFolderPath(Environment.SpecialFolder.Downloads),
    };

    [RelayCommand]
    private async Task Open()
    {
        Logger.Info("打开压缩包选择器", "HomeViewModel");

        var picker = new FileOpenPicker
        {
            ViewMode = PickerViewMode.List,
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
        };
        foreach (var ext in new[] { ".7z", ".zip", ".rar", ".tar", ".gz", ".bz2", ".xz" })
            picker.FileTypeFilter.Add(ext);

        var file = await picker.PickSingleFileAsync();
        if (file == null) return;

        string path = file.Path;
        Logger.Info($"打开压缩包：{path}", "HomeViewModel");

        try
        {
            var engine = new SevenZipEngine();
            var items = await Task.Run(() => engine.ListArchive(path));

            SelectedArchive = new ArchiveItem
            {
                Name = Path.GetFileName(path),
                Count = items.Count,
                Path = path,
            };
            StatusMessage = $"已加载：{items.Count} 个项目";
            Logger.Info($"已加载 {items.Count} 个项目", "HomeViewModel");
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开失败：{ex.Message}";
            Logger.Error($"打开压缩包失败：{ex.Message}", "HomeViewModel");
        }
    }

    [RelayCommand]
    private async Task ExtractToFolder()
    {
        if (SelectedArchive == null)
        {
            StatusMessage = "请先打开一个压缩包";
            return;
        }

        Logger.Info($"解压到文件夹：{SelectedArchive.Path}", "HomeViewModel");

        var picker = new FolderPicker();
        var folder = await picker.PickSingleFolderAsync();
        if (folder == null) return;

        try
        {
            var engine = new SevenZipEngine();
            StatusMessage = "正在解压...";
            await Task.Run(() => engine.Extract(SelectedArchive.Path!, folder.Path, null, null, System.Threading.CancellationToken.None));
            StatusMessage = $"解压完成：{folder.Path}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"解压失败：{ex.Message}";
            Logger.Error($"解压失败：{ex.Message}", "HomeViewModel");
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        LoadRecentArchives();
    }

    private void LoadRecentArchives()
    {
        ArchiveItems.Clear();

        foreach (var dir in _recentDirs)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                continue;

            try
            {
                foreach (var ext in new[] { "*.7z", "*.zip", "*.rar", "*.tar.gz" })
                {
                    foreach (var file in Directory.EnumerateFiles(dir, ext))
                    {
                        var info = new FileInfo(file);
                        ArchiveItems.Add(new ArchiveItem
                        {
                            Name = info.Name,
                            Path = file,
                            Size = (uint)info.Length,
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"扫描 {dir} 失败：{ex.Message}", "HomeViewModel");
            }
        }

        StatusMessage = $"共发现 {ArchiveItems.Count} 个压缩包";
    }

    // 供 XAML Loaded 事件或页面首次显示时调用。
    public void LoadRecent() => LoadRecentArchives();
}
