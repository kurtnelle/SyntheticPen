using SkiaSharp;

namespace SyntheticPen.Vectorize;

/// <summary>
/// Diagnostic overlay: renders the rasterized ink (grey), the Zhang-Suen
/// skeleton (faint red) and the final resampled centerline strokes (per-stroke
/// hue, stroke width modulated by extracted pressure) into a single PNG so
/// extraction quality can be eyeballed. Not used by the runtime path.
/// </summary>
public static class DebugVisualizer
{
    public static void RenderOverlayPng(string svgPath, string outPngPath, VectorizeOptions? options = null)
    {
        var opts = options ?? new VectorizeOptions();

        BinaryMask mask;
        using (var fs = File.OpenRead(svgPath))
            mask = SvgRasterizer.Rasterize(fs, opts);

        var skeleton = ZhangSuenThinning.Thin(mask);

        IReadOnlyList<IReadOnlyList<StrokePoint>> strokes;
        using (var fs = File.OpenRead(svgPath))
            strokes = new CenterlineExtractor().Extract(fs, opts);

        int w = mask.Width, h = mask.Height;
        using var bmp = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.White);

        // Ink (light grey).
        using (var inkPaint = new SKPaint { Color = new SKColor(0xDD, 0xDD, 0xDD) })
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (mask[x, y] == 1)
                        bmp.SetPixel(x, y, inkPaint.Color);

        // Skeleton (faint red).
        var skelColor = new SKColor(0xFF, 0x99, 0x99);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (skeleton[x, y] == 1)
                    bmp.SetPixel(x, y, skelColor);

        // Centerline strokes: convert SVG coords back to raster space.
        var palette = new[]
        {
            new SKColor(0x1F,0x77,0xB4), new SKColor(0xFF,0x7F,0x0E),
            new SKColor(0x2C,0xA0,0x2C), new SKColor(0xD6,0x27,0x28),
            new SKColor(0x94,0x67,0xBD), new SKColor(0x8C,0x56,0x4B),
        };

        for (int si = 0; si < strokes.Count; si++)
        {
            var stroke = strokes[si];
            var col = palette[si % palette.Length];
            for (int i = 1; i < stroke.Count; i++)
            {
                var a = stroke[i - 1];
                var b = stroke[i];
                using var p = new SKPaint
                {
                    Color = col,
                    IsAntialias = true,
                    StrokeCap = SKStrokeCap.Round,
                    StrokeWidth = 1f + 4f * b.Pressure,
                };
                canvas.DrawLine(
                    ToPx(a.X, mask.OriginX, mask.Scale), ToPx(a.Y, mask.OriginY, mask.Scale),
                    ToPx(b.X, mask.OriginX, mask.Scale), ToPx(b.Y, mask.OriginY, mask.Scale), p);
            }
        }

        using var img = SKImage.FromBitmap(bmp);
        using var data = img.Encode(SKEncodedImageFormat.Png, 90);
        using var outFs = File.Create(outPngPath);
        data.SaveTo(outFs);
    }

    private static float ToPx(double svg, double origin, double scale)
        => (float)((svg - origin) * scale);
}
