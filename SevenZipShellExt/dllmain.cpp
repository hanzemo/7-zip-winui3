#include <windows.h>
#include <wrl.h>
#include <shlwapi.h>
#include "ClassFactory.h"
#include "SevenZipParent.h"
#include "guids.h"

using namespace Microsoft::WRL;

HMODULE g_hModule = nullptr;
LONG g_refCount = 0;

BOOL WINAPI DllMain(HINSTANCE hInstance, DWORD reason, LPVOID)
{
    if (reason == DLL_PROCESS_ATTACH) {
        g_hModule = hInstance;
        DisableThreadLibraryCalls(hInstance);
    }
    return TRUE;
}

STDAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, void** ppv)
{
    if (IsEqualCLSID(rclsid, CLSID_SevenZipParent))
        return Make<ClassFactory<SevenZipParent>>().CopyTo(riid, ppv);
    return CLASS_E_CLASSNOTAVAILABLE;
}

STDAPI DllCanUnloadNow()
{
    return g_refCount == 0 ? S_OK : S_FALSE;
}
