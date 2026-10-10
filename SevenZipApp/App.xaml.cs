// SPDX-License-Identifier: GPL-2.0-or-later
//
// App.xaml.cs —— 应用入口。

using Microsoft.UI.Xaml;

namespace SevenZipApp;

public partial class App : Application
{
    public static Window? MainWindow { get; private set; }

    public App()
    {
        Logger.Initialize();
        Logger.Info("App 构造完成", "App");

        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (MainWindow != null)
        {
            MainWindow.Activate();
            return;
        }

        MainWindow = new Views.MainWindow();
        MainWindow.Activate();

        Logger.Info("MainWindow 已激活", "App");
    }
}
