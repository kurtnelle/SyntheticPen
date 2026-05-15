namespace SyntheticPen.Vectorize;

/// <summary>
/// A foreground/background bitmap (1 = ink). Carries the affine that maps a
/// raster pixel back to source SVG coordinates so downstream stages can emit
/// points in the original space:
/// <c>svg = pixel / Scale + Origin</c>.
/// </summary>
public sealed class BinaryMask
{
    public int Width { get; }
    public int Height { get; }

    /// <summary>Pixels-per-SVG-unit used when rendering.</summary>
    public double Scale { get; }
    public double OriginX { get; }
    public double OriginY { get; }

    private readonly byte[] _data;

    public BinaryMask(int width, int height, double scale, double originX, double originY)
    {
        Width = width;
        Height = height;
        Scale = scale;
        OriginX = originX;
        OriginY = originY;
        _data = new byte[width * height];
    }

    public byte this[int x, int y]
    {
        get => _data[y * Width + x];
        set => _data[y * Width + x] = value;
    }

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    public (double X, double Y) ToSvg(double px, double py)
        => (px / Scale + OriginX, py / Scale + OriginY);

    /// <summary>Copy the raw buffer (1/0). Used by stages that thin in place.</summary>
    public byte[] Snapshot() => (byte[])_data.Clone();

    public void Load(byte[] buffer) => Array.Copy(buffer, _data, _data.Length);
}
