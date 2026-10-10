// SPDX-License-Identifier: GPL-2.0-or-later
//
// IArchive.cs —— 7-Zip 的 Archive 接口。

using System;
using System.Runtime.InteropServices;

namespace SevenZipApp.Interop;

[ComImport]
[Guid("23170F69-40C1-278A-1000-000110070000")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IInArchive
{
    [PreserveSig]
    int Open(IInStream stream, ref long maxCheckStartPosition,
             [MarshalAs(UnmanagedType.Interface)] IArchiveOpenCallback openArchiveCallback);

    [PreserveSig]
    int Close();

    [PreserveSig]
    int GetNumberOfItems(out uint numItems);

    [PreserveSig]
    int GetProperty(uint index, ItemPropId propId, out PROPVARIANT value);

    [PreserveSig]
    int Extract([In] uint[] indices, uint numItems,
                [MarshalAs(UnmanagedType.Bool)] bool testMode,
                [MarshalAs(UnmanagedType.Interface)] IArchiveExtractCallback extractCallback);

    [PreserveSig]
    int GetArchiveProperty(ArchivePropId propId, out PROPVARIANT value);

    [PreserveSig]
    int GetNumberOfProperties(out uint numProperties);

    [PreserveSig]
    int GetNumberOfArchiveProperties(out uint numProperties);
}

[ComImport]
[Guid("23170F69-40C1-278A-1000-000110080000")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IOutArchive
{
    [PreserveSig]
    int UpdateItems(ISequentialOutStream outStream, uint numItems,
                    [MarshalAs(UnmanagedType.Interface)] IArchiveUpdateCallback callback);
}

[ComImport]
[Guid("23170F69-40C1-278A-1000-000110040000")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IArchiveOpenCallback
{
    [PreserveSig]
    int SetTotal(ref ulong total);

    [PreserveSig]
    int SetCompleted(ref ulong completeValue);
}

[ComImport]
[Guid("23170F69-40C1-278A-1000-000110100000")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IArchiveExtractCallback
{
    [PreserveSig]
    int SetTotal(ulong total);

    [PreserveSig]
    int SetCompleted(ref ulong completeValue);

    [PreserveSig]
    int GetStream(uint index, out ISequentialOutStream outStream, int askExtractMode);

    [PreserveSig]
    int PrepareOperation(int askExtractMode, [MarshalAs(UnmanagedType.Bool)] bool isExternal);

    [PreserveSig]
    int SetOperationResult(int operationResult);
}

[ComImport]
[Guid("23170F69-40C1-278A-1000-000110120000")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IArchiveUpdateCallback
{
    [PreserveSig]
    int SetTotal(ulong total);

    [PreserveSig]
    int SetCompleted(ref ulong completeValue);

    [PreserveSig]
    int GetUpdateItemInfo(uint index,
                          out uint newData, out uint newProperties, out uint indexInArchive);

    [PreserveSig]
    int GetProperty(uint index, ItemPropId propId, out PROPVARIANT value);

    [PreserveSig]
    int GetStream(uint index, out ISequentialInStream inStream);

    [PreserveSig]
    int SetOperationResult(int operationResult);
}

internal enum ItemPropId : uint
{
    kpidNoProperty = 0,
    kpidMainSubfile,
    kpidHandlerItemIndex,
    kpidPath,
    kpidName,
    kpidExtension,
    kpidIsFolder,
    kpidSize,
    kpidPackedSize,
    kpidAttrib,
    kpidCTime,
    kpidATime,
    kpidMTime,
    kpidSolid,
    kpidComment,
    kpidEncrypted,
    kpidSplitBefore,
    kpidSplitAfter,
    kpidDictionarySize,
    kpidWinSize,
    kpidCRC,
    kpidHostOS,
    kpidFileSystem,
    kpidUser,
    kpidGroup,
    kpidBlock,
    kpidCommented,
    kpidMethod,
    kpidError,
    kpidChecksum,
}

internal enum ArchivePropId : uint
{
    kpidArchiveName = 0,
    kpidSize,
    kpidPackedSize,
    kpidNumSubFolders,
    kpidNumSubFiles,
    kpidNumErrors,
    kpidError,
}

internal static class AskMode
{
    public const int Extract = 0;
    public const int Test = 1;
    public const int Skip = 2;
}

internal static class OperationResult
{
    public const int OK = 0;
    public const int UnsupportedMethod = 1;
    public const int DataError = 2;
    public const int CRCError = 3;
    public const int Unavailable = 4;
    public const int UnexpectedEnd = 5;
    public const int DataAfterEnd = 6;
    public const int IsNotArc = 7;
    public const int HeadersError = 8;
    public const int WrongPassword = 9;
}
