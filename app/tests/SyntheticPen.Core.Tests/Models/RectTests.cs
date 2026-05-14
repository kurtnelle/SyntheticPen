using FluentAssertions;
using SyntheticPen.Core.Models;
using Xunit;

namespace SyntheticPen.Core.Tests.Models;

public class RectTests
{
    [Fact]
    public void IsEmpty_is_true_for_zero_or_negative_dimensions()
    {
        new Rect(0, 0, 0, 10).IsEmpty.Should().BeTrue();
        new Rect(0, 0, 10, 0).IsEmpty.Should().BeTrue();
        new Rect(0, 0, -1, 10).IsEmpty.Should().BeTrue();
        new Rect(0, 0, 10, 10).IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void Right_and_Bottom_compose_from_origin_and_size()
    {
        var r = new Rect(100, 50, 200, 30);
        r.Right.Should().Be(300);
        r.Bottom.Should().Be(80);
    }
}
