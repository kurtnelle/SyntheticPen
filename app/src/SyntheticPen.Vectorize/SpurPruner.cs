namespace SyntheticPen.Vectorize;

/// <summary>
/// Removes skeleton "spurs" — short dead-end whiskers that thinning grows from
/// every bump on an anti-aliased ink outline. A branch is pruned when one end
/// is a free endpoint, the other terminates at a junction, and its pixel
/// length is below <c>widthFactor × (stroke radius at that junction)</c>: a
/// genuine stroke is always longer than it is locally wide, a boundary wobble
/// is not. Isolated short components (no junction — e.g. an i-dot or the
/// period in "K.") are never touched. Iterated, because clearing one spur can
/// demote a junction and expose another.
/// </summary>
public static class SpurPruner
{
    private static readonly int[] DX = { -1, 0, 1, -1, 1, -1, 0, 1 };
    private static readonly int[] DY = { -1, -1, -1, 0, 0, 1, 1, 1 };

    public static BinaryMask Prune(BinaryMask skel, float[] edt, double widthFactor, int maxIterations = 12)
    {
        int w = skel.Width, h = skel.Height;
        var img = skel.Snapshot();
        if (widthFactor <= 0) { var passthru = new BinaryMask(w, h, skel.Scale, skel.OriginX, skel.OriginY); passthru.Load(img); return passthru; }

        for (int iter = 0; iter < maxIterations; iter++)
        {
            var deg = Degrees(img, w, h);
            var toClear = new List<int>();

            for (int y = 1; y < h - 1; y++)
            {
                for (int x = 1; x < w - 1; x++)
                {
                    int idx = y * w + x;
                    if (img[idx] != 1 || deg[idx] != 1) continue; // endpoints only

                    var branch = new List<int> { idx };
                    int px = -1, py = -1, cx = x, cy = y;
                    int terminalDeg = -1, terminalIdx = -1;

                    while (true)
                    {
                        int nx = -1, ny = -1;
                        for (int k = 0; k < 8; k++)
                        {
                            int tx = cx + DX[k], ty = cy + DY[k];
                            if (tx < 0 || ty < 0 || tx >= w || ty >= h) continue;
                            if (img[ty * w + tx] != 1) continue;
                            if (tx == px && ty == py) continue;
                            nx = tx; ny = ty; break;
                        }
                        if (nx < 0) { terminalDeg = 1; break; }     // lone chain, no junction

                        int nIdx = ny * w + nx;
                        if (deg[nIdx] >= 3) { terminalDeg = deg[nIdx]; terminalIdx = nIdx; break; }

                        branch.Add(nIdx);
                        px = cx; py = cy; cx = nx; cy = ny;

                        // Runaway guard: anything this long isn't a spur.
                        if (branch.Count > w + h) { terminalDeg = 2; break; }
                    }

                    if (terminalDeg < 3 || terminalIdx < 0) continue; // not endpoint→junction

                    double radius = edt[terminalIdx];
                    double limit = Math.Max(2.0, widthFactor * radius);
                    if (branch.Count <= limit)
                        toClear.AddRange(branch); // keep the junction pixel itself
                }
            }

            if (toClear.Count == 0) break;
            foreach (var i in toClear) img[i] = 0;
        }

        var result = new BinaryMask(w, h, skel.Scale, skel.OriginX, skel.OriginY);
        result.Load(img);
        return result;
    }

    private static int[] Degrees(byte[] img, int w, int h)
    {
        var deg = new int[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (img[y * w + x] != 1) continue;
                int c = 0;
                for (int k = 0; k < 8; k++)
                {
                    int nx = x + DX[k], ny = y + DY[k];
                    if (nx >= 0 && ny >= 0 && nx < w && ny < h && img[ny * w + nx] == 1) c++;
                }
                deg[y * w + x] = c;
            }
        return deg;
    }
}
