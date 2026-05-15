namespace SyntheticPen.Vectorize;

/// <summary>
/// End-to-end centerline extraction:
/// SVG → high-res raster → binary mask → Euclidean distance transform →
/// Zhang-Suen skeleton → graph trace → Catmull-Rom resample → per-point
/// pressure (from the distance transform) and a curvature-derived velocity
/// suggestion. Output is ordered strokes of <see cref="StrokePoint"/> in
/// source-SVG coordinate space, ready to drive synthetic stylus replay.
/// This is motion-path recovery from rendered ink — not OCR.
/// </summary>
public sealed class CenterlineExtractor
{
    public IReadOnlyList<IReadOnlyList<StrokePoint>> Extract(Stream svgStream, VectorizeOptions? options = null)
    {
        var opts = options ?? new VectorizeOptions();

        var mask = SvgRasterizer.Rasterize(svgStream, opts);
        var edt = EuclideanDistanceTransform.Compute(mask);
        var skeleton = ZhangSuenThinning.Thin(mask);
        skeleton = SpurPruner.Prune(skeleton, edt, opts.SpurWidthFactor);
        var pixelStrokes = SkeletonTracer.Trace(skeleton, opts.MinStrokePixels);

        // Resample spacing is given in SVG units; convert to raster pixels.
        double spacingPx = Math.Max(1.0, opts.ResampleSpacing * mask.Scale);

        // First pass: build geometry + raw radii, tracking the global max
        // radius so pressure can be normalized document-wide (a consistent
        // "heaviest stroke = full pressure" reference).
        var rawStrokes = new List<List<(double X, double Y, double R)>>();
        double maxRadius = 1e-6;

        foreach (var px in pixelStrokes)
        {
            var poly = new List<(double X, double Y)>(px.Count);
            foreach (var (x, y) in px) poly.Add((x, y));

            var smooth = CatmullRomResampler.Resample(poly, spacingPx);
            var withR = new List<(double X, double Y, double R)>(smooth.Count);
            foreach (var (x, y) in smooth)
            {
                double r = SampleBilinear(edt, mask.Width, mask.Height, x, y);
                if (r > maxRadius) maxRadius = r;
                withR.Add((x, y, r));
            }
            if (withR.Count >= 2) rawStrokes.Add(withR);
        }

        // Second pass: map to SVG space, normalize pressure, derive velocity.
        var result = new List<IReadOnlyList<StrokePoint>>(rawStrokes.Count);
        foreach (var s in rawStrokes)
        {
            var outStroke = new List<StrokePoint>(s.Count);
            for (int i = 0; i < s.Count; i++)
            {
                var (px, py, r) = s[i];
                var (sx, sy) = mask.ToSvg(px, py);

                double pNorm = Math.Clamp(r / maxRadius, 0.0, 1.0);
                if (opts.PressureGamma != 1.0)
                    pNorm = Math.Pow(pNorm, opts.PressureGamma);

                double vel = CurvatureVelocity(s, i, mask.Scale, opts.VelocityCurvatureRef);

                outStroke.Add(new StrokePoint
                {
                    X = (float)sx,
                    Y = (float)sy,
                    Pressure = (float)pNorm,
                    Velocity = (float)vel
                });
            }
            result.Add(outStroke);
        }
        return result;
    }

    /// <summary>
    /// Normalized speed suggestion from the local radius of curvature at point
    /// <paramref name="i"/> (computed in source units). Tight curves → slow,
    /// straight runs → ~1. Mirrors the 2/3 power-law slowdown the motion
    /// planner already applies, so replay reads as natural handwriting.
    /// </summary>
    private static double CurvatureVelocity(
        List<(double X, double Y, double R)> s, int i, double scale, double refRadius)
    {
        if (i == 0 || i >= s.Count - 1) return 1.0;

        // Convert to source units so refRadius is meaningful regardless of
        // raster resolution.
        double ax = s[i - 1].X / scale, ay = s[i - 1].Y / scale;
        double bx = s[i].X / scale, by = s[i].Y / scale;
        double cx = s[i + 1].X / scale, cy = s[i + 1].Y / scale;

        double ab = Math.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay));
        double bc = Math.Sqrt((cx - bx) * (cx - bx) + (cy - by) * (cy - by));
        double ca = Math.Sqrt((ax - cx) * (ax - cx) + (ay - cy) * (ay - cy));
        double area = Math.Abs((bx - ax) * (cy - ay) - (cx - ax) * (by - ay)) * 0.5;
        if (area < 1e-9) return 1.0; // collinear → straight → full speed

        double curveR = ab * bc * ca / (4.0 * area);

        const double floor = 0.15;
        double fast = Math.Max(refRadius, 1e-6) * 8.0;
        double t = Math.Clamp(curveR / fast, 0.0, 1.0);
        return Math.Clamp(floor + (1.0 - floor) * Math.Cbrt(t), floor, 1.0);
    }

    private static double SampleBilinear(float[] edt, int w, int h, double x, double y)
    {
        if (x < 0) x = 0; else if (x > w - 1) x = w - 1;
        if (y < 0) y = 0; else if (y > h - 1) y = h - 1;
        int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
        int x1 = Math.Min(x0 + 1, w - 1), y1 = Math.Min(y0 + 1, h - 1);
        double fx = x - x0, fy = y - y0;

        double v00 = edt[y0 * w + x0];
        double v10 = edt[y0 * w + x1];
        double v01 = edt[y1 * w + x0];
        double v11 = edt[y1 * w + x1];
        double a = v00 + (v10 - v00) * fx;
        double b = v01 + (v11 - v01) * fx;
        return a + (b - a) * fy;
    }
}
