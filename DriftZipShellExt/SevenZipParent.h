#pragma once
#include <shobjidl.h>
#include <shlwapi.h>
#include <wrl.h>
#include <vector>
#include <string>
#include "EnumExplorerCommand.h"
#include "SubCommands.h"

using namespace Microsoft::WRL;

// 在 dllmain.cpp 里定义的全局 DLL 模块句柄
extern HMODULE g_hModule;

class SevenZipParent :
    public RuntimeClass<RuntimeClassFlags<ClassicCom>, IExplorerCommand>
{
public:
    IFACEMETHODIMP GetTitle(IShellItemArray*, LPWSTR* ppszName) override
    {
        return SHStrDupW(L"DriftZip", ppszName);
    }

    IFACEMETHODIMP GetIcon(IShellItemArray*, LPWSTR* ppszIcon) override
    {
        wchar_t modulePath[MAX_PATH]{};
        GetModuleFileNameW(g_hModule, modulePath, MAX_PATH);

        std::wstring iconRef = std::wstring(modulePath) + L",-1";
        return SHStrDupW(iconRef.c_str(), ppszIcon);
    }

    IFACEMETHODIMP GetToolTip(IShellItemArray*, LPWSTR* ppszInfotip) override
    {
        *ppszInfotip = nullptr;
        return E_NOTIMPL;
    }

    IFACEMETHODIMP GetCanonicalName(GUID* pguidCommandName) override
    {
        *pguidCommandName = GUID_NULL;
        return E_NOTIMPL;
    }

    IFACEMETHODIMP GetState(IShellItemArray*, BOOL, EXPCMDSTATE* pCmdState) override
    {
        *pCmdState = ECS_ENABLED;
        return S_OK;
    }

    IFACEMETHODIMP Invoke(IShellItemArray*, IBindCtx*) override
    {
        return S_OK;
    }

    IFACEMETHODIMP GetFlags(EXPCMDFLAGS* pFlags) override
    {
        *pFlags = ECF_HASSUBCOMMANDS;
        return S_OK;
    }

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
