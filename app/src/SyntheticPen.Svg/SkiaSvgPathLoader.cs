using System.Globalization;
using System.Xml;
using SkiaSharp;
using SyntheticPen.Core.Models;

namespace SyntheticPen.Svg;

public sealed class SkiaSvgPathLoader : ISvgPathLoader
{
    public Task<SvgDocument> LoadAsync(Stream svgStream, FlattenOptions opts, CancellationToken ct = default)
    {
        if (opts.Tolerance <= 0) throw new ArgumentOutOfRangeException(nameof(opts), "Tolerance must be > 0");

        return Task.Run<SvgDocument>(() =>
        {
            ct.ThrowIfCancellationRequested();

            // Buffer the stream so we can parse XML deterministically.
            using var ms = new MemoryStream();
            svgStream.CopyTo(ms);
            ms.Position = 0;

            XmlDocument xml;
            try
            {
                xml = new XmlDocument { PreserveWhitespace = false };
                xml.Load(ms);
            }
            catch (XmlException ex)
            {
                throw new SvgParseException($"SVG XML is malformed: {ex.Message}", ex, ex.LineNumber);
            }

            var root = xml.DocumentElement;
            if (root == null || !string.Equals(root.LocalName, "svg", StringComparison.OrdinalIgnoreCase))
                throw new SvgParseException("Document root is not <svg>.");

            var viewBox = ParseViewBox(root);

            var strokes = new List<Stroke>();
            CollectStrokes(root, SKMatrix.CreateIdentity(), strokes, opts.Tolerance, ct);

            // Fallback viewBox = bounding box of all points (used when SVG omits viewBox)
            if (viewBox is null)
                viewBox = StrokesBoundingBox(strokes) ?? new Rect(0, 0, 100, 100);

            return new SvgDocument(strokes, viewBox.Value);
        }, ct);
    }

