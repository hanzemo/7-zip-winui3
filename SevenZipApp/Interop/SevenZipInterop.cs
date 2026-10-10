// SPDX-License-Identifier: GPL-2.0-or-later
//
// SevenZipInterop.cs —— 与 7z.dll 的 P/Invoke 边界。
//   7z.dll (来自官方 7-Zip 发行版) 导出一个原生 C 函数:
//     HRESULT CreateObject(const GUID* clsid, const GUID* iid, void** outObject);
//   它按 CLSID+IID 创建对应 Archive handler 实例 (IInArchive / IOutArchive)。

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace SevenZipApp.Interop;

/// <summary>
/// 对官方 7z.dll 的薄封装。线程安全；首次访问时按当前进程架构加载 7z.dll。
/// </summary>
internal static class SevenZipInterop
{
    private static IntPtr _module = IntPtr.Zero;
    private static readonly object _gate = new();

    private delegate int CreateObjectDelegate(
        in Guid clsid, in Guid iid, out IntPtr outObject);

    private static CreateObjectDelegate? _createObject;

    public static void EnsureLoaded()
    {
        if (_module != IntPtr.Zero) return;
        lock (_gate)
        {
            if (_module != IntPtr.Zero) return;

            string[] candidates =
            {
                Path.Combine(AppContext.BaseDirectory, "7z.dll"),
                Path.Combine(AppContext.BaseDirectory, "Assets", "7z.dll"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.dll"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "7-Zip", "7z.dll"),
            };

            IntPtr h = IntPtr.Zero;
            foreach (var path in candidates)
            {
                if (File.Exists(path))
                {
                    h = LoadLibrary(path);
                    if (h != IntPtr.Zero) break;
                }
            }

            if (h == IntPtr.Zero)
                h = LoadLibrary("7z.dll");

            if (h == IntPtr.Zero)
                throw new DllNotFoundException(
                    "无法加载 7z.dll。请将官方 7-Zip 发行版中的 7z.dll 复制到应用程序目录，或安装 7-Zip。");

            var proc = GetProcAddress(h, "CreateObject");
            if (proc == IntPtr.Zero)
            {
                FreeLibrary(h);
                throw new EntryPointNotFoundException("7z.dll 不包含 CreateObject 导出函数。");
            }

            _createObject = Marshal.GetDelegateForFunctionPointer<CreateObjectDelegate>(proc);
            _module = h;
        }
    }

    public static IntPtr CreateObject(in Guid clsid, in Guid iid)
    {
        EnsureLoaded();
        int hr = _createObject!(in clsid, in iid, out IntPtr obj);
        if (hr != 0)
            throw new COMException($"CreateObject 失败 (CLSID={clsid}, hr=0x{hr:X8})", hr);
        return obj;
    }

    public static IInArchive CreateInArchive(Guid clsid)
    {
        var p = CreateObject(in clsid, ComGuids.IID_IInArchive);
        try
        {
            return (IInArchive)Marshal.GetObjectForIUnknown(p);
        }
        finally
        {
            Marshal.Release(p);
        }
    }

    public static IOutArchive CreateOutArchive(Guid clsid)
    {
        var p = CreateObject(in clsid, ComGuids.IID_IOutArchive);
        try
        {
            return (IOutArchive)Marshal.GetObjectForIUnknown(p);
        }
        finally
        {
            Marshal.Release(p);
        }
    }

    #region Native imports
    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeLibrary(IntPtr hModule);

    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
    #endregion
}
