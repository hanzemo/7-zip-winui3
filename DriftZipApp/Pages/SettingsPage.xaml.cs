// SPDX-License-Identifier: GPL-2.0-or-later
//
// SettingsPage.xaml.cs

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using DriftZipApp.Services;

namespace DriftZipApp.Pages
{
    public sealed partial class SettingsPage : Page
    {
        private readonly AppSettings _settings = AppSettings.Current;

        public SettingsPage()
        {
            this.InitializeComponent();
            Logger.Info("SettingsPage 初始化完成", "SettingsPage");

            Loaded += (s, e) =>
            {
                LoadSettingsToUi();
                LoadAssocStatus();
            };
        }

        // ─────────────────────────────────────────────────────
        //  加载设置到 UI
        // ─────────────────────────────────────────────────────
        private void LoadSettingsToUi()
        {
            // 7-Zip
            ChkShellIntegration.IsChecked = _settings.ShellIntegration;
            ChkCascadedMenu.IsChecked     = _settings.CascadedMenu;
            ChkShowIcons.IsChecked        = _settings.ShowIconsInMenu;
            ChkEliminateRoot.IsChecked    = _settings.EliminateRootFolder;
            ChkCtxExtract.IsChecked       = _settings.ContextMenuExtract;
            ChkCtxCompress.IsChecked      = _settings.ContextMenuCompress;

            // Folders
            TxtWorkingFolder.Text    = _settings.WorkingFolder;
            ChkRemovableOnly.IsChecked = _settings.UseForRemovableOnly;

            // Editor
            TxtViewer.Text = _settings.ViewerPath;
            TxtEditor.Text = _settings.EditorPath;
            TxtDiff.Text   = _settings.DiffPath;

            // Settings
            ChkShowDotDot.IsChecked    = _settings.ShowDotDot;
            ChkShowRealIcons.IsChecked = _settings.ShowRealIcons;
            ChkFullRowSelect.IsChecked = _settings.FullRowSelect;
            ChkShowGridLines.IsChecked = _settings.ShowGridLines;
            ChkSingleClick.IsChecked   = _settings.SingleClickOpen;
            ChkAltSelection.IsChecked  = _settings.AlternateSelection;
            ChkSystemMenu.IsChecked    = _settings.UseSystemMenu;
            ChkLargeMemory.IsChecked   = _settings.LargeMemoryPages;

            // 主题
            switch (_settings.Theme)
            {
                case 0: RbLight.IsChecked = true; break;
                case 1: RbDark.IsChecked  = true; break;
                default: RbSystem.IsChecked = true; break;
            }
        }

        // ─────────────────────────────────────────────────────
        //  读取关联状态
        // ─────────────────────────────────────────────────────
        private void LoadAssocStatus()
        {
            var exts = new[]
            {
                // 读写
                ".7z", ".xz", ".bz2", ".gz", ".tar", ".zip", ".wim",
                // 只读
                ".apfs", ".ar", ".arj", ".cab", ".chm", ".cpio", ".cramfs",
                ".dmg", ".ext", ".fat", ".gpt", ".hfs", ".ihex", ".iso",
                ".lzh", ".lzma", ".mbr", ".msi", ".nsis", ".ntfs", ".qcow2",
                ".rar", ".rpm", ".squashfs", ".udf", ".uefi", ".vdi", ".vhd",
                ".vhdx", ".vmdk", ".xar", ".z", ".tgz",
            };

            var rows = new List<AssocRow>();
            foreach (var ext in exts)
            {
                string defaultApp = "（未关联）";
                try
                {
                    using var key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{ext}");
                    var val = key?.GetValue("")?.ToString();
                    if (!string.IsNullOrEmpty(val))
                        defaultApp = val;
                    else
                        defaultApp = "（未设置）";
                }
                catch (Exception ex)
                {
                    defaultApp = $"（无法读取：{ex.GetType().Name}）";
                }

                rows.Add(new AssocRow { Ext = ext, DefaultApp = defaultApp });
            }

            AssocList.ItemsSource = rows;
        }

