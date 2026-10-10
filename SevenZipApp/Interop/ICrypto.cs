// SPDX-License-Identifier: GPL-2.0-or-later
//
// ICrypto.cs —— 7-Zip 加密相关回调接口。

using System;
using System.Runtime.InteropServices;

namespace SevenZipApp.Interop;

/// <summary>
/// ICryptoGetTextPassword —— 解压时提供密码。
/// </summary>
[ComImport]
[Guid("23170F69-40C1-278A-1000-000110200000")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ICryptoGetTextPassword
{
    [PreserveSig]
    int CryptoGetTextPassword(out IntPtr password); // BSTR, caller frees
}

/// <summary>
/// ICryptoGetTextPassword2 —— 压缩时提供密码。
/// </summary>
[ComImport]
[Guid("23170F69-40C1-278A-1000-000110210000")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ICryptoGetTextPassword2
{
    [PreserveSig]
    int CryptoGetTextPassword2([MarshalAs(UnmanagedType.Bool)] out bool passwordIsDefined,
                               out IntPtr password);
}
