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

            var declared = ParseViewBox(root);

            var strokes = new List<Stroke>();
            CollectStrokes(root, SKMatrix.CreateIdentity(), strokes, opts.Tolerance, ct);

            // Source viewBox for replay = union of declared viewBox and actual stroke
            // bounds. This preserves intentional padding when the SVG author set a
            // viewBox, but still includes content that extends past it (e.g. <text>
            // glyphs whose advance/ascent exceed the declared crop region — common
            // when authoring tools emit a viewBox tighter than the typographic box).
            var content = StrokesBoundingBox(strokes);
            Rect viewBox = (declared, content) switch
            {
                ({ } d, { } c) => UnionRect(d, c),
                ({ } d, null)  => d,
                (null, { } c)  => c,
                _              => new Rect(0, 0, 100, 100)
            };

            return new SvgDocument(strokes, viewBox);
        }, ct);
    }

    /// <summary>
    /// Build the SVG's <b>visible ink</b> as a single fillable <see cref="SKPath"/>
    /// in user space, plus the effective viewBox. Filled shapes/glyphs contribute
    /// their region; stroked shapes are converted to their stroke outline (so the
    /// rasterizer sees true ink width). Reuses the same font resolution as the
    /// stroke loader, so the comma-separated font-family fallback list is honoured
    /// — Svg.Skia's renderer does not, which is why cursive signatures fell back
    /// to a default sans-serif. Used by the centerline (vectorize) pipeline.
    /// </summary>
    public static (SKPath Path, Rect ViewBox) BuildFillGeometry(Stream svgStream)
    {
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

        var declared = ParseViewBox(root);
        var combined = new SKPath { FillType = SKPathFillType.Winding };
        CollectFillPaths(root, SKMatrix.CreateIdentity(), combined);

        Rect viewBox;
        if (!combined.IsEmpty)
        {
            var b = combined.TightBounds;
            var contentRect = new Rect(b.Left, b.Top, b.Width, b.Height);
            viewBox = declared is { } d ? UnionRect(d, contentRect) : contentRect;
        }
        else
        {
            viewBox = declared ?? new Rect(0, 0, 100, 100);
        }
        return (combined, viewBox);
    }

    private static void CollectFillPaths(XmlElement element, SKMatrix transform, SKPath combined)
    {
        var localTransform = ApplyTransformAttr(transform, element.GetAttribute("transform"));

        foreach (XmlNode childNode in element.ChildNodes)
        {
            if (childNode is not XmlElement child) continue;
            switch (child.LocalName.ToLowerInvariant())
            {
                case "g":
                case "svg":
                    CollectFillPaths(child, localTransform, combined);
                    break;
                case "path":
                {
                    var p = SKPath.ParseSvgPathData(child.GetAttribute("d"));
                    if (p is not null) AppendInk(child, p, localTransform, combined);
                    break;
                }
                case "polygon":
                case "polyline":
                {
                    var p = BuildPolyPath(child, closed: child.LocalName.Equals("polygon", StringComparison.OrdinalIgnoreCase));
                    if (p is not null) AppendInk(child, p, localTransform, combined);
                    break;
                }
                case "line":
                {
                    var p = new SKPath();
                    p.MoveTo((float)ParseAttr(child, "x1"), (float)ParseAttr(child, "y1"));
                    p.LineTo((float)ParseAttr(child, "x2"), (float)ParseAttr(child, "y2"));
                    AppendInk(child, p, localTransform, combined);
                    break;
                }
                case "text":
                {
                    var p = BuildTextPath(child);
                    if (p is not null) AppendInk(child, p, localTransform, combined);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Add an element's visible region to <paramref name="combined"/>: the fill
    /// area if it has a fill, plus the stroke outline (expanded by stroke-width)
    /// if it is stroked. SVG defaults apply — fill is black unless explicitly
    /// "none", stroke is absent unless set.
    /// </summary>
    private static void AppendInk(XmlElement el, SKPath path, SKMatrix transform, SKPath combined)
    {
        using (path)
        {
            var styleAttr = el.GetAttribute("style");
            string? fill = GetStyleValue(styleAttr, "fill") ?? NullIfEmpty(el.GetAttribute("fill"));
            string? stroke = GetStyleValue(styleAttr, "stroke") ?? NullIfEmpty(el.GetAttribute("stroke"));
            bool hasFill = !string.Equals(fill, "none", StringComparison.OrdinalIgnoreCase)
                           && !string.Equals(fill, "transparent", StringComparison.OrdinalIgnoreCase);
            bool hasStroke = stroke is not null
                             && !string.Equals(stroke, "none", StringComparison.OrdinalIgnoreCase);

            if (hasFill)
            {
                using var f = new SKPath(path);
                f.Transform(transform);
                combined.AddPath(f);
            }

            if (hasStroke)
            {
                var widthStr = GetStyleValue(styleAttr, "stroke-width") ?? NullIfEmpty(el.GetAttribute("stroke-width"));
                float w = widthStr is null ? 1f : (float)ParseLength(widthStr);
                if (w <= 0) w = 1f;

                using var strokePaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = w,
                    StrokeCap = SKStrokeCap.Round,
                    StrokeJoin = SKStrokeJoin.Round
                };
                using var outline = new SKPath();
                if (strokePaint.GetFillPath(path, outline))
                {
                    outline.Transform(transform);
                    combined.AddPath(outline);
                }
            }
        }
    }

    private static SKPath? BuildPolyPath(XmlElement el, bool closed)
    {
        var attr = el.GetAttribute("points");
        if (string.IsNullOrWhiteSpace(attr)) return null;
        var nums = attr.Split(new[] { ' ', ',', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(p => float.Parse(p, CultureInfo.InvariantCulture)).ToArray();
        if (nums.Length < 4) return null;
        var path = new SKPath();
        path.MoveTo(nums[0], nums[1]);
        for (int i = 2; i + 1 < nums.Length; i += 2) path.LineTo(nums[i], nums[i + 1]);
        if (closed) path.Close();
        return path;
    }

    /// <summary>Glyph-outline path for a &lt;text&gt; element, positioned at its
    /// x/y baseline, using the same font resolution as the stroke loader.</summary>
    private static SKPath? BuildTextPath(XmlElement el)
    {
        var text = ExtractText(el);
        if (string.IsNullOrEmpty(text)) return null;

        double x = ParseAttr(el, "x");
        double y = ParseAttr(el, "y");
        var styleAttr = el.GetAttribute("style");
        var familyList = GetStyleValue(styleAttr, "font-family") ?? el.GetAttribute("font-family");
        var sizeStr = GetStyleValue(styleAttr, "font-size") ?? el.GetAttribute("font-size");
        float fontSize = ParseFontSize(sizeStr, defaultSize: 16f);

        using var typeface = ResolveTypeface(familyList) ?? SKTypeface.FromFamilyName(null);
        if (typeface is null) return null;
        using var font = new SKFont(typeface, fontSize);

        var glyphs = new ushort[text.Length];
        font.GetGlyphs(text, glyphs);
        var widths = new float[glyphs.Length];
        font.GetGlyphWidths(glyphs.AsSpan(), widths, Span<SKRect>.Empty, null);

        var path = new SKPath();
        float penX = (float)x, penY = (float)y;
        for (int i = 0; i < glyphs.Length; i++)
        {
            using var gp = font.GetGlyphPath(glyphs[i]);
            if (gp is not null && !gp.IsEmpty)
                path.AddPath(gp, penX, penY, SKPathAddMode.Append);
            penX += widths[i];
        }
        if (path.IsEmpty) { path.Dispose(); return null; }
        return path;
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

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
                case "text":
                    AddTextStrokes(child, localTransform, strokes, tolerance);
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

        AddSkPathStrokes(skPath, transform, strokes, tolerance);
    }

    private static void AddSkPathStrokes(SKPath skPath, SKMatrix transform, List<Stroke> strokes, double tolerance)
    {
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

    private static void AddTextStrokes(XmlElement el, SKMatrix transform, List<Stroke> strokes, double tolerance)
    {
        var text = ExtractText(el);
        if (string.IsNullOrEmpty(text)) return;

        double x = ParseAttr(el, "x");
        double y = ParseAttr(el, "y");

        var styleAttr = el.GetAttribute("style");
        var familyList = GetStyleValue(styleAttr, "font-family") ?? el.GetAttribute("font-family");
        var sizeStr = GetStyleValue(styleAttr, "font-size") ?? el.GetAttribute("font-size");
        float fontSize = ParseFontSize(sizeStr, defaultSize: 16f);

        using var typeface = ResolveTypeface(familyList) ?? SKTypeface.FromFamilyName(null);
        if (typeface is null) return;
        using var font = new SKFont(typeface, fontSize);

        var glyphs = new ushort[text.Length];
        font.GetGlyphs(text, glyphs);
        int glyphCount = glyphs.Length;
        var widths = new float[glyphCount];
        font.GetGlyphWidths(glyphs.AsSpan(), widths, Span<SKRect>.Empty, null);

        using var combined = new SKPath();
        float penX = (float)x;
        float penY = (float)y;
        for (int i = 0; i < glyphCount; i++)
        {
            using var gp = font.GetGlyphPath(glyphs[i]);
            if (gp is not null && !gp.IsEmpty)
                combined.AddPath(gp, penX, penY, SKPathAddMode.Append);
            penX += widths[i];
        }

        if (!combined.IsEmpty)
            AddSkPathStrokes(combined, transform, strokes, tolerance);
    }

    /// <summary>Concatenate inner text including text inside any <tspan> children.</summary>
    private static string ExtractText(XmlElement el)
    {
        var sb = new System.Text.StringBuilder();
        foreach (XmlNode n in el.ChildNodes)
        {
            if (n is XmlText t) sb.Append(t.Value);
            else if (n is XmlElement e && e.LocalName.Equals("tspan", StringComparison.OrdinalIgnoreCase))
                sb.Append(ExtractText(e));
        }
        return sb.ToString();
    }

    private static string? GetStyleValue(string style, string key)
    {
        if (string.IsNullOrWhiteSpace(style)) return null;
        foreach (var part in style.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            int colon = part.IndexOf(':');
            if (colon < 0) continue;
            var k = part.Substring(0, colon).Trim();
            if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
                return part.Substring(colon + 1).Trim();
        }
        return null;
    }

    /// <summary>Walk a comma-separated font-family list and return the first installed match.
    /// <see cref="SKTypeface.FromFamilyName"/> silently returns the default font on a miss,
    /// so we verify the returned typeface's actual FamilyName before accepting it.
    /// Generic CSS keywords (cursive/serif/sans-serif/monospace) are skipped.</summary>
    private static SKTypeface? ResolveTypeface(string? familyList)
    {
        if (string.IsNullOrWhiteSpace(familyList)) return null;
        foreach (var raw in familyList.Split(','))
        {
            var name = raw.Trim().Trim('\'', '"').Trim();
            if (string.IsNullOrEmpty(name)) continue;
            if (name.Equals("cursive", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("serif", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("sans-serif", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("monospace", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("fantasy", StringComparison.OrdinalIgnoreCase))
                continue;
            var tf = SKTypeface.FromFamilyName(name);
            if (tf is not null && string.Equals(tf.FamilyName, name, StringComparison.OrdinalIgnoreCase))
                return tf;
            tf?.Dispose();
        }
        return null;
    }

    /// <summary>Parse '600px' / '12pt' / '14' → float pixels. Treats unitless as px.</summary>
    private static float ParseFontSize(string? s, float defaultSize)
    {
        if (string.IsNullOrWhiteSpace(s)) return defaultSize;
        s = s.Trim();
        float mul = 1f;
        if (s.EndsWith("px", StringComparison.OrdinalIgnoreCase)) s = s[..^2];
        else if (s.EndsWith("pt", StringComparison.OrdinalIgnoreCase)) { s = s[..^2]; mul = 96f / 72f; }
        else if (s.EndsWith("em", StringComparison.OrdinalIgnoreCase)) { s = s[..^2]; mul = 16f; }
        return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
            ? v * mul
            : defaultSize;
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
        => ParseLength(el.GetAttribute(name));

    /// <summary>Parse an SVG length attribute. Accepts unitless numbers and the
    /// common CSS units (px, pt, pc, mm, cm, in, em). Unknown/empty → 0.</summary>
    private static double ParseLength(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0.0;
        s = s.Trim();
        double mul = 1.0;
        // strip trailing unit if present
        if (s.Length >= 2 && char.IsLetter(s[^1]) && char.IsLetter(s[^2]))
        {
            var unit = s[^2..].ToLowerInvariant();
            s = s[..^2];
            mul = unit switch
            {
                "px" => 1.0,
                "pt" => 96.0 / 72.0,
                "pc" => 16.0,        // 1 pica = 12pt
                "mm" => 96.0 / 25.4,
                "cm" => 96.0 / 2.54,
                "in" => 96.0,
                "em" => 16.0,        // assume root font-size of 16px; rare for coords
                _ => 1.0
            };
        }
        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v * mul : 0.0;
    }

    private static Rect UnionRect(Rect a, Rect b)
    {
        double minX = Math.Min(a.X, b.X);
        double minY = Math.Min(a.Y, b.Y);
        double maxX = Math.Max(a.X + a.W, b.X + b.W);
        double maxY = Math.Max(a.Y + a.H, b.Y + b.H);
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

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
