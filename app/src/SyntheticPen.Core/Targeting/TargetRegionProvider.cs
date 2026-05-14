using SyntheticPen.Core.Models;

namespace SyntheticPen.Core.Targeting;

public sealed class TargetRegionProvider : ITargetRegionProvider
{
    private Rect? _current;

    public Rect? Current => _current;
    public event Action<Rect?>? Changed;

    public void Set(Rect? region)
    {
        _current = region;
        Changed?.Invoke(region);
    }
}
