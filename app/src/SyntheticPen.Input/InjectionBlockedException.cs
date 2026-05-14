namespace SyntheticPen.Input;

public sealed class InjectionBlockedException : Exception
{
    public InjectionBlockedException(string reason) : base(reason) { }
}
