using FluentAssertions;
using SyntheticPen.Core;
using Xunit;

namespace SyntheticPen.Core.Tests;

public class VirtualScreenTests
{
    // The actual bug: FitToScreen can emit X == originX + width (the exclusive
    // upper bound). InjectSyntheticPointerInput rejects that with err 87.
    // It must be brought to the last valid pixel, not passed through.
    [Fact]
    public void Coordinate_at_exclusive_upper_bound_clamps_to_last_valid_pixel()
    {
        // Single 1920x1080 desktop at origin (0,0): valid X is 0..1919.
        var (x, y) = VirtualScreen.ClampPixel(1920, 1080, 0, 0, 1920, 1080);
        x.Should().Be(1919);
        y.Should().Be(1079);
    }

    [Fact]
    public void In_range_point_on_offset_desktop_passes_through()
    {
        // Virtual desktop origin -1920, width 5760 -> valid X: -1920..3839.
        var (x, y) = VirtualScreen.ClampPixel(3000, 600, -1920, 0, 5760, 1080);
        (x, y).Should().Be((3000, 600));
    }

    [Fact]
    public void Point_past_right_edge_of_offset_desktop_clamps()
    {
        // origin -1920, width 5760 -> valid X max = -1920+5760-1 = 3839.
        var (x, y) = VirtualScreen.ClampPixel(3924, 600, -1920, 0, 5760, 1080);
        x.Should().Be(3839);
        y.Should().Be(600);
    }

    [Fact]
    public void In_range_and_below_min_behave_correctly()
    {
        VirtualScreen.ClampPixel(500, 400, 0, 0, 1920, 1080).Should().Be((500, 400));
        VirtualScreen.ClampPixel(-10, -5, 0, 0, 1920, 1080).Should().Be((0, 0));
    }

    [Fact]
    public void Degenerate_size_does_not_throw()
    {
        VirtualScreen.ClampPixel(50, 50, 0, 0, 0, 0).Should().Be((0, 0));
    }
}
