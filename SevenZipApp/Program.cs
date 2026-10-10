// SPDX-License-Identifier: GPL-2.0-or-later
//
// Program.cs —— 应用程序入口点。
//
// 参考：Microsoft Learn - "How to create a single-instanced WinUI app with C#"
// 手动定义 Main 可让 App 使用 DispatcherQueueSynchronizationContext，
// 从而在 async 事件处理器中正确绑定到 UI 线程。

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Threading;

namespace SevenZipApp;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        Application.Start((p) =>
        {
            var context = new DispatcherQueueSynchronizationContext(
                DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);

            _ = new App();
        });
    }
}
