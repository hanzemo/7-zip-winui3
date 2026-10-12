// SPDX-License-Identifier: GPL-2.0-or-later
//
// DriftZipEngine.cs —— 基于 SharpSevenZip 库的核心服务。
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SharpSevenZip;

namespace DriftZipApp;

/// <summary>
/// DriftZip 引擎：负责所有压缩与解压操作。
/// </summary>
public sealed class DriftZipEngine
{
    // ─────────────────────────────────────────────────────
    //  枚举压缩包内容
    // ─────────────────────────────────────────────────────
    public IReadOnlyList<ArchiveItem> ListArchive(string archivePath, string? password = null)
    {
        var entries = new List<ArchiveItem>();

        try
        {
            using SharpSevenZipExtractor archive =
                string.IsNullOrEmpty(password)
                    ? new SharpSevenZipExtractor(archivePath)
                    : new SharpSevenZipExtractor(archivePath, password);

            foreach (var entry in archive.ArchiveFileData)
            {
                entries.Add(new ArchiveItem
                {
                    Name      = Path.GetFileName(entry.FileName),
                    Path      = entry.FileName,
                    IsFolder  = entry.IsDirectory,
                    Size      = (uint)entry.Size,
                    Modified  = entry.LastWriteTime,
                    Encrypted = entry.Encrypted,
                    Method    = entry.Method,
                });
            }
        }
        catch (SevenZipException ex)
        {
            throw new SevenZipException($"读取压缩文件失败：{ex.Message}", ex.HResult);
        }

        return entries;
    }

    // ─────────────────────────────────────────────────────
    //  解压
    // ─────────────────────────────────────────────────────
    public async Task ExtractAsync(string archivePath, string destDir,
        string? password, IProgress<int>? progress, CancellationToken ct)
    {
        await Task.Run(() => Extract(archivePath, destDir, password, progress, ct), ct);
    }

    public void Extract(string archivePath, string destDir, string? password,
        IProgress<int>? progress, CancellationToken ct)
    {
        Directory.CreateDirectory(destDir);

        try
        {
            using SharpSevenZipExtractor archive =
                string.IsNullOrEmpty(password)
                    ? new SharpSevenZipExtractor(archivePath)
                    : new SharpSevenZipExtractor(archivePath, password);

            // 进度回调：SharpSevenZip 在解压每个条目时触发 Extracting 事件。
            // e.PercentDone 为整体百分比（0-100），若库不提供则用条目计数兜底。
            int total = archive.ArchiveFileData.Count;
            int done  = 0;

            archive.Extracting += (sender, args) =>
            {
                ct.ThrowIfCancellationRequested();
                done++;
                int pct = total > 0 ? (int)(done * 100L / total) : 0;
                progress?.Report(pct);
            };

            archive.ExtractArchive(destDir);
            progress?.Report(100);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SevenZipException ex)
        {
            throw new SevenZipException($"解压失败：{ex.Message}", ex.HResult);
        }
    }

    // ─────────────────────────────────────────────────────
    //  压缩
    // ─────────────────────────────────────────────────────
    public async Task CompressAsync(CompressionOptions options, string outputFile,
        IProgress<int>? progress, CancellationToken ct)
    {
        await Task.Run(() => Compress(options, outputFile, progress, ct), ct);
    }

    public void Compress(CompressionOptions options, string outputFile,
        IProgress<int>? progress, CancellationToken ct)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        if (options.SourceFiles == null || options.SourceFiles.Count == 0)
            throw new SevenZipException("没有要压缩的源文件。");

        if (string.IsNullOrEmpty(outputFile))
            throw new ArgumentException("输出文件路径不能为空", nameof(outputFile));

        var dirs  = options.SourceFiles.Where(Directory.Exists).ToList();
        var files = options.SourceFiles.Where(File.Exists).ToList();

        if (dirs.Count + files.Count == 0)
            throw new SevenZipException("所选源路径均不存在。");

