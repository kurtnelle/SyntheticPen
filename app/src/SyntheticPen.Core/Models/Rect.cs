namespace SyntheticPen.Core.Models;

public readonly record struct Rect(double X, double Y, double W, double H)
{
    public bool IsEmpty => W <= 0 || H <= 0;
    public double Right => X + W;
    public double Bottom => Y + H;
}
