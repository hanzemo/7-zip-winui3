// SPDX-License-Identifier: GPL-2.0-or-later

using System;
using System.IO;

namespace SevenZipApp;

public class ArchiveItem
{
    public string Name { get; set; } = "";
    public string? Path { get; set; }
    public uint Size { get; set; }
    public int Count { get; set; }
    public bool IsDirectory { get; set; }
    public DateTime Modified { get; set; } = DateTime.MinValue;

    public string SizeText => IsDirectory
        ? "<文件夹>"
        : Size >= 1024 * 1024 ? $"{Size / 1024.0 / 1024.0:F1} MB"
        : Size >= 1024 ? $"{Size / 1024.0:F0} KB"
        : $"{Size} B";

    public string ModifiedText => Modified == DateTime.MinValue
        ? ""
        : Modified.ToString("yyyy-MM-dd HH:mm");

    public string IconGlyph => IsDirectory ? "\uE8B7" : "\uE8A5";
}
