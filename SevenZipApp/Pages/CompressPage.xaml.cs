// SPDX-License-Identifier: GPL-2.0-or-later
//
// CompressPage.xaml.cs

using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace SevenZipApp.Pages
{
    public sealed partial class CompressPage : Page
    {
        private CompressViewModel _viewModel = new CompressViewModel();

        public CompressPage()
        {
            this.InitializeComponent();
            this.DataContext = _viewModel;

            Logger.Info("CompressPage 初始化完成", "CompressPage");
        }

        private async void SelectFiles_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("打开文件选择器", "CompressPage");
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.List;
            picker.FileTypeFilter.Add("*");
            var files = await picker.PickMultipleFilesAsync();
            if (files != null)
            {
                _viewModel.Files = new ObservableCollection<string>();
                foreach (var f in files)
                {
                    _viewModel.Files.Add(f.Path);
                }
                Logger.Info($"选择了 {files.Count} 个文件", "CompressPage");
            }
        }

        private async void Compress_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("开始压缩操作", "CompressPage");
            try
            {
                // TODO: 实现压缩逻辑
                Logger.Info("压缩操作提示：这里需要实现真实的压缩功能", "CompressPage");
            }
            catch (Exception ex)
            {
                Logger.Error($"压缩失败：{ex.Message}", "CompressPage");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("取消压缩", "CompressPage");
            this.NavigateBack();
        }

        private void NavigateBack()
        {
            // 返回上一页
        }
    }

    public class CompressViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<string> files;

        [ObservableProperty]
        private string outputPath = "archive.7z";

        [ObservableProperty]
        private string statusMessage = "准备压缩";

        [ObservableProperty]
        private CompressionLevel compressionLevel = CompressionLevel.Normal;

        [ObservableProperty]
        private bool solid = true;

        [ObservableProperty]
        private bool multiThreaded = true;

        [ObservableProperty]
        private bool encrypt;
    }
}
