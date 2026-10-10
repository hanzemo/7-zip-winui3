// SPDX-License-Identifier: GPL-2.0-or-later
//
// SettingsPage.xaml.cs

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SevenZipApp.Pages
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            Logger.Info("SettingsPage 初始化完成", "SettingsPage");
        }

        private void Save_Settings_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("保存设置", "SettingsPage");
            // 保存设置逻辑
        }
    }
}
