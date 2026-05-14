namespace SyntheticPen.Svg;

public interface ISvgPathLoader
{
    Task<SvgDocument> LoadAsync(Stream svgStream, FlattenOptions opts, CancellationToken ct = default);
}
