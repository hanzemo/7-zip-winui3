// SPDX-License-Identifier: GPL-2.0-or-later
//
// ArchiveUpdateCallback.cs —— 压缩回调。

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace SevenZipApp.Interop;

internal sealed class UpdateInputItem
{
    public string SourcePath = null!;
    public string ArchivePath = null!;
    public bool IsFolder;
    public ulong Size;
    public DateTime Modified;
    public uint Attributes;
}

internal sealed class ArchiveUpdateCallback :
    IArchiveUpdateCallback, ICryptoGetTextPassword2
{
    private readonly IReadOnlyList<UpdateInputItem> _items;
    private readonly string? _password;
    private readonly bool _encrypt;
    private readonly IProgress<ulong>? _progress;

    private int _cursor = -1;
    private FileStream? _currentFs;

    public ArchiveUpdateCallback(IReadOnlyList<UpdateInputItem> items,
        string? password, bool encrypt, IProgress<ulong>? progress = null)
    {
        _items = items;
        _password = password;
        _encrypt = encrypt;
        _progress = progress;
    }

    int IArchiveUpdateCallback.SetTotal(ulong total) => 0;

    int IArchiveUpdateCallback.SetCompleted(ref ulong completeValue)
    {
        _progress?.Report(completeValue);
        return 0;
    }

    int IArchiveUpdateCallback.GetUpdateItemInfo(uint index,
        out uint newData, out uint newProperties, out uint indexInArchive)
    {
        newData = 1;
        newProperties = 1;
        indexInArchive = uint.MaxValue;
        return 0;
    }

    int IArchiveUpdateCallback.GetProperty(uint index, ItemPropId propId, out PROPVARIANT value)
    {
        value = default;
        if (index >= (uint)_items.Count)
            return unchecked((int)0x80070057);

        var item = _items[(int)index];
        switch (propId)
        {
            case ItemPropId.kpidPath:
                value.vt = (ushort)VarEnum.VT_BSTR;
                value.bstrVal = Marshal.StringToBSTR(item.ArchivePath);
                return 0;

            case ItemPropId.kpidName:
                value.vt = (ushort)VarEnum.VT_BSTR;
                value.bstrVal = Marshal.StringToBSTR(Path.GetFileName(item.ArchivePath.TrimEnd('/')));
                return 0;

            case ItemPropId.kpidIsFolder:
                value.vt = (ushort)VarEnum.VT_BOOL;
                value.bVal = item.IsFolder ? (byte)1 : (byte)0;
                return 0;

            case ItemPropId.kpidSize:
                value.vt = (ushort)VarEnum.VT_UI8;
                value.llVal = (long)item.Size;
                return 0;

            case ItemPropId.kpidAttrib:
                value.vt = (ushort)VarEnum.VT_UI4;
                value.ulVal = item.Attributes;
                return 0;

            case ItemPropId.kpidMTime:
                value.vt = (ushort)VarEnum.VT_FILETIME;
                long ft = item.Modified.ToFileTimeUtc();
                value.filetime = new FILETIME
                {
                    dwLowDateTime = (uint)(ft & 0xFFFFFFFFu),
                    dwHighDateTime = (uint)((ulong)ft >> 32),
                };
                return 0;

            default:
                value.vt = (ushort)VarEnum.VT_EMPTY;
                return 0;
        }
    }

    int IArchiveUpdateCallback.GetStream(uint index, out ISequentialInStream inStream)
    {
        inStream = null!;
        if (index >= (uint)_items.Count)
            return unchecked((int)0x80070057);

        var item = _items[(int)index];
        if (item.IsFolder)
            return 0;

        _cursor = (int)index;
        _currentFs = new FileStream(item.SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        inStream = new InFileStream(_currentFs);
        return 0;
    }

    int IArchiveUpdateCallback.SetOperationResult(int operationResult)
    {
        try
        {
            _currentFs?.Dispose();
            _currentFs = null;
        }
        catch { }
        return operationResult == OperationResult.OK ? 0 : unchecked((int)0x80004005);
    }

    int ICryptoGetTextPassword2.CryptoGetTextPassword2(out bool passwordIsDefined, out IntPtr password)
    {
        passwordIsDefined = _encrypt && !string.IsNullOrEmpty(_password);
        password = passwordIsDefined ? Marshal.StringToBSTR(_password!) : IntPtr.Zero;
        return 0;
    }
}
