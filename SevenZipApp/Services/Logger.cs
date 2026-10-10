// SPDX-License-Identifier: GPL-2.0-or-later
//
// Logger.cs —— 简易日志服务。

using System;
using System.IO;
using System.Text;
using System.Threading;

namespace SevenZipApp;

public static class Logger
{
    private static string? _logPath;
    private static readonly object _lock = new();

    public static void Initialize(string? logPath = null)
    {
        if (logPath != null)
        {
            _logPath = logPath;
        }
        else
        {
            _logPath = SafeCreate(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                 "SevenZipApp", "logs", "SevenZipLog.txt"))
                ?? SafeCreate(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                 "SevenZipApp", "logs", "SevenZipLog.txt"))
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                "SevenZipApp", "logs", "SevenZipLog.txt");
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
        }
        catch
        {
            // 忽略。
        }
    }

    private static string? SafeCreate(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            return path;
        }
        catch
        {
            return null;
        }
    }

    public static void Log(string message, LogLevel level = LogLevel.Info, string? source = null)
    {
        if (string.IsNullOrEmpty(_logPath)) return;

        string formatted = string.IsNullOrEmpty(source) ? message : $"[{source}] {message}";

        lock (_lock)
        {
            try
            {
                string entry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {formatted}";
                entry = entry.Replace("\r\n", " ").Replace("\n", " ").Trim();
                File.AppendAllText(_logPath, entry + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // 忽略日志写入失败。
            }
        }
    }

    public static void Debug(string message, string? source = null) => Log(message, LogLevel.Debug, source);
    public static void Info(string message, string? source = null) => Log(message, LogLevel.Info, source);
    public static void Warning(string message, string? source = null) => Log(message, LogLevel.Warning, source);
    public static void Warn(string message, string? source = null) => Log(message, LogLevel.Warning, source);
    public static void Error(string message, string? source = null) => Log(message, LogLevel.Error, source);
}

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
}
