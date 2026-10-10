// SPDX-License-Identifier: GPL-2.0-or-later
//
// Program.cs —— 应用程序入口点。

using Microsoft.UI.Xaml;

namespace SevenZipApp;

/// <summary>
/// 应用程序主类，负责启动和生命周期管理。
/// </summary>
public static class Program
{
    /// <summary>
    /// 主应用程序入口点。
    /// </summary>
    [STAThread]
    public static void Main()
    {
        // 初始化 WinUI 3
        var app = new App();
        app.Activate();
    }
}
