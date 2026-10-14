// SPDX-License-Identifier: GPL-2.0-or-later

using System;
using System.IO;

namespace DriftZipApp;

public class ArchiveItem
{
    public string Name { get; set; } = "";
    public string? Path { get; set; }
    public uint Size { get; set; }
    public int Count { get; set; }

    // 浏览模式
    public bool IsDirectory { get; set; }
    public DateTime Modified { get; set; } = DateTime.MinValue;

    // 兼容旧字段名
    public bool IsFolder
    {
        get => IsDirectory;
        set => IsDirectory = value;
    }

    // 压缩包条目元数据
    public bool Encrypted { get; set; }
    public string? Method { get; set; }

    // ── 新增：详细列 / CRC / 压缩率 ──
    public uint Crc { get; set; }
    public long CompressedSize { get; set; }

    public string CrcText => Crc == 0 ? "" : Crc.ToString("X8");

    public string CompressedSizeText => IsDirectory || CompressedSize <= 0
        ? ""
        : CompressedSize >= 1024 * 1024 ? $"{CompressedSize / 1024.0 / 1024.0:F1} MB"
        : CompressedSize >= 1024 ? $"{CompressedSize / 1024.0:F0} KB"
        : $"{CompressedSize} B";

    public string RatioText
    {
        get
        {
            if (IsDirectory || Size == 0 || CompressedSize <= 0) return "";
            double ratio = 100.0 - (CompressedSize * 100.0 / Size);
            return $"{ratio:F0}%";
        }
    }

    public string EncryptedText => Encrypted ? "是" : "";

    public string SizeText => IsDirectory
        ? "<文件夹>"
        : Size >= 1024 * 1024 ? $"{Size / 1024.0 / 1024.0:F1} MB"
        : Size >= 1024 ? $"{Size / 1024.0:F0} KB"
        : $"{Size} B";

    public string ModifiedText => Modified == DateTime.MinValue
        ? ""
        : Modified.ToString("yyyy-MM-dd HH:mm");

    public string SizeDisplay => SizeText;

    public string IconGlyph => IsDirectory ? "\uE8B7" : "\uE8A5";
}