using System.Text;
using FluentAssertions;
using SyntheticPen.Vectorize;

namespace SyntheticPen.Vectorize.Tests;

public class EuclideanDistanceTransformTests
{
    [Fact]
    public void Center_of_solid_block_holds_distance_to_nearest_background()
    {
        // 7x7, ink = inner 3x3 (cols/rows 2..4). Center (3,3) nearest bg is 2px.
        var m = new BinaryMask(7, 7, 1, 0, 0);
        for (int y = 2; y <= 4; y++)
            for (int x = 2; x <= 4; x++)
                m[x, y] = 1;

        var edt = EuclideanDistanceTransform.Compute(m);

        edt[3 * 7 + 3].Should().BeApproximately(2f, 1e-4f); // center
        edt[3 * 7 + 2].Should().BeApproximately(1f, 1e-4f); // edge of block
        edt[0 * 7 + 0].Should().Be(0f);                     // background
    }
}

public class ZhangSuenThinningTests
{
    [Fact]
    public void Thick_bar_collapses_to_a_single_pixel_line()
    {
        int w = 30, h = 15;
        var m = new BinaryMask(w, h, 1, 0, 0);
        // 5px-tall bar, rows 5..9, cols 3..26.
        for (int y = 5; y <= 9; y++)
            for (int x = 3; x <= 26; x++)
                m[x, y] = 1;

        var skel = ZhangSuenThinning.Thin(m);

        int inkBefore = 0, inkAfter = 0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (m[x, y] == 1) inkBefore++;
                if (skel[x, y] == 1) inkAfter++;
            }

        inkAfter.Should().BeLessThan(inkBefore / 3);

        // Interior columns should have exactly one skeleton pixel (1px wide).
        for (int x = 10; x <= 20; x++)
        {
            int col = 0;
            for (int y = 0; y < h; y++) col += skel[x, y];
            col.Should().Be(1);
        }
    }
}

public class SkeletonTracerTests
{
    [Fact]
    public void Single_line_traces_to_one_ordered_stroke()
    {
        int w = 25, h = 5;
        var skel = new BinaryMask(w, h, 1, 0, 0);
        for (int x = 2; x <= 20; x++) skel[x, 2] = 1;

        var strokes = SkeletonTracer.Trace(skel, minPixels: 3);

        strokes.Should().HaveCount(1);
        strokes[0].Should().HaveCountGreaterThanOrEqualTo(18);
        // Ordered along the line.
        strokes[0][0].X.Should().BeLessThan(strokes[0][^1].X);
    }

    [Fact]
    public void Plus_shape_splits_into_multiple_strokes_at_the_junction()
    {
        int w = 21, h = 21;
        var skel = new BinaryMask(w, h, 1, 0, 0);
        for (int x = 2; x <= 18; x++) skel[x, 10] = 1; // horizontal
        for (int y = 2; y <= 18; y++) skel[10, y] = 1; // vertical

        var strokes = SkeletonTracer.Trace(skel, minPixels: 2);

        // Four arms off a central junction → at least 3 distinct strokes.
        strokes.Count.Should().BeGreaterThanOrEqualTo(3);
    }
}

public class SpurPrunerTests
{
    [Fact]
    public void Short_whisker_off_a_junction_is_pruned_but_the_main_line_and_isolated_marks_survive()
    {
        int w = 40, h = 20;
        var skel = new BinaryMask(w, h, 1, 0, 0);

        // Main horizontal stroke.
        for (int x = 2; x <= 37; x++) skel[x, 10] = 1;
        // 5px dead-end whisker rising from the middle (junction near the line).
        for (int y = 5; y <= 9; y++) skel[20, y] = 1;
        // An isolated short mark (a "period") — no junction, must be kept.
        skel[5, 17] = 1; skel[6, 17] = 1; skel[7, 17] = 1;

        // Uniform local radius of 5 → prune limit = 1.6*5 = 8 px.
        var edt = new float[w * h];
        Array.Fill(edt, 5f);

        var pruned = SpurPruner.Prune(skel, edt, widthFactor: 1.6);

        // The protruding whisker is removed (down to at most the
        // junction-adjacent pixel; 8-connectivity makes that pixel part of
        // the line's neighbourhood, so we don't require it gone).
        pruned[20, 5].Should().Be(0);
        pruned[20, 6].Should().Be(0);
        pruned[20, 7].Should().Be(0);
        pruned[20, 8].Should().Be(0);
        // Main line fully intact.
        for (int x = 2; x <= 37; x++) pruned[x, 10].Should().Be(1);
        // Isolated mark preserved (no junction → not a spur).
        pruned[5, 17].Should().Be(1);
        pruned[7, 17].Should().Be(1);
    }

    [Fact]
    public void Factor_zero_is_a_passthrough()
    {
        var skel = new BinaryMask(10, 10, 1, 0, 0);
        skel[5, 5] = 1; skel[5, 6] = 1;
        var edt = new float[100];

        var outp = SpurPruner.Prune(skel, edt, widthFactor: 0);

        outp[5, 5].Should().Be(1);
        outp[5, 6].Should().Be(1);
    }
}

public class StrokeStitcherTests
{
    // 64x64 uniform-radius field; medianR = 5 -> maxGap=2.5*5=12.5, minLen=2*5=10.
    private static float[] Edt(int w, int h, float r)
    {
        var e = new float[w * h];
        Array.Fill(e, r);
        return e;
    }

