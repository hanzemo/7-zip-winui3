// SPDX-License-Identifier: GPL-2.0-or-later
//
// FilesPage.xaml.cs

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace SevenZipApp.Pages;

public sealed partial class FilesPage : Page
{
    public ArchiveListViewModel ViewModel { get; } = new();

    public FilesPage()
    {
        this.InitializeComponent();
        this.DataContext = ViewModel;

        Logger.Info("FilesPage 初始化完成", "FilesPage");
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        Logger.Warn("Back button clicked", "FilesPage");
        var frame = FindMainFrame();
        if (frame != null && frame.CanGoBack)
        {
            frame.GoBack();
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.RefreshCommand.Execute(null);
    }

    private void Extract_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ExtractCommand.Execute(null);
    }

    private void FilesListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not ArchiveItem selectedItem)
            return;

        if (selectedItem.IsFolder)
        {
            StatusTextBlock.Text = "进入文件夹功能暂未实现";
            Logger.Warn("正在点击文件夹", "FilesPage");
            return;
        }

        ViewModel.ExtractItemSelectedCommand.Execute(selectedItem);
    }

    private Frame? FindMainFrame()
    {
        var parent = this.Parent as DependencyObject;
        while (parent != null)
        {
            if (parent is Frame f && f.Name == "MainFrame")
                return f;
            parent = VisualTreeHelper.GetParent(parent);
        }
        return null;
    }
}

public class ArchiveListViewModel : ObservableObject
{
    private string _selectedArchivePath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ArchiveItem> items = new();

    [ObservableProperty]
    private int totalCount;

    [ObservableProperty]
    private string statusMessage = "请选择一个压缩包";

    [RelayCommand]
    private async Task Refresh()
    {
        if (string.IsNullOrEmpty(_selectedArchivePath))
        {
            StatusMessage = "未选择压缩包";
            Logger.Warn("刷新时未选择压缩包", "ArchiveListViewModel");
            return;
        }

        StatusMessage = "刷新中...";
        Logger.Info($"刷新归档内容：{_selectedArchivePath}", "ArchiveListViewModel");

        try
        {
            var engine = new SevenZipEngine();
            var items = await Task.Run(() => engine.ListArchive(_selectedArchivePath));

            Items.Clear();
            foreach (var item in items)
                Items.Add(item);

            TotalCount = items.Count;
            StatusMessage = $"显示 {items.Count} 个项目";
            Logger.Info($"刷新完成：{items.Count} 个项目", "ArchiveListViewModel");
        }
        catch (Exception ex)
        {
            StatusMessage = $"刷新失败：{ex.Message}";
            Logger.Error($"刷新失败：{ex.Message}", "ArchiveListViewModel");
        }
    }

    [RelayCommand]
    private async Task Extract()
    {
        if (string.IsNullOrEmpty(_selectedArchivePath))
        {
            StatusMessage = "未选择压缩包";
            return;
        }

        Logger.Info($"解压整个归档：{_selectedArchivePath}", "ArchiveListViewModel");

        var savePicker = new FolderPicker();
        var folder = await savePicker.PickSingleFolderAsync();
        if (folder == null) return;

        StatusMessage = "开始解压...";
        try
        {
            var engine = new SevenZipEngine();
            await Task.Run(() => engine.Extract(_selectedArchivePath, folder.Path, null, null, CancellationToken.None));
            StatusMessage = $"解压完成：{folder.Path}";
            Logger.Info("解压完成", "ArchiveListViewModel");
        }
        catch (Exception ex)
        {
            StatusMessage = $"解压失败：{ex.Message}";
            Logger.Error($"解压失败：{ex.Message}", "ArchiveListViewModel");
        }
    }

    [RelayCommand]
    private async Task ExtractItemSelected(ArchiveItem? item)
    {
        if (item == null || item.IsFolder)
        {
            Logger.Warn("未选择文件或选择文件夹", "ArchiveListViewModel");
            StatusMessage = "请选择文件";
            return;
        }

        var savePicker = new FolderPicker();
        var folder = await savePicker.PickSingleFolderAsync();
        if (folder == null) return;

        StatusMessage = "解压单个项目...";
        Logger.Info($"解压单个文件：{item.Name}", "ArchiveListViewModel");

        try
        {
            var engine = new SevenZipEngine();
            await Task.Run(() => engine.Extract(_selectedArchivePath, folder.Path, null, null, CancellationToken.None));
            StatusMessage = $"解压完成：{folder.Path}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"解压失败：{ex.Message}";
            Logger.Error($"解压单个文件失败：{ex.Message}", "ArchiveListViewModel");
        }
    }

    public void SetArchivePath(string archivePath)
    {
        Logger.Info($"设置归档路径：{archivePath}", "ArchiveListViewModel");
        StatusMessage = "加载中...";
        _selectedArchivePath = archivePath;
        _ = RefreshCommand.ExecuteAsync(null);
    }
}
