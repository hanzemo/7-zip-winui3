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

    // 兼容 DriftZipEngine / FilesPage 里的旧字段名
    public bool IsFolder
    {
        get => IsDirectory;
        set => IsDirectory = value;
    }

    // 压缩包条目元数据（DriftZipEngine 用）
    public bool Encrypted { get; set; }
    public string? Method { get; set; }

    public string SizeText => IsDirectory
        ? "<文件夹>"
        : Size >= 1024 * 1024 ? $"{Size / 1024.0 / 1024.0:F1} MB"
        : Size >= 1024 ? $"{Size / 1024.0:F0} KB"
        : $"{Size} B";

    public string ModifiedText => Modified == DateTime.MinValue
        ? ""
        : Modified.ToString("yyyy-MM-dd HH:mm");

    // 兼容 FilesPage 里绑定的旧属性名
    public string SizeDisplay => SizeText;

    public string IconGlyph => IsDirectory ? "\uE8B7" : "\uE8A5";
}
