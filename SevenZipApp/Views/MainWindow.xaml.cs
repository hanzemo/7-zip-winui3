// SPDX-License-Identifier: GPL-2.0-or-later
//
// MainWindow.xaml.cs —— 主窗口，实现自定义标题栏、导航等。

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Drawing;

namespace SevenZipApp
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            
            this.ExtendsContentIntoTitleBar = true;
            this.TitleBar.IsExtensionPresent = true;
            this.TitleBar.ExtendsContentIntoTitleBar = true;
            
            this.SetTitleBar(this.FindChild<Grid>("TitleBarGrid"));
            
            Logger.Info("MainWindow 初始化完成", "MainWindow");
        }

        private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            Logger.Warn("BackRequested received", "MainWindow");
            if (MainFrame?.Content is FrameworkElement element && element.DataContext is Pages.HomePage homePage)
            {
                MainFrame.GoBack();
            }
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            NavigationViewItem selectedItem = (NavigationViewItem)args.InvokedItemContainer;
            string? tag = selectedItem.Tag?.ToString();
            
            Logger.Info($"导航到页面: {tag}", "MainWindow");

            MainFrame.ContentTransitions = null;

            switch (tag)
            {
                case "Home":
                    MainFrame.Navigate(typeof(Pages.HomePage));
                    break;
                case "Compress":
                    MainFrame.Navigate(typeof(Pages.CompressPage));
                    break;
                case "Extract":
                    MainFrame.Navigate(typeof(Pages.ExtractPage));
                    break;
                case "FileExplorer":
                    MainFrame.Navigate(typeof(Pages.FilesPage));
                    break;
                case "Archive":
                    MainFrame.Navigate(typeof(Pages.ArchivePage));
                    break;
                case "Settings":
                    MainFrame.Navigate(typeof(Pages.SettingsPage));
                    break;
                default:
                    Logger.Warn($"未知导航标签: {tag}", "MainWindow");
                    break;
            }
        }

        private T? FindChild<T>(DependencyObject parent, string name) where T : DependencyObject
        {
            if (parent == null) return null;

            int childCount = VisualTreeHelper.GetChildCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild && typedChild.Name == name)
                {
                    return typedChild;
                }
                T? childResult = FindChild<T>(child, name);
                if (childResult != null) return childResult;
            }
            return null;
        }
    }
}
                T? childResult = FindChild<T>(child, name);
                if (childResult != null) return childResult;
            }
            return null;
        }
    }
}
