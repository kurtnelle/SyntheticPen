namespace SyntheticPen.Vectorize;

/// <summary>
/// Post-trace cleanup that makes the centerline read like a human pen path:
/// <list type="bullet">
/// <item><b>Stitch:</b> disconnected fragments whose endpoints sit within a
/// small multiple of the local stroke radius are joined into one continuous
/// stroke (greedy, closest compatible pair first), provided the join doesn't
/// kink past a max angle. A hand never lifts the pen for a sub-pen-width
/// gap.</item>
/// <item><b>Drop:</b> anything still shorter than a small multiple of the
/// stroke radius is a true micro-stroke (shorter than the pen is wide) and is
/// discarded — real handwriting has no mm-scale specks.</item>
/// </list>
/// Thresholds are tied to the median stroke radius so they scale with the art.
/// </summary>
public static class StrokeStitcher
{
    public static List<List<(int X, int Y)>> Stitch(
        List<List<(int X, int Y)>> strokes, float[] edt, int w, int h,
        double gapWidthFactor, double maxAngleDeg, double minLenWidthFactor,
        double islandGapWidthFactor)
    {
        if (strokes.Count == 0) return strokes;

        double medianR = MedianRadius(strokes, edt, w, h);
        if (medianR < 1.0) medianR = 1.0;
        double maxGap = gapWidthFactor * medianR;
        double minLen = minLenWidthFactor * medianR;
        double islandGap = islandGapWidthFactor * medianR;
        double cosLimit = Math.Cos(maxAngleDeg * Math.PI / 180.0);

        var polys = strokes.Select(s => new List<(int X, int Y)>(s)).ToList();

        if (gapWidthFactor > 0)
        {
            bool merged = true;
            while (merged && polys.Count > 1)
            {
                merged = false;
                double best = double.MaxValue;
                int bi = -1, bj = -1;
                bool biTail = false, bjHead = false;

                for (int i = 0; i < polys.Count; i++)
                {
                    for (int j = i + 1; j < polys.Count; j++)
                    {
                        // 4 endpoint combinations: i's {start|end} to j's {start|end}.
                        TryPair(polys[i], polys[j], iTail: true, jHead: true, ref best, ref bi, ref bj, ref biTail, ref bjHead, i, j, maxGap, cosLimit);
                        TryPair(polys[i], polys[j], iTail: true, jHead: false, ref best, ref bi, ref bj, ref biTail, ref bjHead, i, j, maxGap, cosLimit);
                        TryPair(polys[i], polys[j], iTail: false, jHead: true, ref best, ref bi, ref bj, ref biTail, ref bjHead, i, j, maxGap, cosLimit);
                        TryPair(polys[i], polys[j], iTail: false, jHead: false, ref best, ref bi, ref bj, ref biTail, ref bjHead, i, j, maxGap, cosLimit);
                    }
                }

                if (bi >= 0)
                {
                    var a = polys[bi];
                    var b = polys[bj];
                    // Orient so a ends at the join point and b starts at it.
                    if (!biTail) a.Reverse();
                    if (!bjHead) b.Reverse();
                    a.AddRange(b);
                    polys.RemoveAt(bj); // bj > bi, safe
                    merged = true;
                }
            }
        }

        if (minLenWidthFactor > 0)
        {
            // Drop a short stroke only if it's adjacent clutter (something else
            // sits within islandGap). A short stroke alone in its own
            // whitespace is a deliberate mark — a period, i-dot, accent — and
            // is kept regardless of length.
            var kept = new List<List<(int X, int Y)>>(polys.Count);
            for (int i = 0; i < polys.Count; i++)
            {
                if (PolyLength(polys[i]) >= minLen || IsIsland(polys, i, islandGap))
                    kept.Add(polys[i]);
            }
            polys = kept;
        }

        return polys;
    }

