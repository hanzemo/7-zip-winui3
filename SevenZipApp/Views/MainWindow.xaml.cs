// SPDX-License-Identifier: GPL-2.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Linq;

namespace SevenZipApp.Views;

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