        // ─────────────────────────────────────────────────────
        //  保存
        // ─────────────────────────────────────────────────────
        private void Save_Settings_Click(object sender, RoutedEventArgs e)
        {
            // 7-Zip
            _settings.ShellIntegration     = ChkShellIntegration.IsChecked == true;
            _settings.CascadedMenu         = ChkCascadedMenu.IsChecked == true;
            _settings.ShowIconsInMenu      = ChkShowIcons.IsChecked == true;
            _settings.EliminateRootFolder  = ChkEliminateRoot.IsChecked == true;
            _settings.ContextMenuExtract   = ChkCtxExtract.IsChecked == true;
            _settings.ContextMenuCompress  = ChkCtxCompress.IsChecked == true;

            // Folders
            _settings.WorkingFolder       = TxtWorkingFolder.Text?.Trim() ?? "";
            _settings.UseForRemovableOnly = ChkRemovableOnly.IsChecked == true;

            // Editor
            _settings.ViewerPath = TxtViewer.Text?.Trim() ?? "";
            _settings.EditorPath = TxtEditor.Text?.Trim() ?? "";
            _settings.DiffPath   = TxtDiff.Text?.Trim() ?? "";

            // Settings
            _settings.ShowDotDot         = ChkShowDotDot.IsChecked == true;
            _settings.ShowRealIcons      = ChkShowRealIcons.IsChecked == true;
            _settings.FullRowSelect      = ChkFullRowSelect.IsChecked == true;
            _settings.ShowGridLines      = ChkShowGridLines.IsChecked == true;
            _settings.SingleClickOpen    = ChkSingleClick.IsChecked == true;
            _settings.AlternateSelection = ChkAltSelection.IsChecked == true;
            _settings.UseSystemMenu      = ChkSystemMenu.IsChecked == true;
            _settings.LargeMemoryPages   = ChkLargeMemory.IsChecked == true;

            // 主题
            if (RbLight.IsChecked == true) _settings.Theme = 0;
            else if (RbDark.IsChecked == true) _settings.Theme = 1;
            else _settings.Theme = 2;

            _settings.Save();
            ApplyTheme();

            Logger.Info("保存设置", "SettingsPage");
        }

        // ─────────────────────────────────────────────────────
        //  主题立即生效
        // ─────────────────────────────────────────────────────
        private void ApplyTheme()
        {
            if (App.MainWindow?.Content is FrameworkElement root)
            {
                root.RequestedTheme = _settings.Theme switch
                {
                    0 => ElementTheme.Light,
                    1 => ElementTheme.Dark,
                    _ => ElementTheme.Default,
                };
            }
        }

        // ─────────────────────────────────────────────────────
        //  浏览按钮
        // ─────────────────────────────────────────────────────
        private async void BrowseWorkingFolder_Click(object sender, RoutedEventArgs e)
        {
            var folder = await PickFolderAsync();
            if (folder != null) TxtWorkingFolder.Text = folder;
        }

        private async void BrowseViewer_Click(object sender, RoutedEventArgs e)
        {
            var file = await PickExeAsync();
            if (file != null) TxtViewer.Text = file;
        }

        private async void BrowseEditor_Click(object sender, RoutedEventArgs e)
        {
            var file = await PickExeAsync();
            if (file != null) TxtEditor.Text = file;
        }

        private async void BrowseDiff_Click(object sender, RoutedEventArgs e)
        {
            var file = await PickExeAsync();
            if (file != null) TxtDiff.Text = file;
        }

        private async Task<string?> PickFolderAsync()
        {
            var picker = new FolderPicker();
            picker.FileTypeFilter.Add("*");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            var folder = await picker.PickSingleFolderAsync();
            return folder?.Path;
        }

        private async Task<string?> PickExeAsync()
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".exe");
            picker.FileTypeFilter.Add("*");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            var file = await picker.PickSingleFileAsync();
            return file?.Path;
        }
    }

    /// <summary>System Tab 的行模型。</summary>
    public sealed class AssocRow
    {
        public string Ext { get; set; } = "";
        public string DefaultApp { get; set; } = "";
    }
}