    /// <summary>True if no other stroke has any point within
    /// <paramref name="islandGap"/> of stroke <paramref name="idx"/>. A coarse
    /// bounding-box reject keeps this cheap; only the short drop-candidates
    /// reach here.</summary>
    private static bool IsIsland(List<List<(int X, int Y)>> polys, int idx, double islandGap)
    {
        var s = polys[idx];
        int sMinX = int.MaxValue, sMinY = int.MaxValue, sMaxX = int.MinValue, sMaxY = int.MinValue;
        foreach (var (x, y) in s)
        {
            if (x < sMinX) sMinX = x; if (x > sMaxX) sMaxX = x;
            if (y < sMinY) sMinY = y; if (y > sMaxY) sMaxY = y;
        }
        double g2 = islandGap * islandGap;

        for (int j = 0; j < polys.Count; j++)
        {
            if (j == idx) continue;
            var o = polys[j];
            // Bbox gap reject: if the expanded boxes don't overlap, skip the
            // O(points) scan for this stroke.
            int oMinX = int.MaxValue, oMinY = int.MaxValue, oMaxX = int.MinValue, oMaxY = int.MinValue;
            foreach (var (x, y) in o)
            {
                if (x < oMinX) oMinX = x; if (x > oMaxX) oMaxX = x;
                if (y < oMinY) oMinY = y; if (y > oMaxY) oMaxY = y;
            }
            double bx = AxisGap(sMinX, sMaxX, oMinX, oMaxX);
            double by = AxisGap(sMinY, sMaxY, oMinY, oMaxY);
            if (bx * bx + by * by > g2) continue;

            foreach (var ps in s)
                foreach (var po in o)
                {
                    double dx = ps.X - po.X, dy = ps.Y - po.Y;
                    if (dx * dx + dy * dy <= g2) return false; // a neighbor — not an island
                }
        }
        return true;
    }

    private static double AxisGap(int aMin, int aMax, int bMin, int bMax)
    {
        if (aMax < bMin) return bMin - aMax;
        if (bMax < aMin) return aMin - bMax;
        return 0; // overlapping on this axis
    }

    private static void TryPair(
        List<(int X, int Y)> a, List<(int X, int Y)> b,
        bool iTail, bool jHead,
        ref double best, ref int bi, ref int bj, ref bool biTail, ref bool bjHead,
        int i, int j, double maxGap, double cosLimit)
    {
        var ae = iTail ? a[^1] : a[0];
        var bs = jHead ? b[0] : b[^1];
        double gap = Dist(ae, bs);
        if (gap > maxGap || gap >= best) return;

        // dIn  = pen direction ARRIVING at a's join endpoint.
        // dOut = pen direction LEAVING b's join endpoint into b.
        // Continuous if they point roughly the same way.
        var dIn = ArriveDir(a, atTail: iTail);
        var dOut = DepartDir(b, atHead: jHead);
        double dot = dIn.X * dOut.X + dIn.Y * dOut.Y;
        if (dot < cosLimit) return;

        best = gap; bi = i; bj = j; biTail = iTail; bjHead = jHead;
    }

    private const int K = 4;

    /// <summary>Unit direction the pen is moving as it reaches the chosen
    /// endpoint of <paramref name="p"/> (tail = natural order, head = reversed).</summary>
    private static (double X, double Y) ArriveDir(List<(int X, int Y)> p, bool atTail)
    {
        int n = p.Count;
        var to = atTail ? p[^1] : p[0];
        var from = atTail ? p[Math.Max(0, n - 1 - K)] : p[Math.Min(n - 1, K)];
        return Unit(to.X - from.X, to.Y - from.Y);
    }

    /// <summary>Unit direction the pen moves departing the chosen endpoint of
    /// <paramref name="p"/> into the stroke (head = natural order, tail = reversed).</summary>
    private static (double X, double Y) DepartDir(List<(int X, int Y)> p, bool atHead)
    {
        int n = p.Count;
        var from = atHead ? p[0] : p[^1];
        var to = atHead ? p[Math.Min(n - 1, K)] : p[Math.Max(0, n - 1 - K)];
        return Unit(to.X - from.X, to.Y - from.Y);
    }

    private static (double X, double Y) Unit(double dx, double dy)
    {
        double m = Math.Sqrt(dx * dx + dy * dy);
        return m < 1e-9 ? (0, 0) : (dx / m, dy / m);
    }

    private static double MedianRadius(List<List<(int X, int Y)>> strokes, float[] edt, int w, int h)
    {
        var rs = new List<float>();
        foreach (var s in strokes)
            foreach (var (x, y) in s)
                if (x >= 0 && y >= 0 && x < w && y < h)
                    rs.Add(edt[y * w + x]);
        if (rs.Count == 0) return 1.0;
        rs.Sort();
        return rs[rs.Count / 2];
    }

    private static double PolyLength(List<(int X, int Y)> p)
    {
        double len = 0;
        for (int i = 1; i < p.Count; i++) len += Dist(p[i - 1], p[i]);
        return len;
    }

    private static double Dist((int X, int Y) a, (int X, int Y) b)
    {
        double dx = a.X - b.X, dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
