// SPDX-License-Identifier: GPL-2.0-or-later
//
// Program.cs —— 应用程序入口点。

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Threading;
using WinRT;

namespace SevenZipApp;

public static class Program
{
    [STAThread]
    public static void Main()
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
}
