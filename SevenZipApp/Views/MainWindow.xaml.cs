// SPDX-License-Identifier: GPL-2.0-or-later
//
// MainWindow.xaml.cs —— 主窗口，实现自定义标题栏、导航等。

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using SevenZipApp.Pages;
using System;

namespace SevenZipApp.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();

        this.ExtendsContentIntoTitleBar = true;
        this.SetTitleBar(this.TitleBarGrid);

        this.MainFrame.Navigate(typeof(HomePage));

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

        MainFrame.ContentTransitions = new TransitionCollection();

        Type? pageType = tag switch
        {
            "Home"         => typeof(HomePage),
            "Compress"     => typeof(CompressPage),
            "Extract"      => typeof(ExtractPage),
            "FileExplorer" => typeof(FilesPage),
            "Archive"      => typeof(ArchivePage),
            "Settings"     => typeof(SettingsPage),
            _              => null,
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
