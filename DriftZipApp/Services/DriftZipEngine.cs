// SPDX-License-Identifier: GPL-2.0-or-later
//
// DriftZipEngine.cs —— 基于 SharpSevenZip 的核心服务（全功能版）。

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SharpSevenZip;

namespace DriftZipApp;

public sealed class DriftZipEngine
{
    // ═════════════════════════════════════════════════════════
    //  1. 列举压缩包内容
    // ═════════════════════════════════════════════════════════
    public IReadOnlyList<ArchiveItem> ListArchive(string archivePath, string? password = null)
        => ListArchiveInternal(archivePath, password, withDetails: false);

    public IReadOnlyList<ArchiveItem> ListArchiveWithDetails(string archivePath, string? password = null)
        => ListArchiveInternal(archivePath, password, withDetails: true);

    private IReadOnlyList<ArchiveItem> ListArchiveInternal(string archivePath, string? password, bool withDetails)
    {
        var entries = new List<ArchiveItem>();

        try
        {
            using var archive = string.IsNullOrEmpty(password)
                ? new SharpSevenZipExtractor(archivePath)
                : new SharpSevenZipExtractor(archivePath, password);

            foreach (var entry in archive.ArchiveFileData)
            {
                var item = new ArchiveItem
                {
                    Name      = Path.GetFileName(entry.FileName),
                    Path      = entry.FileName,
                    IsFolder  = entry.IsDirectory,
                    Size      = (uint)entry.Size,
                    Modified  = entry.LastWriteTime,
                    Encrypted = entry.Encrypted,
                    Method    = entry.Method,
                };

                if (withDetails)
                {
                    // ArchiveFileInfo 的 CRC / 压缩后大小
                    try { item.Crc = entry.Crc; } catch { }
                    try { item.CompressedSize = entry.CompressedSize; } catch { }
                }

                entries.Add(item);
            }
        }
        catch (SevenZipException ex)
        {
            throw new SevenZipException($"读取压缩文件失败：{ex.Message}", ex.HResult);
        }

        return entries;
    }

