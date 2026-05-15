namespace SyntheticPen.Core;

/// <summary>
/// Pixel-domain clamping for injection. Win32 pointer/mouse injection treats
/// the virtual desktop as the half-open span <c>[origin, origin+size)</c>;
/// the last addressable pixel is <c>origin+size-1</c>. A coordinate equal to
/// <c>origin+size</c> (which <c>FitToScreen</c> can produce when the fitted
/// content's far edge lands exactly on the calibrated rect's edge) is one
/// pixel out of range and <c>InjectSyntheticPointerInput</c> rejects the whole
/// event with ERROR_INVALID_PARAMETER. Clamp to the inclusive valid range
/// before injecting.
/// </summary>
public static class VirtualScreen
{
    public static (int X, int Y) ClampPixel(
        int x, int y, int originX, int originY, int width, int height)
    {
        int maxX = width <= 0 ? originX : originX + width - 1;
        int maxY = height <= 0 ? originY : originY + height - 1;
        return (
            Math.Clamp(x, originX, maxX),
            Math.Clamp(y, originY, maxY));
    }
}
