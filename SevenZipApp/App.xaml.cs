// SPDX-License-Identifier: GPL-2.0-or-later
//
// App.xaml.cs

using Microsoft.UI.Xaml;
using System;

namespace SevenZipApp;

public partial class App : Application
{
    public static Window? MainWindow { get; private set; }

    public App()
    {
        this.InitializeComponent();

        // UI 线程未处理异常
        this.UnhandledException += (s, e) =>
        {
            Program.WriteCrashLog("App.UnhandledException", e.Exception);
            // 保持 false：让系统记录完整崩溃信息；改为 true 则吞掉异常（可能导致进程继续运行但状态异常）
            e.Handled = false;
        };

        // 非 UI 线程未处理异常
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                Program.WriteCrashLog("AppDomain.UnhandledException", ex);
            else
                Program.WriteCrashLog("AppDomain.UnhandledException", new Exception(e.ExceptionObject?.ToString() ?? "unknown"));
        };

        // Task 内未观察的异常（可选，便于排查）
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            Program.WriteCrashLog("TaskScheduler.UnobservedTaskException", e.Exception);
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            MainWindow = new Views.MainWindow();
            MainWindow.Activate();
        }
        catch (Exception ex)
        {
            Program.WriteCrashLog("App.OnLaunched", ex);
            throw;
        }
    }
}
