#pragma once
#include <shobjidl.h>
#include <vector>
#include <wrl.h>

using namespace Microsoft::WRL;

class EnumExplorerCommand :
    public RuntimeClass<RuntimeClassFlags<ClassicCom>, IEnumExplorerCommand>
{
public:
    explicit EnumExplorerCommand(std::vector<ComPtr<IExplorerCommand>> cmds)
        : _cmds(std::move(cmds)), _index(0) {}

    IFACEMETHODIMP Next(ULONG celt, IExplorerCommand** rgelt, ULONG* pceltFetched) override
    {
        ULONG fetched = 0;
        while (fetched < celt && _index < _cmds.size())
        {
            _cmds[_index].CopyTo(IID_PPV_ARGS(&rgelt[fetched]));
            ++_index; ++fetched;
        }
        if (pceltFetched) *pceltFetched = fetched;
        return fetched == celt ? S_OK : S_FALSE;
    }
    IFACEMETHODIMP Skip(ULONG celt) override
    {
        _index += celt;
        if (_index > _cmds.size()) _index = _cmds.size();
        return S_OK;
    }
    IFACEMETHODIMP Reset() override { _index = 0; return S_OK; }
    IFACEMETHODIMP Clone(IEnumExplorerCommand** ppenum) override
    {
        auto clone = Make<EnumExplorerCommand>(_cmds);
        return clone->QueryInterface(IID_PPV_ARGS(ppenum));
    }
private:
    std::vector<ComPtr<IExplorerCommand>> _cmds;
    size_t _index;
};
