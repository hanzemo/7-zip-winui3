// SPDX-License-Identifier: GPL-2.0-or-later

using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Linq;

namespace DriftZipApp.Views;

public sealed partial class MainWindow : Window
{
    private bool _firstActivated = false;

    public MainWindow()
    {
        this.InitializeComponent();

        this.ExtendsContentIntoTitleBar = true;
        this.SetTitleBar(AppTitleBar);

        this.Activated += MainWindow_FirstActivated;
    }

    private void MainWindow_FirstActivated(object sender, WindowActivatedEventArgs args)
    {
        if (_firstActivated) return;
        _firstActivated = true;

        MainFrame.Navigate(typeof(Pages.HomePage));
        NavView.SelectedItem = NavView.MenuItems
            .OfType<NavigationViewItem>()
            .FirstOrDefault(i => i.Tag?.ToString() == "Home");
    }

    public void OpenArchive(string path)
    {
        if (string.IsNullOrEmpty(path)) return;

        MainFrame.Navigate(typeof(Pages.ArchivePage), path);

        NavView.SelectedItem = NavView.MenuItems
            .OfType<NavigationViewItem>()
            .FirstOrDefault(i => i.Tag?.ToString() == "Archive");
    }

    /// <summary>
    /// 处理来自 Windows 右键菜单的命令
    /// </summary>
    public void HandleShellCommand(string verb, string target)
    {
        Logger.Info($"HandleShellCommand: {verb} {target}", "MainWindow");

        DispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                var engine = new DriftZipEngine();
                string? dir;
                string? output;

                switch (verb)
                {
                    case "--extract-here":
                        dir = Path.GetDirectoryName(target);
                        if (string.IsNullOrEmpty(dir)) return;
                        await Task.Run(() => engine.Extract(target, dir!, null, null, default));
                        Logger.Info($"解压到此目录完成：{target}", "MainWindow");
                        break;

                    case "--extract-to":
                    {
                        var picker = new Windows.Storage.Pickers.FolderPicker();
                        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                        picker.FileTypeFilter.Add("*");
                        var folder = await picker.PickSingleFolderAsync();
                        if (folder == null) return;
                        await Task.Run(() => engine.Extract(target, folder.Path, null, null, default));
                        break;
                    }

                    case "--compress-here":
                        dir = Path.GetDirectoryName(target);
                        if (string.IsNullOrEmpty(dir)) return;
                        output = Path.Combine(dir, Path.GetFileNameWithoutExtension(target) + ".7z");
                        await Task.Run(() => engine.Compress(new CompressionOptions
                        {
                            Format = OutputFormat.SevenZip,
                            Level = CompressionLevel.Normal,
                            SourceFiles = new System.Collections.Generic.List<string> { target },
                        }, output, null, default));
                        Logger.Info($"压缩到此目录完成：{output}", "MainWindow");
                        break;

                    case "--compress-to":
                    {
                        var save = new Windows.Storage.Pickers.FileSavePicker();
                        save.SuggestedFileName = Path.GetFileNameWithoutExtension(target);
                        save.FileTypeChoices.Add("7z", new[] { ".7z" });
                        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                        WinRT.Interop.InitializeWithWindow.Initialize(save, hwnd);
                        var file = await save.PickSaveFileAsync();
                        if (file == null) return;
                        await Task.Run(() => engine.Compress(new CompressionOptions
                        {
                            Format = OutputFormat.SevenZip,
                            Level = CompressionLevel.Normal,
                            SourceFiles = new System.Collections.Generic.List<string> { target },
                        }, file.Path, null, default));
                        break;
                    }
                }

                if (MainFrame.Content is Pages.HomePage home)
                    home.ViewModel.Refresh();
            }
            catch (Exception ex)
            {
                Logger.Error($"HandleShellCommand 失败：{ex.Message}", "MainWindow");
            }
        });
    }


    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is not NavigationViewItem selectedItem) return;

        string? tag = selectedItem.Tag?.ToString();
        Logger.Info($"导航到页面：{tag}", "MainWindow");

        MainFrame.ContentTransitions = new TransitionCollection();

        Type? pageType = tag switch
        {
            "Home"         => typeof(Pages.HomePage),
            "Compress"     => typeof(Pages.CompressPage),
            "Extract"      => typeof(Pages.ExtractPage),
            "FileExplorer" => typeof(Pages.FilesPage),
            "Archive"      => typeof(Pages.ArchivePage),
            "Settings"     => typeof(Pages.SettingsPage),
            _              => null,
        };

        if (pageType != null)
            MainFrame.Navigate(pageType);
        else
            Logger.Warn($"未知导航标签：{tag}", "MainWindow");
    }

    private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        if (MainFrame.CanGoBack)
            MainFrame.GoBack();
    }
}