    private static Rect? ParseViewBox(XmlElement root)
    {
        var attr = root.GetAttribute("viewBox");
        if (string.IsNullOrWhiteSpace(attr)) return null;
        var parts = attr.Split(new[] { ' ', ',', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4) throw new SvgParseException($"Invalid viewBox: '{attr}'");
        var v = parts.Select(p => double.Parse(p, CultureInfo.InvariantCulture)).ToArray();
        return new Rect(v[0], v[1], v[2], v[3]);
    }

    private static void CollectStrokes(XmlElement element, SKMatrix transform, List<Stroke> strokes, double tolerance, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var localTransform = ApplyTransformAttr(transform, element.GetAttribute("transform"));

        foreach (XmlNode childNode in element.ChildNodes)
        {
            if (childNode is not XmlElement child) continue;
            var name = child.LocalName.ToLowerInvariant();
            switch (name)
            {
                case "g":
                case "svg":
                    CollectStrokes(child, localTransform, strokes, tolerance, ct);
                    break;
                case "path":
                    AddPathStrokes(child.GetAttribute("d"), localTransform, strokes, tolerance);
                    break;
                case "line":
                    AddLine(child, localTransform, strokes);
                    break;
                case "polyline":
                case "polygon":
                    AddPoly(child, localTransform, strokes, closed: name == "polygon");
                    break;
            }
        }
    }

    private static SKMatrix ApplyTransformAttr(SKMatrix parent, string transformAttr)
    {
        if (string.IsNullOrWhiteSpace(transformAttr)) return parent;
        var m = parent;
        foreach (var t in TokenizeTransforms(transformAttr))
        {
            m = m.PreConcat(t);
        }
        return m;
    }

    private static IEnumerable<SKMatrix> TokenizeTransforms(string s)
    {
        int i = 0;
        while (i < s.Length)
        {
            while (i < s.Length && (char.IsWhiteSpace(s[i]) || s[i] == ',')) i++;
            int nameStart = i;
            while (i < s.Length && char.IsLetter(s[i])) i++;
            if (i == nameStart) yield break;
            string name = s.Substring(nameStart, i - nameStart);
            while (i < s.Length && s[i] != '(') i++;
            if (i >= s.Length) yield break;
            i++;
            int argsStart = i;
            while (i < s.Length && s[i] != ')') i++;
            string argsStr = s.Substring(argsStart, i - argsStart);
            if (i < s.Length) i++;
            var args = argsStr.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(a => float.Parse(a, CultureInfo.InvariantCulture))
                              .ToArray();
            switch (name)
            {
                case "translate":
                    yield return SKMatrix.CreateTranslation(args[0], args.Length > 1 ? args[1] : 0);
                    break;
                case "scale":
                    yield return SKMatrix.CreateScale(args[0], args.Length > 1 ? args[1] : args[0]);
                    break;
                case "matrix":
                    yield return new SKMatrix(args[0], args[2], args[4], args[1], args[3], args[5], 0, 0, 1);
                    break;
                case "rotate":
                    yield return SKMatrix.CreateRotationDegrees(args[0],
                        args.Length > 1 ? args[1] : 0, args.Length > 2 ? args[2] : 0);
                    break;
            }
        }
    }

    private static void AddPathStrokes(string d, SKMatrix transform, List<Stroke> strokes, double tolerance)
    {
        if (string.IsNullOrWhiteSpace(d)) return;
        SKPath? skPath;
        try { skPath = SKPath.ParseSvgPathData(d); }
        catch (Exception ex) { throw new SvgParseException($"Failed to parse path d='{d}': {ex.Message}", ex); }
        if (skPath is null) throw new SvgParseException($"Failed to parse path d='{d}'");

        var stroke = new List<PointF>();
        using var iter = skPath.CreateIterator(forceClose: false);
        var pts = new SKPoint[4];
        SKPoint subpathStart = default;
        SKPathVerb verb;

        while ((verb = iter.Next(pts)) != SKPathVerb.Done)
        {
            switch (verb)
            {
                case SKPathVerb.Move:
                    if (stroke.Count > 0)
                    {
                        strokes.Add(new Stroke(stroke.ToArray()));
                        stroke.Clear();
                    }
                    subpathStart = pts[0];
                    stroke.Add(Transform(pts[0], transform));
                    break;
                case SKPathVerb.Line:
                    stroke.Add(Transform(pts[1], transform));
                    break;
                case SKPathVerb.Quad:
                {
                    var flat = BezierFlattener.FlattenQuadratic(
                        new PointF(pts[0].X, pts[0].Y),
                        new PointF(pts[1].X, pts[1].Y),
                        new PointF(pts[2].X, pts[2].Y),
                        tolerance);
                    for (int k = 1; k < flat.Count; k++) stroke.Add(Transform(flat[k], transform));
                    break;
                }
                case SKPathVerb.Cubic:
                {
                    var flat = BezierFlattener.FlattenCubic(
                        new PointF(pts[0].X, pts[0].Y),
                        new PointF(pts[1].X, pts[1].Y),
                        new PointF(pts[2].X, pts[2].Y),
                        new PointF(pts[3].X, pts[3].Y),
                        tolerance);
                    for (int k = 1; k < flat.Count; k++) stroke.Add(Transform(flat[k], transform));
                    break;
                }
                case SKPathVerb.Conic:
                {
                    // Conic -> quads. SKPath.ConvertConicToQuads emits 2^pow2 quads, each 3 points.
                    var quads = new SKPoint[2 * 2 + 1];
                    int count = SKPath.ConvertConicToQuads(pts[0], pts[1], pts[2], iter.ConicWeight(), quads, pow2: 2);
                    for (int k = 0; k < count; k++)
                    {
                        int idx = k * 2;
                        var flat = BezierFlattener.FlattenQuadratic(
                            new PointF(quads[idx].X, quads[idx].Y),
                            new PointF(quads[idx + 1].X, quads[idx + 1].Y),
                            new PointF(quads[idx + 2].X, quads[idx + 2].Y),
                            tolerance);
                        for (int j = 1; j < flat.Count; j++) stroke.Add(Transform(flat[j], transform));
                    }
                    break;
                }
                case SKPathVerb.Close:
                {
                    // Skip zero-length close: avoid duplicating subpathStart when the last
                    // emitted point already coincides with it (SKPath may emit an implicit
                    // line back to start before the Close verb).
                    var closeTo = Transform(subpathStart, transform);
                    if (stroke.Count == 0 || !PointsEqual(stroke[^1], closeTo))
                        stroke.Add(closeTo);
                    break;
                }
            }
        }
        if (stroke.Count > 0) strokes.Add(new Stroke(stroke.ToArray()));
    }

    private static PointF Transform(SKPoint p, SKMatrix m)
    {
        var t = m.MapPoint(p);
        return new PointF(t.X, t.Y);
    }

    private static PointF Transform(PointF p, SKMatrix m)
        => Transform(new SKPoint((float)p.X, (float)p.Y), m);

    private static bool PointsEqual(PointF a, PointF b)
    {
        const double eps = 1e-6;
        return Math.Abs(a.X - b.X) < eps && Math.Abs(a.Y - b.Y) < eps;
    }

    private static void AddLine(XmlElement el, SKMatrix t, List<Stroke> strokes)
    {
        double x1 = ParseAttr(el, "x1"), y1 = ParseAttr(el, "y1");
        double x2 = ParseAttr(el, "x2"), y2 = ParseAttr(el, "y2");
        strokes.Add(new Stroke(new[]
        {
            Transform(new PointF(x1, y1), t),
            Transform(new PointF(x2, y2), t)
        }));
    }

    private static void AddPoly(XmlElement el, SKMatrix t, List<Stroke> strokes, bool closed)
    {
        var attr = el.GetAttribute("points");
        if (string.IsNullOrWhiteSpace(attr)) return;
        var nums = attr.Split(new[] { ' ', ',', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(p => double.Parse(p, CultureInfo.InvariantCulture))
                       .ToArray();
        var pts = new List<PointF>();
        for (int i = 0; i + 1 < nums.Length; i += 2)
            pts.Add(Transform(new PointF(nums[i], nums[i + 1]), t));
        if (closed && pts.Count > 0) pts.Add(pts[0]);
        if (pts.Count >= 2) strokes.Add(new Stroke(pts.ToArray()));
    }

    private static double ParseAttr(XmlElement el, string name)
        => double.TryParse(el.GetAttribute(name), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0.0;

    private static Rect? StrokesBoundingBox(IReadOnlyList<Stroke> strokes)
    {
        if (strokes.Count == 0) return null;
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        foreach (var s in strokes)
            foreach (var p in s.Points)
            {
                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
            }
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }
}
