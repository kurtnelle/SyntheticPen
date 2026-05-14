namespace SyntheticPen.Svg;

public sealed class SkiaSvgPathLoader : ISvgPathLoader
{
    public Task<SvgDocument> LoadAsync(Stream svgStream, FlattenOptions opts, CancellationToken ct = default)
        => throw new NotImplementedException("Implemented in Task 5");
}