        try
        {
            var compressor = new SharpSevenZipCompressor
            {
                ArchiveFormat    = options.Format.GetArchiveFormat(),
                CompressionLevel = options.Level.GetSharpLevel(),
            };

            // 进度回调：SharpSevenZip 在每个条目开始时触发 Compressing 事件。
            int total = options.SourceFiles.Count;
            int done  = 0;

            compressor.Compressing += (sender, args) =>
            {
                ct.ThrowIfCancellationRequested();
                done++;
                int pct = total > 0 ? (int)(done * 100L / total) : 0;
                progress?.Report(pct);
            };

            // 确保输出目录存在。
            var outputDir = Path.GetDirectoryName(Path.GetFullPath(outputFile));
            if (!string.IsNullOrEmpty(outputDir))
                Directory.CreateDirectory(outputDir);

            bool hasPassword = options.Encrypt && !string.IsNullOrEmpty(options.Password);

            if (dirs.Count == 1 && files.Count == 0)
            {
                compressor.CompressDirectory(dirs[0], outputFile);
                progress?.Report(100);
                return;
            }

            if (files.Count == 1 && dirs.Count == 0)
            {
                if (hasPassword)
                    compressor.CompressFilesEncrypted(outputFile, options.Password!, files[0]);
                else
                    compressor.CompressFiles(outputFile, files[0]);
                progress?.Report(100);
                return;
            }

            if (dirs.Count == 0)
            {
                var arr = files.ToArray();
                if (hasPassword)
                    compressor.CompressFilesEncrypted(outputFile, options.Password!, arr);
                else
                    compressor.CompressFiles(outputFile, arr);
                progress?.Report(100);
                return;
            }

            var firstSource = options.SourceFiles[0];
            var baseDir = Directory.Exists(firstSource)
                ? firstSource
                : Path.GetDirectoryName(firstSource)!;

            compressor.CompressDirectory(baseDir, outputFile);
            progress?.Report(100);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SevenZipException ex)
        {
            throw new SevenZipException($"压缩失败：{ex.Message}", ex.HResult);
        }
    }
}

/// <summary>
/// DriftZip 引擎错误。
/// </summary>
public sealed class SevenZipException : Exception
{
    public int HResult7z { get; }

    public SevenZipException(string message) : base(message) { }
    public SevenZipException(string message, int hr) : base(message) { HResult7z = hr; }
    public SevenZipException(string message, Exception inner) : base(message, inner) { }
}

// ─────────────────────────────────────────────────────
//  辅助扩展方法
// ─────────────────────────────────────────────────────
internal static class OutputFormatExtensions
{
    public static OutArchiveFormat GetArchiveFormat(this OutputFormat format) => format switch
    {
        OutputFormat.SevenZip => OutArchiveFormat.SevenZip,
        OutputFormat.Zip      => OutArchiveFormat.Zip,
        OutputFormat.Tar      => OutArchiveFormat.Tar,
        OutputFormat.GZip     => OutArchiveFormat.GZip,
        OutputFormat.BZip2    => OutArchiveFormat.BZip2,
        OutputFormat.XZ       => OutArchiveFormat.XZ,
        _                     => OutArchiveFormat.SevenZip,
    };
}

internal static class CompressionLevelExtensions
{
    public static SharpSevenZip.CompressionLevel GetSharpLevel(this CompressionLevel level) => level switch
    {
        CompressionLevel.Store   => SharpSevenZip.CompressionLevel.None,
        CompressionLevel.Fastest => SharpSevenZip.CompressionLevel.Fast,
        CompressionLevel.Fast    => SharpSevenZip.CompressionLevel.Low,
        CompressionLevel.Normal  => SharpSevenZip.CompressionLevel.Normal,
        CompressionLevel.Maximum => SharpSevenZip.CompressionLevel.High,
        CompressionLevel.Ultra   => SharpSevenZip.CompressionLevel.Ultra,
        _                        => SharpSevenZip.CompressionLevel.Normal,
    };
}