    private static List<(int X, int Y)> Seg(int x0, int y0, int x1, int y1, int step = 2)
    {
        var p = new List<(int, int)>();
        int dx = Math.Sign(x1 - x0), dy = Math.Sign(y1 - y0);
        int x = x0, y = y0;
        while (true)
        {
            p.Add((x, y));
            if (x == x1 && y == y1) break;
            x += dx * step * (dx != 0 ? 1 : 0);
            y += dy * step * (dy != 0 ? 1 : 0);
            if (Math.Abs(x - x0) > Math.Abs(x1 - x0)) x = x1;
            if (Math.Abs(y - y0) > Math.Abs(y1 - y0)) y = y1;
        }
        return p;
    }

    [Fact]
    public void Collinear_fragments_within_gap_are_joined()
    {
        var a = Seg(0, 0, 10, 0);
        var b = Seg(14, 0, 30, 0); // 4px gap from a's end, same heading
        var res = StrokeStitcher.Stitch(new() { a, b }, Edt(64, 64, 5f), 64, 64,
            gapWidthFactor: 2.5, maxAngleDeg: 75, minLenWidthFactor: 2.0,
            islandGapWidthFactor: 6.0);

        res.Should().HaveCount(1);
        res[0].Should().Contain((0, 0)).And.Contain((30, 0));
    }

    [Fact]
    public void Sharp_corner_within_gap_is_not_joined()
    {
        var a = Seg(0, 0, 10, 0);   // heading +x
        var b = Seg(12, 0, 12, 16); // 2px gap but heading +y (~90deg)
        var res = StrokeStitcher.Stitch(new() { a, b }, Edt(64, 64, 5f), 64, 64,
            gapWidthFactor: 2.5, maxAngleDeg: 75, minLenWidthFactor: 0, // drop disabled
            islandGapWidthFactor: 6.0);

        res.Should().HaveCount(2); // angle gate prevents the kink
    }

    [Fact]
    public void Short_stroke_adjacent_to_ink_is_dropped()
    {
        // medianR=5, IslandGapWidthFactor=6 -> islandGap=30. The speck sits
        // 8px from the long stroke (< 30) so it's adjacent clutter, not an
        // island, and is short (< minLen 10) -> dropped.
        var longStroke = Seg(0, 0, 40, 0);
        var speck = Seg(10, 8, 13, 8);
        var res = StrokeStitcher.Stitch(new() { longStroke, speck }, Edt(256, 256, 5f), 256, 256,
            gapWidthFactor: 2.5, maxAngleDeg: 75, minLenWidthFactor: 2.0,
            islandGapWidthFactor: 6.0);

        res.Should().HaveCount(1);
        res[0].Should().Contain((40, 0));
    }

    [Fact]
    public void Isolated_island_speck_is_kept_even_though_it_is_short()
    {
        // Same short speck, but far from any other stroke (> islandGap 30):
        // a deliberate mark (period / i-dot) — must survive.
        var longStroke = Seg(0, 0, 40, 0);
        var island = Seg(200, 200, 203, 200);
        var res = StrokeStitcher.Stitch(new() { longStroke, island }, Edt(256, 256, 5f), 256, 256,
            gapWidthFactor: 2.5, maxAngleDeg: 75, minLenWidthFactor: 2.0,
            islandGapWidthFactor: 6.0);

        res.Should().HaveCount(2);
        res.Any(s => s.Contains((200, 200))).Should().BeTrue();
    }

    [Fact]
    public void Factors_zero_is_a_passthrough()
    {
        var a = Seg(0, 0, 4, 0); // tiny, would be dropped if filtering on
        var b = Seg(6, 0, 10, 0);
        var res = StrokeStitcher.Stitch(new() { a, b }, Edt(32, 32, 5f), 32, 32,
            gapWidthFactor: 0, maxAngleDeg: 75, minLenWidthFactor: 0,
            islandGapWidthFactor: 6.0);

        res.Should().HaveCount(2); // nothing merged, nothing dropped
    }
}

public class CenterlineExtractorEndToEndTests
{
    private static Stream Svg(string body, double w = 200, double h = 100)
    {
        var s = $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {w} {h}'>{body}</svg>";
        return new MemoryStream(Encoding.UTF8.GetBytes(s));
    }

    [Fact]
    public void Thick_stroked_line_yields_a_pressured_centerline_in_svg_space()
    {
        // A fat horizontal stroke from (20,50) to (180,50), 12 units wide.
        using var svg = Svg("<path d='M20 50 L180 50' stroke='black' stroke-width='12' fill='none'/>");

        var extractor = new CenterlineExtractor();
        var strokes = extractor.Extract(svg, new VectorizeOptions(TargetResolution: 800));

        strokes.Should().NotBeEmpty();
        var longest = strokes.OrderByDescending(s => s.Count).First();
        longest.Count.Should().BeGreaterThan(5);

        foreach (var p in longest)
        {
            float.IsFinite(p.X).Should().BeTrue();
            float.IsFinite(p.Y).Should().BeTrue();
            p.Pressure.Should().BeInRange(0f, 1f);
            p.Velocity.Should().BeInRange(0f, 1f);
        }

        // Centerline should sit on the ink: y≈50, x within the drawn span,
        // expressed in source SVG coordinates.
        var mid = longest[longest.Count / 2];
        mid.Y.Should().BeApproximately(50f, 6f);
        mid.X.Should().BeInRange(15f, 185f);

        // A 12-wide stroke has radius ~6; pressure should be clearly non-zero
        // along the body.
        longest.Max(p => p.Pressure).Should().BeGreaterThan(0.5f);
    }

    [Fact]
    public void Empty_drawing_throws_rather_than_returning_garbage()
    {
        using var svg = Svg("");
        var extractor = new CenterlineExtractor();
        Action act = () => extractor.Extract(svg);
        act.Should().Throw<InvalidOperationException>();
    }
}
