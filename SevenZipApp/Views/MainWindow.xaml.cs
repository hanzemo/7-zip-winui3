// SPDX-License-Identifier: GPL-2.0-or-later
//
// MainWindow.xaml.cs —— 主窗口，实现自定义标题栏、导航等。

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace SevenZipApp.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();

        // 自定义标题栏：必须在 code-behind 中设置（WinUI 3 不支持在 XAML 中设置）。
        this.ExtendsContentIntoTitleBar = true;

        // 让自定义标题栏区域可以拖动窗口。
        this.SetTitleBar(this.TitleBarGrid);

        // 初始页面。
        this.MainFrame.Navigate(typeof(Pages.HomePage));

        Logger.Info("MainWindow 初始化完成", "MainWindow");
    }

    private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        Logger.Warn("BackRequested received", "MainWindow");
        if (MainFrame.CanGoBack)
        {
            MainFrame.GoBack();
        }
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is not NavigationViewItem selectedItem)
            return;

        string? tag = selectedItem.Tag?.ToString();

        Logger.Info($"导航到页面: {tag}", "MainWindow");

        MainFrame.ContentTransitions = null;

        Type? pageType = tag switch
        {
            "Home"          => typeof(Pages.HomePage),
            "Compress"      => typeof(Pages.CompressPage),
            "Extract"       => typeof(Pages.ExtractPage),
            "FileExplorer"  => typeof(Pages.FilesPage),
            "Archive"       => typeof(Pages.ArchivePage),
            "Settings"      => typeof(Pages.SettingsPage),
            _               => null,
        };

        if (pageType != null)
        {
            MainFrame.Navigate(pageType);
        }
        else
        {
            Logger.Warn($"未知导航标签: {tag}", "MainWindow");
        }
    }
}
