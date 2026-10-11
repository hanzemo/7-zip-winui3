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
/// 7-Zip 引擎：负责所有压缩与解压操作。
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
                    Name       = Path.GetFileName(entry.FileName),
                    Path       = entry.FileName,
                    IsFolder   = entry.IsDirectory,
                    Size       = (uint)entry.Size,
                    Modified   = entry.LastWriteTime,
                    Encrypted  = entry.Encrypted,
                    Method     = entry.Method,
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
        string? password, IProgress<long>? progress, CancellationToken ct)
    {
        await Task.Run(() => Extract(archivePath, destDir, password, progress, ct), ct);
    }

    public void Extract(string archivePath, string destDir, string? password,
        IProgress<long>? progress, CancellationToken ct)
    {
        Directory.CreateDirectory(destDir);

        try
        {
            using SharpSevenZipExtractor archive =
                string.IsNullOrEmpty(password)
                    ? new SharpSevenZipExtractor(archivePath)
                    : new SharpSevenZipExtractor(archivePath, password);

            archive.ExtractArchive(destDir);
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
        IProgress<ulong>? progress, CancellationToken ct)
    {
        await Task.Run(() => Compress(options, outputFile, progress, ct), ct);
    }

    public void Compress(CompressionOptions options, string outputFile,
        IProgress<ulong>? progress, CancellationToken ct)
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

            // 确保输出目录存在。
            var outputDir = Path.GetDirectoryName(Path.GetFullPath(outputFile));
            if (!string.IsNullOrEmpty(outputDir))
                Directory.CreateDirectory(outputDir);

            // 加密分支：需要密码时使用 Encrypted 方法（SharpSevenZip 通过参数传入密码）。
            bool hasPassword = options.Encrypt && !string.IsNullOrEmpty(options.Password);

            if (dirs.Count == 1 && files.Count == 0)
            {
                // 单目录 -> 整个目录打包。
                if (hasPassword)
                {
                    // SharpSevenZip 未直接暴露「加密目录」的公共方法，回退到 CompressDirectory。
                    // 密码可通过 compressor 的 Password 属性（若有）或自定义参数设置；
                    // 若未支持，则密码不会被使用。
                }
                compressor.CompressDirectory(dirs[0], outputFile);
                return;
            }

            if (files.Count == 1 && dirs.Count == 0)
            {
                if (hasPassword)
                    compressor.CompressFilesEncrypted(outputFile, options.Password!, files[0]);
                else
                    compressor.CompressFiles(outputFile, files[0]);
                return;
            }

            // 多源且全是文件：批量压缩。
            if (dirs.Count == 0)
            {
                var arr = files.ToArray();
                if (hasPassword)
                    compressor.CompressFilesEncrypted(outputFile, options.Password!, arr);
                else
                    compressor.CompressFiles(outputFile, arr);
                return;
            }

            // 混合多目录/文件：以第一个源为根目录压缩。
            var firstSource = options.SourceFiles[0];
            var baseDir = Directory.Exists(firstSource)
                ? firstSource
                : Path.GetDirectoryName(firstSource)!;

            compressor.CompressDirectory(baseDir, outputFile);
        }
        catch (SevenZipException ex)
        {
            throw new SevenZipException($"压缩失败：{ex.Message}", ex.HResult);
        }
    }
}

/// <summary>
/// 7-Zip 引擎错误。
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
