// SPDX-License-Identifier: GPL-2.0-or-later
//
// CompressionOptions.cs

using System.Collections.Generic;

namespace DriftZipApp;

public enum OutputFormat
{
    SevenZip,
    Zip,
    Tar,
    GZip,
    BZip2,
    XZ,
}

public enum CompressionLevel
{
    Store,
    Fastest,
    Fast,
    Normal,
    Maximum,
    Ultra,
}

public sealed class CompressionOptions
{
    public OutputFormat Format { get; set; } = OutputFormat.SevenZip;
    public CompressionLevel Level { get; set; } = CompressionLevel.Normal;
    public bool Solid { get; set; } = true;
    public bool MultiThreaded { get; set; } = true;
    public bool Encrypt { get; set; }
    public string? Password { get; set; }
    public List<string> SourceFiles { get; set; } = new();

    // ── 高级选项 ──
    public bool EncryptHeaders { get; set; }
    public long VolumeSize { get; set; }
    public long DictionarySize { get; set; }
    public long SolidBlockSize { get; set; }
    public bool CreateSfx { get; set; }
    public string? Comment { get; set; }
}
