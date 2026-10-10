// SPDX-License-Identifier: GPL-2.0-or-later
//
// ArchiveItem.cs
//

using System;

namespace SevenZipApp;

/// <summary>
/// 压缩包内一个条目（文件或文件夹）的描述。
/// </summary>
public sealed class ArchiveItem
{
    /// <summary>
    /// 条目在压缩包内的相对路径，含目录。
    /// </summary>
    public string? Path { get; internal set; }

    /// <summary>
    /// 条目文件名（最后一段）。
    /// </summary>
    public string? Name { get; internal set; }

    /// <summary>
    /// 是否文件夹。
    /// </summary>
    public bool IsFolder { get; internal set; }

    /// <summary>
    /// 解压后字节数。
    /// </summary>
    public ulong Size { get; internal set; }

    /// <summary>
    /// 压缩后字节数。
    /// </summary>
    public ulong PackedSize { get; internal set; }

    /// <summary>
    /// 是否加密。
    /// </summary>
    public bool Encrypted { get; internal set; }

    /// <summary>
    /// 修改时间。
    /// </summary>
    public DateTime Modified { get; internal set; }

    /// <summary>
    /// 压缩方法。
    /// </summary>
    public string? Method { get; internal set; }

    /// <summary>
    /// 条目数量（用于主列表显示）。
    /// </summary>
    public int Count { get; internal set; }

    internal ArchiveItem() { }
}
