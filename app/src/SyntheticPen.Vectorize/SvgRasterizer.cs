using SkiaSharp;
using SyntheticPen.Svg;

namespace SyntheticPen.Vectorize;

/// <summary>
/// Renders an SVG's visible ink into a high-resolution anti-aliased raster and
/// thresholds it into a <see cref="BinaryMask"/>. Geometry comes from
/// <see cref="SkiaSvgPathLoader.BuildFillGeometry"/> rather than Svg.Skia's
/// renderer, because that path honours the comma-separated font-family
/// fallback list (Svg.Skia does not, which silently substituted a sans-serif
/// for cursive signatures). Anti-aliasing is preserved through rendering and
/// only collapsed at the final threshold.
/// </summary>
public static class SvgRasterizer
{
    public static BinaryMask Rasterize(Stream svgStream, VectorizeOptions opts)
    {
        var (path, viewBox) = SkiaSvgPathLoader.BuildFillGeometry(svgStream);
        using (path)
        {
            if (path.IsEmpty || viewBox.W <= 0 || viewBox.H <= 0)
                throw new InvalidOperationException("SVG has no renderable ink.");

            double longSide = Math.Max(viewBox.W, viewBox.H);
            double scale = opts.TargetResolution / longSide;

            // Cap to ~64 MP so a huge viewBox can't allocate an enormous bitmap.
            const long maxPixels = 64_000_000;
            while ((long)Math.Ceiling(viewBox.W * scale) * (long)Math.Ceiling(viewBox.H * scale) > maxPixels)
                scale *= 0.8;

            int margin = 2;
            int w = (int)Math.Ceiling(viewBox.W * scale) + margin * 2;
            int h = (int)Math.Ceiling(viewBox.H * scale) + margin * 2;

            using var bmp = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Premul);
            using (var canvas = new SKCanvas(bmp))
            {
                canvas.Clear(SKColors.White);
                canvas.Translate(margin, margin);
                canvas.Scale((float)scale);
                canvas.Translate(-(float)viewBox.X, -(float)viewBox.Y);
                using var paint = new SKPaint
                {
                    IsAntialias = true,
                    Color = SKColors.Black,
                    Style = SKPaintStyle.Fill
                };
                canvas.DrawPath(path, paint);
            }

            double originX = viewBox.X - margin / scale;
            double originY = viewBox.Y - margin / scale;
            var mask = new BinaryMask(w, h, scale, originX, originY);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var c = bmp.GetPixel(x, y);
                    int lum = (c.Red * 299 + c.Green * 587 + c.Blue * 114) / 1000;
                    mask[x, y] = (byte)(lum < opts.InkThreshold ? 1 : 0);
                }
            }
            return mask;
        }
    }
}
