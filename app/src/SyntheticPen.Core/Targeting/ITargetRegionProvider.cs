using SyntheticPen.Core.Models;

namespace SyntheticPen.Core.Targeting;

public interface ITargetRegionProvider
{
    Rect? Current { get; }
    event Action<Rect?> Changed;
    void Set(Rect? region);
}
