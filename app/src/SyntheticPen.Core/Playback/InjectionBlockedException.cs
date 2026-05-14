namespace SyntheticPen.Core.Playback;

public sealed class InjectionBlockedException : Exception
{
    public InjectionBlockedException(string reason) : base(reason) { }
}
