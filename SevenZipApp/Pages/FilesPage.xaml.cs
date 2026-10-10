// SPDX-License-Identifier: GPL-2.0-or-later
//
// FilesPage.xaml.cs

using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SevenZipApp.Interop;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace SevenZipApp.Pages
{
    public sealed partial class FilesPage : Page
    {
        private ArchiveListViewModel _viewModel = new ArchiveListViewModel();

        public FilesPage()
        {
            this.InitializeComponent();
            this.DataContext = _viewModel;

            Logger.Info("FilesPage 初始化完成", "FilesPage");
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Logger.Warn("Back button clicked", "FilesPage");
            var frame = this.FindChild<Frame>(this.Parent, "MainFrame");
            if (frame != null && frame.CanGoBack)
            {
                frame.GoBack();
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.RefreshCommand.Execute(null);
        }

        private void Extract_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ExtractCommand.Execute(null);
        }

        private void FilesGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var selectedItem = e.ClickedItem as ArchiveItem;
            if (selectedItem != null && !selectedItem.IsFolder)
            {
                _viewModel.ExtractItemSelectedCommand.Execute(selectedItem);
            }
            else if (selectedItem != null && selectedItem.IsFolder)
            {
                Logger.Warn("正在点击文件夹", "FilesPage");
                StatusTextBlock.Text = "进入文件夹功能暂未实现";
            }
        }

        private T? FindChild<T>(DependencyObject parent, string name) where T : DependencyObject
        {
            if (parent == null) return null;
            int childCount = VisualTreeHelper.GetChildCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild && typedChild.Name == name)
                {
                    return typedChild;
                }
                T? result = FindChild<T>(child, name);
                if (result != null) return result;
            }
            return null;
        }
    }

    public class ArchiveListViewModel
    {
        public ArchiveListViewModel()
        {
            Items = new ObservableCollection<ArchiveItem>();
            StatusMessage = "请选择一个压缩包";
            _selectedArchivePath = string.Empty;
        }

        [ObservableProperty]
        private ObservableCollection<ArchiveItem> items;

        [ObservableProperty]
        private int totalCount;

        [ObservableProperty]
        private string statusMessage;

        [ObservableProperty]
        private string _selectedArchivePath;

        [RelayCommand]
        private async Task Extract()
        {
            var savePicker = new FolderPicker();
            savePicker.FileTypeFilter.Add("*");
            string? folderPath = await savePicker.PickSingleFolderAsync();
            if (folderPath != null)
            {
                StatusMessage = "开始解压...";
                try
                {
                    var engine = new SevenZipEngine();
                    await Task.Run(() => engine.Extract(_selectedArchivePath, folderPath, null, null, new CancellationToken()));
                    StatusMessage = "解压完成!";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"解压失败: {ex.Message}";
                }
            }
        }
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
            Logger.Info($"解压整个归档：{_selectedArchivePath}", "ArchiveListViewModel");
            var savePicker = new FolderPicker();
            savePicker.FileTypeFilter.Add("*");
            string? folderPath = await savePicker.PickSingleFolderAsync();
            if (folderPath != null)
            {
                StatusMessage = "开始解压...";
                try
                {
                    var engine = new SevenZipEngine();
                    await Task.Run(() => engine.Extract(_selectedArchivePath, folderPath, null, null, new CancellationToken()));
                    StatusMessage = "解压完成!";
                    Logger.Info("解压完成", "ArchiveListViewModel");
                }
                catch (Exception ex)
                {
                    StatusMessage = $"解压失败：{ex.Message}";
                    Logger.Error($"解压失败：{ex.Message}", "ArchiveListViewModel");
                }
            }
        }

        [RelayCommand]
        private async Task ExtractItemSelected()
        {
            var selectedItem = Items.FirstOrDefault() as ArchiveItem;
            if (selectedItem != null && !selectedItem.IsFolder)
            {
                var savePicker = new FolderPicker();
                savePicker.FileTypeFilter.Add("*");
                string? folderPath = await savePicker.PickSingleFolderAsync();
                if (folderPath != null)
                {
                    StatusMessage = "解压单个项目...";
                    Logger.Info($"解压单个文件：{selectedItem.Name}", "ArchiveListViewModel");
                    try
                    {
                        // TODO: 实现单文件解压
                        StatusMessage = "解压完成!";
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"解压失败：{ex.Message}";
                        Logger.Error($"解压单个文件失败：{ex.Message}", "ArchiveListViewModel");
                    }
                }
            }
            else
            {
                Logger.Warn("未选择文件或选择文件夹", "ArchiveListViewModel");
                StatusMessage = "请选择文件";
            }
        }

        public void SetArchivePath(string archivePath)
        {
            Logger.Info($"设置归档路径：{archivePath}", "ArchiveListViewModel");
            StatusMessage = "加载中...";
            _selectedArchivePath = archivePath;
            RefreshCommand.Execute(null);
        }

        public async Task NavigateToFolder(ArchiveItem folder)
        {
            if (folder.IsFolder)
            {
                Logger.Info($"进入文件夹：{folder.Name}", "ArchiveListViewModel");
                StatusMessage = $"进入 {folder.Name}";
                // TODO: 实现文件夹导航
            }
        }
    }
}
