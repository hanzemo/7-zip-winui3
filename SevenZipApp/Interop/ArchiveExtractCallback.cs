// SPDX-License-Identifier: GPL-2.0-or-later
//
// ArchiveExtractCallback.cs —— 7z.dll 回调接口的 C# 实现。

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace SevenZipApp.Interop;

internal sealed class ArchiveExtractCallback :
    IArchiveExtractCallback, ICryptoGetTextPassword
{
    private readonly string _destDir;
    private readonly string? _password;
    private readonly IProgress<long>? _progress;
    private readonly Func<uint, ArchiveItem, bool>? _filter;

    private ArchiveItem? _current;
    private ExtractFileStream? _currentStream;
    private IReadOnlyList<ArchiveItem>? _items;

    public ArchiveExtractCallback(string destDir, string? password,
        IProgress<long>? progress = null,
        Func<uint, ArchiveItem, bool>? filter = null)
    {
        _destDir = destDir;
        _password = password;
        _progress = progress;
        _filter = filter;
    }

    internal void SetArchiveItems(IReadOnlyList<ArchiveItem> items) => _items = items;

    int IArchiveExtractCallback.SetTotal(ulong total) => 0;

    int IArchiveExtractCallback.SetCompleted(ref ulong completeValue)
    {
        _progress?.Report((long)completeValue);
        return 0;
    }

    int IArchiveExtractCallback.GetStream(uint index, out ISequentialOutStream outStream, int askExtractMode)
    {
        outStream = null!;
        if (askExtractMode == AskMode.Skip)
            return 0;

        if (_items is null || (int)index >= _items.Count)
            return unchecked((int)0x80004001);

        var item = _items[(int)index];
        _current = item;

        if (_filter is not null && !_filter(index, item))
            return 0;

        if (askExtractMode == AskMode.Test)
        {
            outStream = new NullOutStream();
            return 0;
        }

        string fullPath = Path.Combine(_destDir, item.Path ?? item.Name ?? $"item_{index}");
        try
        {
            fullPath = SafePath(_destDir, item.Path ?? item.Name ?? $"item_{index}");
        }
        catch (InvalidDataException ex)
        {
            return unchecked((int)0x80070057);
        }

        if (item.IsFolder)
        {
            Directory.CreateDirectory(fullPath);
            return 0;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        _currentStream = new ExtractFileStream(fullPath);
        outStream = _currentStream;
        return 0;
    }

    int IArchiveExtractCallback.PrepareOperation(int askExtractMode, bool isExternal) => 0;

    int IArchiveExtractCallback.SetOperationResult(int operationResult)
    {
        try
        {
            _currentStream?.Close();
            _currentStream = null;
        }
        catch { }

        return operationResult switch
        {
            OperationResult.OK => 0,
            OperationResult.WrongPassword => unchecked((int)0x80070057),
            _ => unchecked((int)0x80004005),
        };
    }

    int ICryptoGetTextPassword.CryptoGetTextPassword(out IntPtr password)
    {
        if (string.IsNullOrEmpty(_password))
        {
            password = IntPtr.Zero;
            return unchecked((int)0x80004001);
        }
        password = Marshal.StringToBSTR(_password);
        return 0;
    }

    private static string SafePath(string root, string relative)
    {
        relative = relative.Replace('\\', Path.DirectorySeparatorChar)
                           .Replace('/', Path.DirectorySeparatorChar);
        var combined = Path.GetFullPath(Path.Combine(root, relative));
        var rootFull = Path.GetFullPath(root);
        if (!combined.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"压缩包包含越界路径: {relative}");
        return combined;
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    private sealed class NullOutStream : ISequentialOutStream
    {
        int ISequentialOutStream.Write(byte[] data, uint size, out uint processedSize)
        {
            processedSize = size;
            return 0;
        }
    }
}

internal sealed class ArchiveOpenCallback : IArchiveOpenCallback
{
    private readonly IProgress<long>? _progress;
    public ArchiveOpenCallback(IProgress<long>? progress = null) => _progress = progress;

    int IArchiveOpenCallback.SetTotal(ref ulong total) => 0;

    int IArchiveOpenCallback.SetCompleted(ref ulong completeValue)
    {
        _progress?.Report((long)completeValue);
        return 0;
    }
}
