// SPDX-License-Identifier: GPL-2.0-or-later
//
// ExtractPage.xaml.cs

using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace SevenZipApp.Pages
{
    public sealed partial class ExtractPage : Page
    {
        private ExtractViewModel _viewModel = new ExtractViewModel();

        public ExtractPage()
        {
            this.InitializeComponent();
            this.DataContext = _viewModel;

            Logger.Info("ExtractPage 初始化完成", "ExtractPage");
        }

        private async void SelectArchive_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("打开压缩包选择器", "ExtractPage");
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.List;
            picker.FileTypeFilter.Add(".7z");
            picker.FileTypeFilter.Add(".zip");
            picker.FileTypeFilter.Add(".rar");
            picker.FileTypeFilter.Add(".tar");
            picker.FileTypeFilter.Add(".gz");
            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                _viewModel.ArchivePath = file.Path;
                Logger.Info($"选择了压缩包：{file.Path}", "ExtractPage");
            }
        }

        private async void SelectOutput_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("打开目标文件夹选择器", "ExtractPage");
            var picker = new FolderPicker();
            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                _viewModel.OutputPath = folder.Path;
                Logger.Info($"选择了解压目录：{folder.Path}", "ExtractPage");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("取消解压", "ExtractPage");
            this.NavigateBack();
        }

        private void NavigateBack()
        {
            // 返回上一页
        }
    }

    public class ExtractViewModel : ObservableObject
    {
        [ObservableProperty]
        private string archivePath = "";

        [ObservableProperty]
        private string outputPath = "";

        [ObservableProperty]
        private string statusMessage = "准备解压";

        [ObservableProperty]
        private string password = "";

        [RelayCommand]
        private async Task Extract()
        {
            Logger.Info($"开始解压：{archivePath} -> {outputPath}", "ExtractViewModel");
            try
            {
                var engine = new SevenZipEngine();
                await engine.ExtractAsync(archivePath, outputPath, password, null, new CancellationToken());
                StatusMessage = "解压完成!";
                Logger.Info("解压操作完成", "ExtractViewModel");
            }
            catch (Exception ex)
            {
                StatusMessage = $"解压失败：{ex.Message}";
                Logger.Error($"解压失败：{ex.Message}", "ExtractViewModel");
            }
        }
    }
}