    // ═════════════════════════════════════════════════════════
    //  2. 测试压缩包
    // ═════════════════════════════════════════════════════════
    public async Task<bool> TestArchiveAsync(string archivePath, string? password, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var archive = string.IsNullOrEmpty(password)
                    ? new SharpSevenZipExtractor(archivePath)
                    : new SharpSevenZipExtractor(archivePath, password);

                using var ms = new MemoryStream();
                foreach (var entry in archive.ArchiveFileData)
                {
                    ct.ThrowIfCancellationRequested();
                    if (entry.IsDirectory) continue;
                    try
                    {
                        ms.SetLength(0);
                        archive.ExtractFile(entry.FileName, ms);
                    }
                    catch { return false; }
                }
                return true;
            }
            catch { return false; }
        }, ct);
    }

    // ═════════════════════════════════════════════════════════
    //  3. 解压
    // ═════════════════════════════════════════════════════════
    public async Task ExtractAsync(string archivePath, string destDir,
        string? password, IProgress<int>? progress, CancellationToken ct)
        => await Task.Run(() => Extract(archivePath, destDir, password, progress, ct), ct);

    public void Extract(string archivePath, string destDir, string? password,
        IProgress<int>? progress, CancellationToken ct)
    {
        Directory.CreateDirectory(destDir);

        try
        {
            using var archive = string.IsNullOrEmpty(password)
                ? new SharpSevenZipExtractor(archivePath)
                : new SharpSevenZipExtractor(archivePath, password);

            int total = archive.ArchiveFileData.Count;
            int done  = 0;

            archive.Extracting += (sender, args) =>
            {
                ct.ThrowIfCancellationRequested();
                done++;
                progress?.Report(total > 0 ? (int)(done * 100L / total) : 0);
            };

            archive.ExtractArchive(destDir);
            progress?.Report(100);
        }
        catch (OperationCanceledException) { throw; }
        catch (SevenZipException ex)
        {
            throw new SevenZipException($"解压失败：{ex.Message}", ex.HResult);
        }
    }

    /// <summary>提取选中条目（按包内路径）。</summary>
    public async Task ExtractSelectedAsync(string archivePath, IEnumerable<string> entryNames,
        string destDir, string? password, IProgress<int>? progress,
        ConflictPolicy conflict = ConflictPolicy.Overwrite, CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            Directory.CreateDirectory(destDir);

            using var archive = string.IsNullOrEmpty(password)
                ? new SharpSevenZipExtractor(archivePath)
                : new SharpSevenZipExtractor(archivePath, password);

            var names = entryNames.ToList();
            int total = names.Count, done = 0;

            foreach (var name in names)
            {
                ct.ThrowIfCancellationRequested();
                var outPath = Path.Combine(destDir, Path.GetFileName(name));

                // 冲突策略
                if (File.Exists(outPath))
                {
                    switch (conflict)
                    {
                        case ConflictPolicy.Skip:     done++; continue;
                        case ConflictPolicy.Rename:   outPath = MakeUniquePath(outPath); break;
                        case ConflictPolicy.Overwrite: break;
                    }
                }

                using (var fs = File.Create(outPath))
                {
                    archive.ExtractFile(name, fs);
                }

                done++;
                progress?.Report(total > 0 ? (int)(done * 100L / total) : 0);
            }
        }, ct);
    }

    private static string MakeUniquePath(string path)
    {
        var dir  = Path.GetDirectoryName(path) ?? "";
        var name = Path.GetFileNameWithoutExtension(path);
        var ext  = Path.GetExtension(path);
        int i = 1;
        string candidate;
        do { candidate = Path.Combine(dir, $"{name} ({i}){ext}"); i++; }
        while (File.Exists(candidate));
        return candidate;
    }

    // ═════════════════════════════════════════════════════════
    //  4. 压缩
    // ═════════════════════════════════════════════════════════
    public async Task CompressAsync(CompressionOptions options, string outputFile,
        IProgress<int>? progress, CancellationToken ct)
        => await Task.Run(() => Compress(options, outputFile, progress, ct), ct);

    public void Compress(CompressionOptions options, string outputFile,
        IProgress<int>? progress, CancellationToken ct)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
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

            // ── 高级参数 ──
            if (options.MultiThreaded)
                compressor.CustomParameters.Add("mt", "on");

            if (options.Encrypt && options.EncryptHeaders)
                compressor.CustomParameters.Add("he", "on");

            if (options.DictionarySize > 0)
                compressor.CustomParameters.Add("d", options.DictionarySize.ToString());

            if (options.SolidBlockSize > 0)
                compressor.CustomParameters.Add("s", options.SolidBlockSize.ToString());

            if (options.VolumeSize > 0)
                compressor.VolumeSize = options.VolumeSize;

            int total = options.SourceFiles.Count;
            int done  = 0;

            compressor.Compressing += (sender, args) =>
            {
                ct.ThrowIfCancellationRequested();
                done++;
                progress?.Report(total > 0 ? (int)(done * 100L / total) : 0);
            };

            var outputDir = Path.GetDirectoryName(Path.GetFullPath(outputFile));
            if (!string.IsNullOrEmpty(outputDir))
                Directory.CreateDirectory(outputDir);

            bool hasPassword = options.Encrypt && !string.IsNullOrEmpty(options.Password);

            // SFX 时先压成临时 .7z
            string realOutput = outputFile;
            if (options.CreateSfx && options.Format == OutputFormat.SevenZip)
                realOutput = outputFile + ".tmp.7z";

            if (dirs.Count == 1 && files.Count == 0)
            {
                compressor.CompressDirectory(dirs[0], realOutput);
            }
            else if (files.Count == 1 && dirs.Count == 0)
            {
                if (hasPassword) compressor.CompressFilesEncrypted(realOutput, options.Password!, files[0]);
                else             compressor.CompressFiles(realOutput, files[0]);
            }
            else if (dirs.Count == 0)
            {
                var arr = files.ToArray();
                if (hasPassword) compressor.CompressFilesEncrypted(realOutput, options.Password!, arr);
                else             compressor.CompressFiles(realOutput, arr);
            }
            else
            {
                var firstSource = options.SourceFiles[0];
                var baseDir = Directory.Exists(firstSource)
                    ? firstSource
                    : Path.GetDirectoryName(firstSource)!;
                compressor.CompressDirectory(baseDir, realOutput);
            }

            // ZIP 注释（仅 ZIP）
            if (!string.IsNullOrEmpty(options.Comment)
                && options.Format == OutputFormat.Zip
                && File.Exists(realOutput))
            {
                try { SetZipComment(realOutput, options.Comment!); } catch { }
            }

            // 转 SFX
            if (options.CreateSfx && options.Format == OutputFormat.SevenZip)
            {
                try
                {
                    var sfx = new SharpSevenZipSfx();
                    sfx.MakeSfx(realOutput, outputFile);
                }
                finally
                {
                    if (File.Exists(realOutput)) File.Delete(realOutput);
                }
            }

            progress?.Report(100);
        }
        catch (OperationCanceledException) { throw; }
        catch (SevenZipException ex)
        {
            throw new SevenZipException($"压缩失败：{ex.Message}", ex.HResult);
        }
    }

    // ═════════════════════════════════════════════════════════
    //  5. 添加 / 更新文件到现有压缩包
    // ═════════════════════════════════════════════════════════
    public async Task UpdateArchiveAsync(string archivePath, IEnumerable<string> newFiles,
        string? password, UpdateMode mode = UpdateMode.Add, CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            var files = newFiles.Where(File.Exists).ToList();
            if (files.Count == 0) return;

            // ZIP → 原地更新
            if (IsZip(archivePath))
            {
                UpdateZip(archivePath, files, mode);
                return;
            }

            // 7z/其他 → 重写
            RewriteArchive(archivePath, password, deleteNames: null, renameMap: null, addFiles: files);
        }, ct);
    }

    // ═════════════════════════════════════════════════════════
    //  6. 删除包内文件
    // ═════════════════════════════════════════════════════════
    public async Task DeleteEntriesAsync(string archivePath, IEnumerable<string> entryNames,
        string? password, CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            var names = entryNames.ToList();
            if (names.Count == 0) return;

            if (IsZip(archivePath)) { DeleteZipEntries(archivePath, names); return; }

            RewriteArchive(archivePath, password, deleteNames: names, renameMap: null, addFiles: null);
        }, ct);
    }

    // ═════════════════════════════════════════════════════════
    //  7. 包内重命名
    // ═════════════════════════════════════════════════════════
    public async Task RenameEntryAsync(string archivePath, string oldName, string newName,
        string? password, CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            if (IsZip(archivePath)) { RenameZipEntry(archivePath, oldName, newName); return; }

            var map = new Dictionary<string, string> { [oldName] = newName };
            RewriteArchive(archivePath, password, deleteNames: null, renameMap: map, addFiles: null);
        }, ct);
    }

    // ═════════════════════════════════════════════════════════
    //  8. ZIP 注释
    // ═════════════════════════════════════════════════════════
    public string? GetZipComment(string zipPath)
    {
        try
        {
            using var zip = ZipFile.OpenRead(zipPath);
            return zip.Comment;
        }
        catch { return null; }
    }

    public void SetZipComment(string zipPath, string comment)
    {
        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Update);
        zip.Comment = comment;
    }

    // ═════════════════════════════════════════════════════════
    //  内部：7z 重写
    // ═════════════════════════════════════════════════════════
    private void RewriteArchive(string archivePath, string? password,
        List<string>? deleteNames, Dictionary<string, string>? renameMap, List<string>? addFiles)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "DriftZipApp_Rewrite", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            using (var ex = string.IsNullOrEmpty(password)
                ? new SharpSevenZipExtractor(archivePath)
                : new SharpSevenZipExtractor(archivePath, password))
            {
                ex.ExtractArchive(tempDir);
            }

            if (deleteNames != null)
            {
                foreach (var name in deleteNames)
                {
                    var full = Path.Combine(tempDir, name.Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(full)) File.Delete(full);
                    else if (Directory.Exists(full)) Directory.Delete(full, true);
                }
            }

            if (renameMap != null)
            {
                foreach (var kv in renameMap)
                {
                    var src = Path.Combine(tempDir, kv.Key.Replace('/', Path.DirectorySeparatorChar));
                    var dst = Path.Combine(tempDir, kv.Value.Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(src)) File.Move(src, dst);
                    else if (Directory.Exists(src)) Directory.Move(src, dst);
                }
            }

            if (addFiles != null)
            {
                foreach (var f in addFiles)
                {
                    var dst = Path.Combine(tempDir, Path.GetFileName(f));
                    File.Copy(f, dst, true);
                }
            }

            var tempArchive = archivePath + ".tmp";
            var compressor = new SharpSevenZipCompressor
            {
                ArchiveFormat    = IsZip(archivePath) ? OutArchiveFormat.Zip : OutArchiveFormat.SevenZip,
                CompressionLevel = SharpSevenZip.CompressionLevel.Normal,
            };

            if (!string.IsNullOrEmpty(password))
                compressor.CompressDirectory(tempDir, tempArchive, password);
            else
                compressor.CompressDirectory(tempDir, tempArchive);

            File.Delete(archivePath);
            File.Move(tempArchive, archivePath);
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    // ═════════════════════════════════════════════════════════
    //  内部：ZIP 原地操作
    // ═════════════════════════════════════════════════════════
    private void UpdateZip(string zipPath, List<string> newFiles, UpdateMode mode)
    {
        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Update);
        foreach (var f in newFiles)
        {
            var entryName = Path.GetFileName(f);
            var existing = zip.GetEntry(entryName);

            switch (mode)
            {
                case UpdateMode.Add:
                    if (existing == null) zip.CreateEntryFromFile(f, entryName);
                    break;
                case UpdateMode.Update:
                case UpdateMode.Overwrite:
                    existing?.Delete();
                    zip.CreateEntryFromFile(f, entryName);
                    break;
                case UpdateMode.Freshen:
                    if (existing != null) { existing.Delete(); zip.CreateEntryFromFile(f, entryName); }
                    break;
                case UpdateMode.Sync:
                    foreach (var e in zip.Entries.ToList())
                        if (e.FullName != entryName) e.Delete();
                    existing?.Delete();
                    zip.CreateEntryFromFile(f, entryName);
                    break;
            }
        }
    }

    private void DeleteZipEntries(string zipPath, List<string> names)
    {
        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Update);
        foreach (var name in names) zip.GetEntry(name)?.Delete();
    }

    private void RenameZipEntry(string zipPath, string oldName, string newName)
    {
        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Update);
        var entry = zip.GetEntry(oldName);
        if (entry == null) return;

        var newEntry = zip.CreateEntry(newName);
        using (var src = entry.Open())
        using (var dst = newEntry.Open())
            src.CopyTo(dst);
        entry.Delete();
    }

    private static bool IsZip(string path)
        => path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
}

// ═════════════════════════════════════════════════════════
//  枚举
// ═════════════════════════════════════════════════════════
public enum ConflictPolicy { Overwrite, Skip, Rename }
public enum UpdateMode { Add, Update, Freshen, Sync, Overwrite }

// ═════════════════════════════════════════════════════════
//  异常
// ═════════════════════════════════════════════════════════
public sealed class SevenZipException : Exception
{
    public int HResult7z { get; }
    public SevenZipException(string message) : base(message) { }
    public SevenZipException(string message, int hr) : base(message) { HResult7z = hr; }
    public SevenZipException(string message, Exception inner) : base(message, inner) { }
}

// ═════════════════════════════════════════════════════════
//  扩展
// ═════════════════════════════════════════════════════════
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