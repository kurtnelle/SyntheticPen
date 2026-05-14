using SyntheticPen.Core.Models;

namespace SyntheticPen.Svg;

public interface ISvgPathLoader
{
    Task<IReadOnlyList<Stroke>> LoadAsync(Stream svgStream, CancellationToken ct = default);
}
