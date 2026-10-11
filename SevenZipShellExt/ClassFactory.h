#pragma once
#include <windows.h>
#include <wrl.h>
#include <wrl/implements.h>

using namespace Microsoft::WRL;

template<typename T>
class ClassFactory : public RuntimeClass<RuntimeClassFlags<ClassicCom>, IClassFactory>
{
public:
    IFACEMETHODIMP CreateInstance(IUnknown* outer, REFIID riid, void** ppv) override
    {
        if (outer != nullptr) return CLASS_E_NOAGGREGATION;
        return Make<T>().CopyTo(riid, ppv);
    }
    IFACEMETHODIMP LockServer(BOOL) override { return S_OK; }
};
