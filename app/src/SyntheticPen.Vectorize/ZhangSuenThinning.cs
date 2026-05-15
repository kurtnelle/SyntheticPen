namespace SyntheticPen.Vectorize;

/// <summary>
/// Zhang-Suen iterative thinning. Two sub-iterations per pass strip boundary
/// pixels that aren't required for connectivity until only a 1-pixel-wide
/// skeleton (medial line) remains. Operates on a copy; the input mask is left
/// intact for the distance-transform stage.
/// </summary>
public static class ZhangSuenThinning
{
    public static BinaryMask Thin(BinaryMask source)
    {
        int w = source.Width, h = source.Height;
        var img = source.Snapshot(); // 1 = ink

        var toClear = new List<int>();
        bool changed = true;

        while (changed)
        {
            changed = false;

            for (int step = 0; step < 2; step++)
            {
                toClear.Clear();
                // Skip the 1px border so the 8-neighborhood is always valid.
                for (int y = 1; y < h - 1; y++)
                {
                    for (int x = 1; x < w - 1; x++)
                    {
                        if (img[y * w + x] == 0) continue;

                        // P2 P3 P4 / P9 .  P5 / P8 P7 P6  (clockwise from north)
                        int p2 = img[(y - 1) * w + x];
                        int p3 = img[(y - 1) * w + x + 1];
                        int p4 = img[y * w + x + 1];
                        int p5 = img[(y + 1) * w + x + 1];
                        int p6 = img[(y + 1) * w + x];
                        int p7 = img[(y + 1) * w + x - 1];
                        int p8 = img[y * w + x - 1];
                        int p9 = img[(y - 1) * w + x - 1];

                        int bsum = p2 + p3 + p4 + p5 + p6 + p7 + p8 + p9;
                        if (bsum < 2 || bsum > 6) continue;

                        int a = Transitions(p2, p3, p4, p5, p6, p7, p8, p9);
                        if (a != 1) continue;

                        if (step == 0)
                        {
                            if (p2 * p4 * p6 != 0) continue;
                            if (p4 * p6 * p8 != 0) continue;
                        }
                        else
                        {
                            if (p2 * p4 * p8 != 0) continue;
                            if (p2 * p6 * p8 != 0) continue;
                        }

                        toClear.Add(y * w + x);
                    }
                }

                if (toClear.Count > 0)
                {
                    changed = true;
                    foreach (var idx in toClear) img[idx] = 0;
                }
            }
        }

        var result = new BinaryMask(w, h, source.Scale, source.OriginX, source.OriginY);
        result.Load(img);
        return result;
    }

    /// <summary>Count 0→1 transitions in the ordered ring P2..P9,P2.</summary>
    private static int Transitions(int p2, int p3, int p4, int p5, int p6, int p7, int p8, int p9)
    {
        int c = 0;
        if (p2 == 0 && p3 == 1) c++;
        if (p3 == 0 && p4 == 1) c++;
        if (p4 == 0 && p5 == 1) c++;
        if (p5 == 0 && p6 == 1) c++;
        if (p6 == 0 && p7 == 1) c++;
        if (p7 == 0 && p8 == 1) c++;
        if (p8 == 0 && p9 == 1) c++;
        if (p9 == 0 && p2 == 1) c++;
        return c;
    }
}
