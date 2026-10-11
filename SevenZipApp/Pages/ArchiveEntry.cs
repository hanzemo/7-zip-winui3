// SPDX-License-Identifier: GPL-2.0-or-later

using System;

namespace SevenZipApp.Pages;

public class ArchiveEntry
{
    public string Name { get; set; } = "";
    public ulong Size { get; set; }
    public DateTime Modified { get; set; }
    public bool IsDirectory { get; set; }

    public string SizeText => IsDirectory ? "<文件夹>" : $"{Size:N0} B";
    public string ModifiedText => Modified.ToString("yyyy-MM-dd HH:mm");
}
