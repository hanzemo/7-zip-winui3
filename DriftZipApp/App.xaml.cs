// SPDX-License-Identifier: GPL-2.0-or-later

using Microsoft.UI.Xaml;
using System;
using System.IO;
using Windows.ApplicationModel.Activation;

namespace DriftZipApp;

public partial class App : Application
{
    public static Window? MainWindow { get; private set; }

    public App()
    {
        Logger.Initialize();
        this.InitializeComponent();

        this.UnhandledException += (s, e) =>
        {
            Program.WriteCrashLog("App.UnhandledException", e.Exception);
            e.Handled = false;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                Program.WriteCrashLog("AppDomain.UnhandledException", ex);
        };
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            MainWindow = new Views.MainWindow();
            MainWindow.Activate();

            // ── 应用保存的主题 ──
            try
            {
                var settings = DriftZipApp.Services.AppSettings.Current;
                if (MainWindow.Content is FrameworkElement root)
                {
                    root.RequestedTheme = settings.Theme switch
                    {
                        0 => ElementTheme.Light,
                        1 => ElementTheme.Dark,
                        _ => ElementTheme.Default,
                    };
                }
            }
            catch { }

            HandleActivation();
            HandleCommandLine();
        }
        catch (Exception ex)
        {
            Program.WriteCrashLog("App.OnLaunched", ex);
            throw;
        }
    }

    private static void HandleActivation()
    {
        try
        {
            var activated = Microsoft.Windows.AppLifecycle.AppInstance
                .GetCurrent().GetActivatedEventArgs();

            if (activated.Kind == Microsoft.Windows.AppLifecycle.ExtendedActivationKind.File)
            {
                var fileArgs = activated.Data as IFileActivatedEventArgs;
                if (fileArgs?.Files.Count > 0)
                {
                    var path = (fileArgs.Files[0] as Windows.Storage.StorageFile)?.Path;
                    if (!string.IsNullOrEmpty(path))
                        (MainWindow as Views.MainWindow)?.OpenArchive(path);
                }
            }
        }
        catch (Exception ex)
        {
            Program.WriteCrashLog("App.HandleActivation", ex);
        }
    }

    private static void HandleCommandLine()
    {
        try
        {
            var args = Environment.GetCommandLineArgs();
            Program.WriteCrashLog("App.HandleCommandLine.Called", new Exception(string.Join(" | ", args)));

            if (args.Length < 3) return;

            var verb = args[1];
            var target = args[2];

            if (string.IsNullOrEmpty(verb)) return;
            if (!File.Exists(target) && !Directory.Exists(target)) return;

            (MainWindow as Views.MainWindow)?.HandleShellCommand(verb, target);
        }
        catch (Exception ex)
        {
            Program.WriteCrashLog("App.HandleCommandLine", ex);
        }
    }
}
