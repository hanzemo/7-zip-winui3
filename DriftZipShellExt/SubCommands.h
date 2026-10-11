#pragma once
#include <shobjidl.h>
#include <shlwapi.h>
#include <wrl.h>
#include <string>
#include "guids.h"

using namespace Microsoft::WRL;

class SubCommandBase : public RuntimeClass<RuntimeClassFlags<ClassicCom>, IExplorerCommand>
{
public:
    IFACEMETHODIMP EnumSubCommands(IEnumExplorerCommand** ppEnum) override
    { *ppEnum = nullptr; return E_NOTIMPL; }
    IFACEMETHODIMP GetIcon(IShellItemArray*, LPWSTR* ppszIcon) override
    { *ppszIcon = nullptr; return E_NOTIMPL; }
    IFACEMETHODIMP GetToolTip(IShellItemArray*, LPWSTR* ppszInfotip) override
    { *ppszInfotip = nullptr; return E_NOTIMPL; }
    IFACEMETHODIMP GetCanonicalName(GUID* pguidCommandName) override
    { *pguidCommandName = GUID_NULL; return E_NOTIMPL; }
    IFACEMETHODIMP GetState(IShellItemArray*, BOOL, EXPCMDSTATE* pCmdState) override
    { *pCmdState = ECS_ENABLED; return S_OK; }
    IFACEMETHODIMP GetFlags(EXPCMDFLAGS* pFlags) override
    { *pFlags = ECF_DEFAULT; return S_OK; }

protected:
    static std::wstring GetFirstPath(IShellItemArray* items)
    {
        if (!items) return {};
        DWORD count = 0;
        items->GetCount(&count);
        if (count == 0) return {};
        ComPtr<IShellItem> item;
        if (FAILED(items->GetItemAt(0, &item))) return {};
        PWSTR path = nullptr;
        if (FAILED(item->GetDisplayName(SIGDN_FILESYSPATH, &path))) return {};
        std::wstring s(path);
        CoTaskMemFree(path);
        return s;
    }

    static void LaunchApp(const std::wstring& args)
    {
        wchar_t modulePath[MAX_PATH]{};
        HMODULE hSelf = nullptr;
        GetModuleHandleExW(
            GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
            reinterpret_cast<LPCWSTR>(&LaunchApp), &hSelf);
        GetModuleFileNameW(hSelf, modulePath, MAX_PATH);
        PathRemoveFileSpecW(modulePath);

        wchar_t exePath[MAX_PATH]{};
        wcscpy_s(exePath, modulePath);
        PathAppendW(exePath, L"DriftZipApp.exe");

        std::wstring params = args;
        SHELLEXECUTEINFOW sei{};
        sei.cbSize = sizeof(sei);
        sei.fMask = SEE_MASK_NOASYNC | SEE_MASK_FLAG_NO_UI;
        sei.lpFile = exePath;
        sei.lpParameters = params.c_str();
        sei.nShow = SW_SHOWNORMAL;
        ShellExecuteExW(&sei);
    }
};

class ExtractToCommand : public SubCommandBase
{
public:
    IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* ppszName) override
    { return SHStrDupW(L"\u89e3\u538b\u5230\u2026", ppszName); }
    IFACEMETHODIMP Invoke(IShellItemArray* items, IBindCtx*) override
    {
        auto path = GetFirstPath(items);
        if (path.empty()) return E_FAIL;
        LaunchApp(L"--extract-to \"" + path + L"\"");
        return S_OK;
    }
};

class CompressToCommand : public SubCommandBase
{
public:
    IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* ppszName) override
    { return SHStrDupW(L"\u538b\u7f29\u5230\u2026", ppszName); }
    IFACEMETHODIMP Invoke(IShellItemArray* items, IBindCtx*) override
    {
        auto path = GetFirstPath(items);
        if (path.empty()) return E_FAIL;
        LaunchApp(L"--compress-to \"" + path + L"\"");
        return S_OK;
    }
};

class ExtractHereCommand : public SubCommandBase
{
public:
    IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* ppszName) override
    { return SHStrDupW(L"\u89e3\u538b\u5230\u6b64\u76ee\u5f55", ppszName); }
    IFACEMETHODIMP Invoke(IShellItemArray* items, IBindCtx*) override
    {
        auto path = GetFirstPath(items);
        if (path.empty()) return E_FAIL;
        LaunchApp(L"--extract-here \"" + path + L"\"");
        return S_OK;
    }
};

class CompressHereCommand : public SubCommandBase
{
public:
    IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* ppszName) override
    { return SHStrDupW(L"\u538b\u7f29\u5230\u6b64\u76ee\u5f55", ppszName); }
    IFACEMETHODIMP Invoke(IShellItemArray* items, IBindCtx*) override
    {
        auto path = GetFirstPath(items);
        if (path.empty()) return E_FAIL;
        LaunchApp(L"--compress-here \"" + path + L"\"");
        return S_OK;
    }
};
