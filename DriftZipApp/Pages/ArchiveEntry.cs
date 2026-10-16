// SPDX-License-Identifier: GPL-2.0-or-later

using System;

namespace DriftZipApp.Pages;

public class ArchiveEntry
{
    public string Name { get; set; } = "";
    public string FullPath { get; set; } = "";
    public string RawName { get; set; } = "";
    public ulong Size { get; set; }
    public long CompressedSize { get; set; }
    public uint Crc { get; set; }
    public DateTime Modified { get; set; }
    public bool IsDirectory { get; set; }

    public string SizeText => IsDirectory
        ? "<文件夹>"
        : Size >= 1024 * 1024 ? $"{Size / 1024.0 / 1024.0:F1} MB"
        : Size >= 1024 ? $"{Size / 1024.0:F0} KB"
        : $"{Size} B";

    public string CompressedSizeText => IsDirectory || CompressedSize <= 0
        ? ""
        : CompressedSize >= 1024 * 1024 ? $"{CompressedSize / 1024.0 / 1024.0:F1} MB"
        : CompressedSize >= 1024 ? $"{CompressedSize / 1024.0:F0} KB"
        : $"{CompressedSize} B";

    public double RatioValue
    {
        get
        {
            if (IsDirectory || Size == 0 || CompressedSize <= 0) return 0;
            return 100.0 - (CompressedSize * 100.0 / Size);
        }
    }

    public string RatioText => RatioValue <= 0 ? "" : $"{RatioValue:F0}%";

    public string CrcText => Crc == 0 ? "" : Crc.ToString("X8");

    public string ModifiedText => Modified == DateTime.MinValue
        ? ""
        : Modified.ToString("yyyy-MM-dd HH:mm");

    public long SharpCompressedSize { get; set; }
    public string SharpRatioText => SharpCompressedSize > 0 && Size > 0
        ? $"{100.0 - (SharpCompressedSize * 100.0 / Size):F0}%"
        : "";
    public string IconGlyph => IsDirectory ? "\uE8B7" : "\uE8A5";
}