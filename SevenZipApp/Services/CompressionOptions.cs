// SPDX-License-Identifier: GPL-2.0-or-later
//
// CompressionOptions.cs
//

using System.Collections.Generic;

namespace SevenZipApp;

/// <summary>
/// 压缩级别（与 7-Zip 压缩级别对应）。
/// </summary>
public enum CompressionLevel
{
    Store   = 0, // 仅存储
    Fastest = 1,
    Fast    = 3,
    Normal  = 5,
    Maximum = 7,
    Ultra   = 9,
}

/// <summary>
/// 压缩选项参数。
/// </summary>
public sealed class CompressionOptions
{
    /// <summary>输出格式（7z、zip 等）。</summary>
    public OutputFormat Format { get; set; } = OutputFormat.SevenZip;

    /// <summary>压缩级别（0-9）。</summary>
    public CompressionLevel Level { get; set; } = CompressionLevel.Normal;

    /// <summary>固实压缩（仅 7z 生效）。</summary>
    public bool Solid { get; set; } = true;

    /// <summary>使用多线程（仅 7z/zip 内 LZMA 等）。</summary>
    public bool MultiThreaded { get; set; } = true;

    /// <summary>加密？</summary>
    public bool Encrypt { get; set; }

    /// <summary>加密密码。</summary>
    public string? Password { get; set; }

    /// <summary>加密文件头（仅 7z）—— 让目录也看不见。</summary>
    public bool EncryptHeaders { get; set; }

    /// <summary>需要打包的源文件列表（绝对路径）。</summary>
    public IReadOnlyList<string> SourceFiles { get; set; } = System.Array.Empty<string>();

    /// <summary>压缩包内根目录前缀（可为空）。</summary>
    public string? RootPrefix { get; set; }
}

/// <summary>
/// 输出格式枚举。
/// </summary>
public enum OutputFormat
{
    SevenZip, // .7z
    Zip,      // .zip
    Tar,      // .tar
    GZip,     // .gz
    BZip2,    // .bz2
    XZ,       // .xz
    Wim,      // .wim
}
