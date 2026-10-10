// SPDX-License-Identifier: GPL-2.0-or-later
//
// IUnknown.cs —— COM IUnknown 接口的 .NET 声明。

using System;
using System.Runtime.InteropServices;

namespace SevenZipApp.Interop;

[ComImport]
[Guid("00000000-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUnknown
{
    [PreserveSig]
    int QueryInterface([In] in Guid iid, out IntPtr ppv);

    [PreserveSig]
    uint AddRef();

    [PreserveSig]
    uint Release();
}
