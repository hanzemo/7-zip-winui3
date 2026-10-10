// SPDX-License-Identifier: GPL-2.0-or-later
//
// TranslateViewModel.cs —— 设置属性，更新 UI。

using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SevenZipApp.Interop;
using SevenZipApp.Services;

namespace SevenZipApp.Pages
{
    public class CompressViewModel
    {
        public CompressViewModel()
        {
            SelectedFiles = new ObservableCollection<FileInfo>();
            StatusMessage = "准备就绪";
            CompressionLevel = CompressionLevel.Normal;
            OutputFileName = "archive.7z";
            Password = string.Empty;
            Encrypt = false;
            MultiThreaded = true;
            Solid = true;
        }

        [ObservableProperty]
        private ObservableCollection<FileInfo> selectedFiles;

        [ObservableProperty]
        private string outputFileName;

        [ObservableProperty]
        private string statusMessage;

        [ObservableProperty]
        private CompressionLevel compressionLevel;

        [ObservableProperty]
        private string password;

        [ObservableProperty]
        private bool encrypt;

        [ObservableProperty]
        private bool multiThreaded;

        [ObservableProperty]
        private bool solid;

        [RelayCommand]
        private async Task SelectFiles()
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.List;
            picker.FileTypeFilter.Add("*");
            var files = await picker.PickMultipleFilesAsync();
            if (files != null)
            {
                foreach (var file in files)
                {
                    SelectedFiles.Add(new FileInfo(file.Path));
                }
            }
        }

        [RelayCommand]
        private async Task Compress()
        {
            try
            {
                StatusMessage = "开始压缩...";
                var options = new CompressionOptions
                {
                    Format = OutputFormat.SevenZip,
                    Level = CompressionLevel.Normal,
                    Solid = Solid,
                    MultiThreaded = MultiThreaded,
                    Encrypt = Encrypt,
                    Password = Password,
                    SourceFiles = SelectedFiles.Select(f => f.FullName).ToList(),
                    RootPrefix = ""
                };

                var engine = new SevenZipEngine();
                await engine.CompressAsync(options, OutputFileName, null, new CancellationToken());

                StatusMessage = "压缩完成!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"压缩失败: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task Cancel()
        {
            StatusMessage = "已取消";
        }
    }
}
