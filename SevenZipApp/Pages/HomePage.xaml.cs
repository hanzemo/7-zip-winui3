// SPDX-License-Identifier: GPL-2.0-or-later
//
// HomePage.xaml.cs

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace SevenZipApp.Pages
{
    public sealed partial class HomePage : Page
    {
        public HomeViewModel ViewModel { get; } = new();

        public HomePage()
        {
            this.InitializeComponent();
            this.DataContext = ViewModel;

            Logger.Info("HomePage 初始化完成", "HomePage");
        }

        private void ArchiveGridView_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.LoadRecentArchives();
        }

        private void ArchiveGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var selectedItem = e.ClickedItem as ArchiveItem;
            if (selectedItem != null)
            {
                Logger.Info($"选择归档：{selectedItem.Name}", "HomePage");
                var frame = this.FindControl<Frame>(this.Content, "MainFrame");
                if (frame != null)
                {
                    frame.Navigate(typeof(FilesPage), selectedItem);
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.RefreshCommand.Execute(null);
        }

        private T? FindChild<T>(DependencyObject parent, string name) where T : DependencyObject
        {
            if (parent == null) return null;
            int count = VisualTreeHelper.GetChildCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T tchild && tchild.Name == name)
                    return tchild;
                var result = FindChild<T>(child, name);
                if (result != null) return result;
            }
            return null;
        }
    }

    public class HomeViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ArchiveItem> archiveItems = new();

        [ObservableProperty]
        private string statusMessage = "准备就绪";

        [RelayCommand]
        private async Task Open(string path)
        {
            Logger.Info($"打开压缩包：{path}", "HomeViewModel");
            try
            {
                var engine = new SevenZipEngine();
                var items = engine.ListArchive(path);
                var item = new ArchiveItem
                {
                    Name = Path.GetFileName(path),
                    Count = items.Count,
                    Path = path
                };
                SelectArchive(item);
                StatusMessage = $"已加载：{items.Count} 个项目";
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
            Logger.Info($"解压到文件夹操作", "HomeViewModel");
            var picker = new FolderPicker();
            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                StatusMessage = $"正在解压到：{folder.Path}...";
                // await 实现
            }
        }

        public void LoadRecentArchives()
        {
            Logger.Info("加载最近归档", "HomeViewModel");
            // 实现
        }

        public void Refresh()
        {
            Logger.Info("刷新操作", "HomeViewModel");
            LoadRecentArchives();
        }

        public void SelectArchive(ArchiveItem item)
        {
            Logger.Info($"选择归档：{item.Name}", "HomeViewModel");
            // 导航
        }
    }
}
