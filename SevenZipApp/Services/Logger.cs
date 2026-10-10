// SPDX-License-Identifier: GPL-2.0-or-later
//
// Logger.cs —— 简易日志服务。

using System;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.UI.Xaml;

namespace SevenZipApp
{
    public class Logger
    {
        private static readonly object _lock = new object();
        private static string _logPath;
        private static int _logMaxLines = 1000;

        private Logger() { }

        public static void Initialize(string logPath = null)
        {
            if (logPath == null)
            {
                // 尝试多个可能的路径
                string[] possiblePaths = {
                    Path.Combine(AppContext.BaseDirectory, "logs"),
                    Path.Combine(AppContext.BaseDirectory, "..", "logs"), // one level up
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Directories))
                };

                foreach (string path in possiblePaths)
                {
                    try
                    {
                        if (path != null && Directory.Exists(path))
                        {
                            Directory.CreateDirectory(Path.Combine(path, "logs"));
                            _logPath = Path.Combine(path, "logs", "SevenZipLog.txt");
                            break;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(_logPath))
                {
                    // 默认使用当前目录的logs
                    _logPath = Path.Combine(AppContext.BaseDirectory, "logs", "SevenZipLog.txt");
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
                    }
                    catch (Exception ex)
                    {
                        // 如果无法创建目录，使用用户文档目录
                        string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        _logPath = Path.Combine(docPath, "SevenZipLog.txt");
                    }
                }
            }
            else
            {
                _logPath = logPath;
            }
            
            // 确保目录存在
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
            }
            catch
            {
                // 忽略创建目录失败
            }
        }

        public static void Log(string message, LogLevel level = LogLevel.Info, string? source = null)
        {
            if (string.IsNullOrEmpty(_logPath) || !File.Exists(_logPath) && !Directory.Exists(Path.GetDirectoryName(_logPath))) return;
            
            string formattedMessage = source == null ? message : $"[{source}] {message}";
            lock (_lock)
            {
                try
                {
                    string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {formattedMessage}";
                    // 清理 CR LF
                    logEntry = logEntry.Replace("\r\n", " ").Replace("\n", " ").Trim();
                    File.AppendAllText(_logPath, logEntry + Environment.NewLine, Encoding.UTF8);
                    
                    // 同步到控制台
                    Console.WriteLine(logEntry);
                }
                catch (Exception ex)
                {
                    // 忽略日志写入失败
                }
            }
        }

        public static void Debug(string message, string? source = null) => Log(message, LogLevel.Debug, source);
        public static void Info(string message, string? source = null) => Log(message, LogLevel.Info, source);
        public static void Warning(string message, string? source = null) => Log(message, LogLevel.Warn, source);
        public static void Error(string message, string? source = null) => Log(message, LogLevel.Error, source);

#pragma warning disable CS8618 // 非空字段未初始化
        private static async Task<bool> EnsureMaxLinesAsync()
#pragma warning restore CS8618 // 非空字段未初始化
        {
            if (!File.Exists(_logPath)) return false;
            
            var lines = await Task.Run(() => File.ReadAllLines(_logPath));
            if (lines.Length > _logMaxLines)
            {
                // 保留前100行（关键日志）
                var prefixLines = lines.Take(100).ToArray();
                using (var writer = File.CreateText(_logPath + ".tmp"))
                {
                    foreach (var line in prefixLines)
                    {
                        writer.WriteLine(line);
                    }
                    writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] LOG ARCHIVED");
                }
                File.Delete(_logPath);
                File.Move(_logPath + ".tmp", _logPath);
            }
            return true;
        }
    }

    public enum LogLevel
    {
        Info,
        Warning,
        Warn = Info,  // 别名
        Error,
        Debug
    }
}
