// SPDX-License-Identifier: GPL-2.0-or-later
//
// ArchiveItem.cs —— 压缩包内一个条目（文件或文件夹）的描述。

using System;

namespace SevenZipApp;

public sealed class ArchiveItem
{
    /// <summary>条目在压缩包内的相对路径，含目录。</summary>
    public string? Path { get; set; }

    /// <summary>条目文件名（最后一段）。</summary>
    public string? Name { get; set; }

    /// <summary>是否文件夹。</summary>
    public bool IsFolder { get; set; }

    /// <summary>解压后字节数。</summary>
    public ulong Size { get; set; }

    /// <summary>压缩后字节数。</summary>
    public ulong PackedSize { get; set; }

    /// <summary>是否加密。</summary>
    public bool Encrypted { get; set; }

    /// <summary>修改时间。</summary>
    public DateTime Modified { get; set; }

    /// <summary>压缩方法。</summary>
    public string? Method { get; set; }

    /// <summary>条目数量（用于主列表显示）。</summary>
    public int Count { get; set; }

    /// <summary>人类可读的大小字符串，用于 UI 显示。</summary>
    public string SizeDisplay => FormatBytes(Size);

    /// <summary>UI 图标字形（Segoe MDL2 Assets）。</summary>
    public string IconGlyph => IsFolder ? "\uE8B7" : "\uE8A5";

    private static string FormatBytes(ulong bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        int unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return $"{size:0.##} {units[unit]}";
    }
}
