// SPDX-License-Identifier: GPL-2.0-or-later
//
// Program.cs —— 应用程序入口点。

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Threading;
using WinRT;

namespace SevenZipApp;

public static class Program
{
    private static readonly string CrashLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SevenZipApp-crash.log");

    [STAThread]
    public static void Main()
    {
        try
        {
            ComWrappersSupport.InitializeComWrappers();

            Application.Start((p) =>
            {
                var context = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);

                _ = new App();
            });
        }
        catch (Exception ex)
        {
            WriteCrashLog("Program.Main", ex);
            throw;
        }
    }

    /// <summary>
    /// 把异常写到 %LOCALAPPDATA%\SevenZipApp-crash.log，用于定位启动崩溃。
    /// </summary>
    public static void WriteCrashLog(string source, Exception ex)
    {
        try
        {
            File.AppendAllText(CrashLogPath,
                $"===== {DateTime.Now:yyyy-MM-dd HH:mm:ss} [{source}] =====\n" +
                $"{ex}\n\n");
        }
        catch
        {
            // 写日志失败就吞掉，避免二次崩溃。
        }
    }
}
