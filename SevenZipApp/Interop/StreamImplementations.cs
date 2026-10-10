// SPDX-License-Identifier: GPL-2.0-or-later
//
// StreamImplementations.cs —— 给 7z.dll 的流实现。

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SevenZipApp.Interop;

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
internal sealed class InFileStream : IInStream
{
    private readonly FileStream _fs;

    public InFileStream(string path, FileMode mode = FileMode.Open, FileAccess access = FileAccess.Read)
        => _fs = new FileStream(path, mode, access, FileShare.ReadWrite);

    public InFileStream(FileStream fs) => _fs = fs;

    public long Length => _fs.Length;

    int IInStream.Read(byte[] data, uint size, out uint processedSize)
    {
        int read = _fs.Read(data, 0, (int)Math.Min(size, (uint)data.Length));
        processedSize = (uint)read;
        return 0;
    }

    int IInStream.Seek(long offset, int seekOrigin, out long newPosition)
    {
        SeekOrigin so = seekOrigin switch
        {
            SeekOrigin7z.Set => SeekOrigin.Begin,
            SeekOrigin7z.Cur => SeekOrigin.Current,
            SeekOrigin7z.End => SeekOrigin.End,
            _ => SeekOrigin.Begin,
        };
        newPosition = _fs.Seek(offset, so);
        return 0;
    }

    public void Close() => _fs.Dispose();
}

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
internal sealed class OutFileStream : IOutStream
{
    private readonly FileStream _fs;

    public OutFileStream(string path) => _fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);

    int IOutStream.Write(byte[] data, uint size, out uint processedSize)
    {
        int toWrite = (int)Math.Min(size, (uint)data.Length);
        _fs.Write(data, 0, toWrite);
        processedSize = (uint)toWrite;
        return 0;
    }

    int IOutStream.Seek(long offset, int seekOrigin, out long newPosition)
    {
        SeekOrigin so = seekOrigin switch
        {
            SeekOrigin7z.Set => SeekOrigin.Begin,
            SeekOrigin7z.Cur => SeekOrigin.Current,
            SeekOrigin7z.End => SeekOrigin.End,
            _ => SeekOrigin.Begin,
        };
        newPosition = _fs.Seek(offset, so);
        return 0;
    }

    int IOutStream.SetSize(long newSize)
    {
        _fs.SetLength(newSize);
        return 0;
    }

    public void Close() => _fs.Dispose();
}

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
internal sealed class ExtractFileStream : ISequentialOutStream
{
    private readonly FileStream _fs;

    public ExtractFileStream(string path)
        => _fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);

    int ISequentialOutStream.Write(byte[] data, uint size, out uint processedSize)
    {
        int toWrite = (int)Math.Min(size, (uint)data.Length);
        _fs.Write(data, 0, toWrite);
        processedSize = (uint)toWrite;
        return 0;
    }

    public void Close() => _fs.Dispose();
}
