// SPDX-License-Identifier: GPL-2.0-or-later
//
// IStream.cs —— 7-Zip 中的流接口。

using System;
using System.Runtime.InteropServices;

namespace SevenZipApp.Interop;

[ComImport]
[Guid("23170F69-40C1-278A-0000-000400010000")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ISequentialInStream
{
    [PreserveSig]
    int Read([Out] byte[] data, uint size, out uint processedSize);
}

[ComImport]
[Guid("23170F69-40C1-278A-0000-000400020000")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IInStream : ISequentialInStream
{
    [PreserveSig]
    int Read([Out] byte[] data, uint size, out uint processedSize);

    [PreserveSig]
    int Seek(long offset, int seekOrigin, out long newPosition);
}

[ComImport]
[Guid("23170F69-40C1-278A-0000-000400030000")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ISequentialOutStream
{
    [PreserveSig]
    int Write([In] byte[] data, uint size, out uint processedSize);
}

[ComImport]
[Guid("23170F69-40C1-278A-0000-000400040000")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IOutStream : ISequentialOutStream
{
    [PreserveSig]
    int Write([In] byte[] data, uint size, out uint processedSize);

    [PreserveSig]
    int Seek(long offset, int seekOrigin, out long newPosition);

    [PreserveSig]
    int SetSize(long newSize);
}

internal static class SeekOrigin7z
{
    public const int Set = 0;
    public const int Cur = 1;
    public const int End = 2;
}
