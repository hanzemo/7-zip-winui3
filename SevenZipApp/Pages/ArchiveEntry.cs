// SPDX-License-Identifier: GPL-2.0-or-later

using System;

namespace SevenZipApp.Pages;

public class ArchiveEntry
{
    public string Name { get; set; } = "";
    public string FullPath { get; set; } = "";
    public ulong Size { get; set; }
    public DateTime Modified { get; set; }
    public bool IsDirectory { get; set; }

    public string SizeText => IsDirectory ? "<文件夹>" : $"{Size:N0} B";
    public string ModifiedText => Modified == DateTime.MinValue
        ? ""
        : Modified.ToString("yyyy-MM-dd HH:mm");

    public string IconGlyph => IsDirectory ? "\uE8B7" : "\uE8A5";
}
