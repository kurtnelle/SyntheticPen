using SyntheticPen.Core.Models;

namespace SyntheticPen.Svg;

public sealed class SkiaSvgPathLoader : ISvgPathLoader
{
    public Task<IReadOnlyList<Stroke>> LoadAsync(Stream svgStream, CancellationToken ct = default)
        => throw new NotImplementedException("Phase 1");
}
