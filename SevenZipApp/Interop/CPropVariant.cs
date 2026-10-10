// SPDX-License-Identifier: GPL-2.0-or-later
//
// CPropVariant.cs —— 7-Zip 源码中 PROPVARIANT 的最小可用子集。

using System;
using System.Runtime.InteropServices;

namespace SevenZipApp.Interop;

internal enum VarEnum : ushort
{
    VT_EMPTY = 0,
    VT_NULL = 1,
    VT_I2 = 2,
    VT_I4 = 3,
    VT_I8 = 20,
    VT_R8 = 5,
    VT_BSTR = 8,
    VT_BOOL = 11,
    VT_FILETIME = 64,
    VT_UI1 = 17,
    VT_UI2 = 18,
    VT_UI4 = 19,
    VT_UI8 = 21,
}

[StructLayout(LayoutKind.Sequential)]
internal struct FILETIME
{
    public uint dwLowDateTime;
    public uint dwHighDateTime;
}

[StructLayout(LayoutKind.Explicit)]
internal struct PROPVARIANT
{
    [FieldOffset(0)] public ushort vt;
    [FieldOffset(2)] public ushort wReserved1;
    [FieldOffset(4)] public ushort wReserved2;
    [FieldOffset(6)] public ushort wReserved3;

    [FieldOffset(8)] public sbyte cVal;
    [FieldOffset(8)] public byte bVal;
    [FieldOffset(8)] public short iVal;
    [FieldOffset(8)] public uint uiVal;
    [FieldOffset(8)] public int lVal;
    [FieldOffset(8)] public uint ulVal;
    [FieldOffset(8)] public long hVal;
    [FieldOffset(8)] public long llVal;
    [FieldOffset(8)] public float fltVal;
    [FieldOffset(8)] public double dblVal;
    [FieldOffset(8)] public FILETIME filetime;
    [FieldOffset(8)] public IntPtr bstrVal;

    [System.Diagnostics.DebuggerStepThrough]
    public object? ToObject()
    {
        return (VarEnum)vt switch
        {
            VarEnum.VT_EMPTY or VarEnum.VT_NULL => null,
            VarEnum.VT_UI1 => bVal,
            VarEnum.VT_I2 => iVal,
            VarEnum.VT_UI2 => uiVal,
            VarEnum.VT_I4 or VarEnum.VT_I8 => llVal,
            VarEnum.VT_UI4 or VarEnum.VT_UI8 => (ulong)llVal,
            VarEnum.VT_BOOL => bVal != 0,
            VarEnum.VT_FILETIME => DateTime.FromFileTimeUtc(((long)filetime.dwHighDateTime << 32) | filetime.dwLowDateTime),
            VarEnum.VT_BSTR => bstrVal == IntPtr.Zero ? null : Marshal.PtrToStringBSTR(bstrVal),
            _ => null,
        };
    }

    public void Clear()
    {
        if ((VarEnum)vt == VarEnum.VT_BSTR && bstrVal != IntPtr.Zero)
        {
            Marshal.FreeBSTR(bstrVal);
            bstrVal = IntPtr.Zero;
        }
        vt = (ushort)VarEnum.VT_EMPTY;
    }
}
