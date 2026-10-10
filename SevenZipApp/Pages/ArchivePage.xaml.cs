// SPDX-License-Identifier: GPL-2.0-or-later
//
// ArchivePage.xaml.cs

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SevenZipApp.Pages
{
    public sealed partial class ArchivePage : Page
    {
        public ArchivePage()
        {
            this.InitializeComponent();
            Logger.Info("ArchivePage 初始化完成", "ArchivePage");
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("清空历史记录", "ArchivePage");
            // 清空历史记录
        }
    }
}
