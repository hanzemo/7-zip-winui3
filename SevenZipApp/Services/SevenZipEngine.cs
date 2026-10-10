// SPDX-License-Identifier: GPL-2.0-or-later
//
// SevenZipEngine.cs —— 基于 SharpSevenZip 库的核心服务。
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SharpSevenZip;

namespace SevenZipApp;

public sealed class SevenZipEngine
{
    public IReadOnlyList<ArchiveItem> ListArchive(string archivePath, string? password = null)
    {
        var entries = new List<ArchiveItem>();

        try
        {
            using var archive = string.IsNullOrEmpty(password)
                ? new SevenZipExtractor(archivePath)
                : new SevenZipExtractor(archivePath, password);

            foreach (var entry in archive.ArchiveFileData)
            {
                entries.Add(new ArchiveItem
                {
                    Name       = entry.FileName,
                    Path       = entry.FileName,
                    IsFolder   = entry.IsDirectory,
                    Size       = (long)entry.Size,
                    PackedSize = (long)entry.CompressedSize,
                    Modified   = entry.LastWriteTime,
                    Index      = entry.Index,
                });
            }
        }
        catch (SharpSevenZipException ex)
        {
            throw new SevenZipException($"读取压缩文件失败：{ex.Message}", ex.HResult);
        }

        return entries;
    }

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
            using var archive = string.IsNullOrEmpty(password)
                ? new SevenZipExtractor(archivePath)
                : new SevenZipExtractor(archivePath, password);

            archive.ExtractArchive(destDir);
        }
        catch (SharpSevenZipException ex)
        {
            throw new SevenZipException($"解压失败：{ex.Message}", ex.HResult);
        }
    }

    public async Task CompressAsync(CompressionOptions options, string outputFile,
        IProgress<ulong>? progress, CancellationToken ct)
    {
        await Task.Run(() => Compress(options, outputFile, progress, ct), ct);
    }

    public void Compress(CompressionOptions options, string outputFile,
        IProgress<ulong>? progress, CancellationToken ct)
    {
        if (options.SourceFiles.Count == 0)
            throw new SevenZipException("没有要压缩的源文件。");

        if (string.IsNullOrEmpty(outputFile))
            throw new ArgumentException("输出文件路径不能为空", nameof(outputFile));

        try
        {
            var compressor = new SharpSevenZipCompressor
            {
                ArchiveFormat    = options.Format.GetArchiveFormat(),
                CompressionLevel = options.Level.GetSharpLevel(),
            };

            if (options.Format == OutputFormat.SevenZip)
                compressor.CompressionMethod = CompressionMethod.Lzma2;

            if (!string.IsNullOrEmpty(options.Password))
                compressor.Password = options.Password;

            var dirs  = options.SourceFiles.Where(Directory.Exists).ToList();
            var files = options.SourceFiles.Where(File.Exists).ToList();

            if (dirs.Count == 1 && files.Count == 0)
            {
                compressor.CompressDirectory(dirs[0], outputFile, recursive: true);
                return;
            }

            if (files.Count == 1 && dirs.Count == 0)
            {
                compressor.CompressFiles(outputFile, files[0]);
                return;
            }

            var firstSource = options.SourceFiles[0];
            var baseDir = Directory.Exists(firstSource)
                ? firstSource
                                : Path.GetDirectoryName(firstSource)!;

            compressor.CompressDirectory(baseDir, outputFile, recursive: true);
        }
        catch (SharpSevenZipException ex)
        {
            throw new SevenZipException($"压缩失败：{ex.Message}", ex.HResult);
        }
    }
}

public sealed class SevenZipException : Exception
{
    public int HResult7z { get; }

    public SevenZipException(string message) : base(message) { }
    public SevenZipException(string message, int hr) : base(message) { HResult7z = hr; }
    public SevenZipException(string message, Exception inner) : base(message, inner) { }
}

internal static class OutputFormatExtensions
{
    public static OutArchiveFormat GetArchiveFormat(this OutputFormat format)
    {
        return format switch
        {
            OutputFormat.SevenZip => OutArchiveFormat.SevenZip,
            OutputFormat.Zip      => OutArchiveFormat.Zip,
            OutputFormat.Tar      => OutArchiveFormat.Tar,
            OutputFormat.GZip     => OutArchiveFormat.GZip,
            OutputFormat.BZip2    => OutArchiveFormat.BZip2,
            OutputFormat.XZ       => OutArchiveFormat.XZ,
            _ => OutArchiveFormat.SevenZip,
        };
    }
}

internal static class CompressionLevelExtensions
{
    public static SharpSevenZip.CompressionLevel GetSharpLevel(this CompressionLevel level)
    {
        return level switch
        {
            CompressionLevel.Fastest => SharpSevenZip.CompressionLevel.Fastest,
            CompressionLevel.Fast    => SharpSevenZip.CompressionLevel.Fast,
            CompressionLevel.Normal  => SharpSevenZip.CompressionLevel.Normal,
            CompressionLevel.High    => SharpSevenZip.CompressionLevel.High,
            CompressionLevel.Maximum => SharpSevenZip.CompressionLevel.Maximum,
            CompressionLevel.Ultra   => SharpSevenZip.CompressionLevel.Ultra,
            _ => SharpSevenZip.CompressionLevel.Normal,
        };
    }
}
