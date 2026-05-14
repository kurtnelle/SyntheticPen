using FluentAssertions;
using SyntheticPen.Core.Models;
using SyntheticPen.Svg;
using Xunit;

namespace SyntheticPen.Svg.Tests;

public class SkiaSvgPathLoaderTests
{
    private static Stream Open(string name)
        => File.OpenRead(Path.Combine(AppContext.BaseDirectory, "fixtures", name));

    private readonly ISvgPathLoader _loader = new SkiaSvgPathLoader();
    private readonly FlattenOptions _opts = new(Tolerance: 0.25);

    [Fact]
    public async Task Straight_line_yields_one_stroke_with_two_points()
    {
        await using var s = Open("straight_line.svg");
        var doc = await _loader.LoadAsync(s, _opts);
        doc.Strokes.Should().HaveCount(1);
        doc.Strokes[0].Points.Should().HaveCount(2);
        doc.Strokes[0].Points[0].Should().Be(new PointF(10, 50));
        doc.Strokes[0].Points[1].Should().Be(new PointF(90, 50));
        doc.SourceViewBox.Should().Be(new Rect(0, 0, 100, 100));
    }

    [Fact]
    public async Task Square_yields_one_stroke_with_five_points()
    {
        await using var s = Open("square.svg");
        var doc = await _loader.LoadAsync(s, _opts);
        doc.Strokes.Should().HaveCount(1);
        doc.Strokes[0].Points.Should().HaveCount(5);     // M, L, L, L, Z back to start
        doc.Strokes[0].Points[0].Should().Be(new PointF(10, 10));
        doc.Strokes[0].Points[^1].Should().Be(new PointF(10, 10));
    }

    [Fact]
    public async Task Cursive_signature_yields_two_strokes()
    {
        await using var s = Open("cursive_signature.svg");
        var doc = await _loader.LoadAsync(s, _opts);
        doc.Strokes.Should().HaveCount(2);
        doc.Strokes[0].Points.Count.Should().BeGreaterThan(5);   // curve was flattened
    }

    [Fact]
    public async Task ViewBox_with_offset_is_reported()
    {
        await using var s = Open("viewbox_offset.svg");
        var doc = await _loader.LoadAsync(s, _opts);
        doc.SourceViewBox.Should().Be(new Rect(50, 25, 100, 50));
    }

    [Fact]
    public async Task Empty_svg_yields_zero_strokes()
    {
        await using var s = Open("empty.svg");
        var doc = await _loader.LoadAsync(s, _opts);
        doc.Strokes.Should().BeEmpty();
    }

    [Fact]
    public async Task Malformed_svg_throws_SvgParseException()
    {
        await using var s = Open("malformed.svg");
        var act = () => _loader.LoadAsync(s, _opts);
        await act.Should().ThrowAsync<SvgParseException>();
    }

    [Fact]
    public async Task Invalid_tolerance_throws()
    {
        await using var s = Open("straight_line.svg");
        var act = () => _loader.LoadAsync(s, new FlattenOptions(Tolerance: 0));
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
