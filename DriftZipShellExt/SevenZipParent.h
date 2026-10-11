#pragma once
#include <shobjidl.h>
#include <shlwapi.h>
#include <wrl.h>
#include <vector>
#include "EnumExplorerCommand.h"
#include "SubCommands.h"

using namespace Microsoft::WRL;

class SevenZipParent :
    public RuntimeClass<RuntimeClassFlags<ClassicCom>, IExplorerCommand>
{
public:
    IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* ppszName) override
    { return SHStrDupW(L"DriftZip", ppszName); }

    IFACEMETHODIMP GetIcon(IShellItemArray*, LPWSTR* ppszIcon) override
    {
        wchar_t modulePath[MAX_PATH]{};
        HMODULE hSelf = nullptr;
        GetModuleHandleExW(
            GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
            reinterpret_cast<LPCWSTR>(&GetIcon), &hSelf);
        GetModuleFileNameW(hSelf, modulePath, MAX_PATH);

        std::wstring iconRef = std::wstring(modulePath) + L",-1";
        return SHStrDupW(iconRef.c_str(), ppszIcon);
    }
    { *ppszIcon = nullptr; return E_NOTIMPL; }

    IFACEMETHODIMP GetToolTip(IShellItemArray*, LPWSTR* ppszInfotip) override
    { *ppszInfotip = nullptr; return E_NOTIMPL; }

    IFACEMETHODIMP GetCanonicalName(GUID* pguidCommandName) override
    { *pguidCommandName = GUID_NULL; return E_NOTIMPL; }

    IFACEMETHODIMP GetState(IShellItemArray*, BOOL, EXPCMDSTATE* pCmdState) override
    { *pCmdState = ECS_ENABLED; return S_OK; }

    IFACEMETHODIMP Invoke(IShellItemArray*, IBindCtx*) override
    { return S_OK; }

    IFACEMETHODIMP GetFlags(EXPCMDFLAGS* pFlags) override
    { *pFlags = ECF_HASSUBCOMMANDS; return S_OK; }

    IFACEMETHODIMP EnumSubCommands(IEnumExplorerCommand** ppEnum) override
    {
        std::vector<ComPtr<IExplorerCommand>> cmds;
        cmds.push_back(Make<ExtractToCommand>());
        cmds.push_back(Make<CompressToCommand>());
        cmds.push_back(Make<ExtractHereCommand>());
        cmds.push_back(Make<CompressHereCommand>());
        auto e = Make<EnumExplorerCommand>(std::move(cmds));
        return e.CopyTo(ppEnum);
    }
};
