using FluentAssertions;
using SyntheticPen.Svg;
using Xunit;

namespace SyntheticPen.Svg.Tests;

public class SmokeTests
{
    [Fact]
    public void Loader_type_is_resolvable()
    {
        typeof(SkiaSvgPathLoader).Should().Implement<ISvgPathLoader>();
    }
}
