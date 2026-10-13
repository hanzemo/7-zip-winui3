// SPDX-License-Identifier: GPL-2.0-or-later
//
// AppSettings.cs —— 应用设置模型 + JSON 持久化。

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DriftZipApp.Services;

public sealed class AppSettings
{
    // ── 7-Zip Tab ──
    public bool ShellIntegration { get; set; } = true;
    public bool CascadedMenu { get; set; } = true;
    public bool ShowIconsInMenu { get; set; } = true;
    public bool EliminateRootFolder { get; set; } = false;
    public bool ContextMenuExtract { get; set; } = true;
    public bool ContextMenuCompress { get; set; } = true;

    // ── Folders Tab ──
    public string WorkingFolder { get; set; } = "";
    public bool UseForRemovableOnly { get; set; } = false;

    // ── Editor Tab ──
    public string ViewerPath { get; set; } = "";
    public string EditorPath { get; set; } = "";
    public string DiffPath   { get; set; } = "";

    // ── Settings Tab ──
    public bool ShowDotDot { get; set; } = true;
    public bool ShowRealIcons { get; set; } = false;
    public bool FullRowSelect { get; set; } = true;
    public bool ShowGridLines { get; set; } = false;
    public bool SingleClickOpen { get; set; } = false;
    public bool AlternateSelection { get; set; } = false;
    public bool UseSystemMenu { get; set; } = true;
    public bool LargeMemoryPages { get; set; } = false;

    // ── 主题 ──
    /// <summary>0=浅色, 1=深色, 2=系统默认</summary>
    public int Theme { get; set; } = 2;

    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DriftZipApp");

    private static readonly string SettingsPath =
        Path.Combine(SettingsDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var s = JsonSerializer.Deserialize<AppSettings>(json, JsonOpts);
                if (s != null) return s;
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"读取 settings.json 失败：{ex.Message}", "AppSettings");
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(this, JsonOpts);
            File.WriteAllText(SettingsPath, json);
            Logger.Info("设置已保存", "AppSettings");
        }
        catch (Exception ex)
        {
            Logger.Error($"保存 settings.json 失败：{ex.Message}", "AppSettings");
        }
    }

    public static AppSettings Current { get; private set; } = Load();

    public static void Reload() => Current = Load();
}