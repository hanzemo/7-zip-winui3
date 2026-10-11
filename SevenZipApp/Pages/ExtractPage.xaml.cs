// SPDX-License-Identifier: GPL-2.0-or-later
//
// ExtractPage.xaml.cs

using WinRT.Interop;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace SevenZipApp.Pages;

public sealed partial class ExtractPage : Page
{
    private string _archivePath = string.Empty;
    private string _outputPath = string.Empty;

    public ExtractPage()
    {
        this.InitializeComponent();

        Logger.Info("ExtractPage 初始化完成", "ExtractPage");
    }

    private async void SelectArchive_Click(object sender, RoutedEventArgs e)
    {
        Logger.Info("打开压缩包选择器", "ExtractPage");

        var picker = new FileOpenPicker
        {
            ViewMode = PickerViewMode.List,
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
        };
        foreach (var ext in new[] { ".7z", ".zip", ".rar", ".tar", ".gz", ".bz2", ".xz" })
            picker.FileTypeFilter.Add(ext);

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            _archivePath = file.Path;
            ArchivePathBox.Text = _archivePath;
            Logger.Info($"选择了压缩包：{_archivePath}", "ExtractPage");
        }
    }

    private async void SelectOutput_Click(object sender, RoutedEventArgs e)
    {
        Logger.Info("打开目标文件夹选择器", "ExtractPage");

        var picker = new FolderPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        var folder = await picker.PickSingleFolderAsync();
        if (folder != null)
        {
            _outputPath = folder.Path;
            ExtractToBox.Text = _outputPath;
            Logger.Info($"选择了解压目录：{_outputPath}", "ExtractPage");
        }
    }

    private async void Extract_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_archivePath))
        {
            StatusTextBlock.Text = "请先选择压缩包";
            return;
        }
        if (string.IsNullOrEmpty(_outputPath))
        {
            StatusTextBlock.Text = "请选择解压目录";
            return;
        }

        var password = string.IsNullOrEmpty(PasswordBox.Password) ? null : PasswordBox.Password;

        StatusTextBlock.Text = "正在解压...";
        Logger.Info($"开始解压：{_archivePath} -> {_outputPath}", "ExtractPage");

        try
        {
            var engine = new SevenZipEngine();
            await Task.Run(() => engine.Extract(_archivePath, _outputPath, password, null, CancellationToken.None));
            StatusTextBlock.Text = $"解压完成：{_outputPath}";
            Logger.Info("解压操作完成", "ExtractPage");
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"解压失败：{ex.Message}";
            Logger.Error($"解压失败：{ex.Message}", "ExtractPage");
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Logger.Info("取消解压", "ExtractPage");
        _archivePath = string.Empty;
        _outputPath = string.Empty;
        ArchivePathBox.Text = string.Empty;
        ExtractToBox.Text = string.Empty;
        PasswordBox.Password = string.Empty;
        StatusTextBlock.Text = "已取消";
    }
}
