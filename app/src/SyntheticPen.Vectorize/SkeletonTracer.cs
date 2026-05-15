namespace SyntheticPen.Vectorize;

/// <summary>
/// Turns a 1-pixel skeleton into ordered polylines.
///
/// Pixels are classified by 8-neighbour degree (1 = endpoint, 2 = on a chain,
/// ≥3 = junction). The skeleton is first decomposed into <i>edges</i> (maximal
/// chains between nodes) plus any pure loops, then edges are <b>stitched
/// through junctions</b>: at a crossing the pen continues along whichever
/// branch best preserves its current heading (maximal direction dot-product),
/// so a cursive loop replays as one flowing stroke instead of fragmenting into
/// a piece per crossing. Strokes are emitted left-to-right then top-to-bottom
/// as a reasonable natural writing order.
/// </summary>
public static class SkeletonTracer
{
    private static readonly int[] DX = { -1, 0, 1, -1, 1, -1, 0, 1 };
    private static readonly int[] DY = { -1, -1, -1, 0, 0, 1, 1, 1 };

    // Reject a continuation if the pen would have to turn more than ~115°
    // (dot < this). Crossings in handwriting are shallow; a near-reversal is
    // almost never the same pen stroke.
    private const double MinContinuationDot = -0.45;

    private sealed class Edge
    {
        public required List<(int X, int Y)> Pixels;
        public bool Used;
        public bool IsLoop; // closed chain with no node continuation
    }

    public static List<List<(int X, int Y)>> Trace(BinaryMask skel, int minPixels)
    {
        int w = skel.Width, h = skel.Height;
        var deg = new int[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (skel[x, y] == 1)
                    deg[y * w + x] = NeighbourCount(skel, x, y);

        var edges = ExtractEdges(skel, deg, w, h);

        // node pixel-index -> incident edges (with which end touches the node)
        var nodeEdges = new Dictionary<int, List<(Edge E, bool AtStart)>>();
        void Reg(int node, Edge e, bool atStart)
        {
            if (!nodeEdges.TryGetValue(node, out var l))
                nodeEdges[node] = l = new();
            l.Add((e, atStart));
        }
        foreach (var e in edges)
        {
            if (e.IsLoop) continue;
            Reg(NodeKey(e.Pixels[0], w), e, true);
            Reg(NodeKey(e.Pixels[^1], w), e, false);
        }

        var strokes = new List<List<(int X, int Y)>>();

        // 1) Start at endpoints (degree-1 nodes) and flow through junctions.
        foreach (var e in edges)
        {
            if (e.Used || e.IsLoop) continue;
            int ka = NodeKey(e.Pixels[0], w), kb = NodeKey(e.Pixels[^1], w);
            bool aEnd = deg[ka] == 1, bEnd = deg[kb] == 1;
            if (!aEnd && !bEnd) continue;
            var startAtA = aEnd; // begin from the endpoint side
            var path = Flow(e, startAtA, nodeEdges, deg, w);
            if (path.Count >= minPixels) strokes.Add(path);
        }

        // 2) Remaining edges (junction-only nets, cycles touching junctions).
        foreach (var e in edges)
        {
            if (e.Used || e.IsLoop) continue;
            var path = Flow(e, true, nodeEdges, deg, w);
            if (path.Count >= minPixels) strokes.Add(path);
        }

        // 3) Pure loops.
        foreach (var e in edges)
        {
            if (e.Used || !e.IsLoop) continue;
            e.Used = true;
            if (e.Pixels.Count >= minPixels) strokes.Add(new List<(int, int)>(e.Pixels));
        }

        strokes.Sort((a, b) =>
        {
            int ax = MinX(a), bx = MinX(b);
            return ax != bx ? ax.CompareTo(bx) : MinY(a).CompareTo(MinY(b));
        });
        return strokes;
    }

    /// <summary>Walk a stroke starting from <paramref name="e"/>, continuing
    /// through each junction along the branch that best preserves heading.</summary>
    private static List<(int X, int Y)> Flow(
        Edge e, bool startAtA,
        Dictionary<int, List<(Edge E, bool AtStart)>> nodeEdges, int[] deg, int w)
    {
        var path = new List<(int X, int Y)>();
        var cur = e;
        bool atStart = startAtA;

        while (true)
        {
            cur.Used = true;
            var seg = atStart ? cur.Pixels : Reversed(cur.Pixels);

            // Skip the duplicated seam pixel (shared node) on continuations.
            int from = path.Count == 0 ? 0 : 1;
            for (int i = from; i < seg.Count; i++) path.Add(seg[i]);

            var endPixel = seg[^1];
            int endNode = NodeKey(endPixel, w);
            if (!nodeEdges.TryGetValue(endNode, out var incident)) break;
            if (deg[endNode] == 1) break; // reached an endpoint

            // Heading as we arrive at the junction.
            var dIn = Heading(seg, fromEnd: true);

            Edge? best = null;
            bool bestAtStart = false;
            double bestDot = double.NegativeInfinity;

            foreach (var (cand, candAtStart) in incident)
            {
                if (cand.Used || ReferenceEquals(cand, cur)) continue;
                var candSeg = candAtStart ? cand.Pixels : Reversed(cand.Pixels);
                var dOut = Heading(candSeg, fromEnd: false);
                double dot = dIn.X * dOut.X + dIn.Y * dOut.Y;
                if (dot > bestDot) { bestDot = dot; best = cand; bestAtStart = candAtStart; }
            }

            if (best is null || bestDot < MinContinuationDot) break;
            cur = best;
            atStart = bestAtStart;
        }
        return path;
    }

    /// <summary>Unit travel direction over the first/last few samples.</summary>
    private static (double X, double Y) Heading(List<(int X, int Y)> seg, bool fromEnd)
    {
        const int K = 4;
        int n = seg.Count;
        (int X, int Y) a, b;
        if (fromEnd)
        {
            b = seg[n - 1];
            a = seg[Math.Max(0, n - 1 - K)];
        }
        else
        {
            a = seg[0];
            b = seg[Math.Min(n - 1, K)];
        }
        double dx = b.X - a.X, dy = b.Y - a.Y;
        double m = Math.Sqrt(dx * dx + dy * dy);
        return m < 1e-9 ? (0, 0) : (dx / m, dy / m);
    }

    private static List<Edge> ExtractEdges(BinaryMask skel, int[] deg, int w, int h)
    {
        var edges = new List<Edge>();
        var usedStep = new HashSet<long>();
        var visited = new bool[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (skel[x, y] != 1) continue;
                if (deg[y * w + x] == 2) continue; // not a node

                for (int k = 0; k < 8; k++)
                {
                    int nx = x + DX[k], ny = y + DY[k];
                    if (!skel.InBounds(nx, ny) || skel[nx, ny] != 1) continue;
                    long step = StepKey(x, y, nx, ny, w);
                    if (usedStep.Contains(step)) continue;

                    var pix = new List<(int X, int Y)> { (x, y) };
                    int px = x, py = y, cx = nx, cy = ny;
                    usedStep.Add(StepKey(x, y, nx, ny, w));

                    while (true)
                    {
                        pix.Add((cx, cy));
                        if (deg[cy * w + cx] != 2)
                        {
                            usedStep.Add(StepKey(cx, cy, px, py, w));
                            break;
                        }
                        visited[cy * w + cx] = true;
                        int sx = -1, sy = -1;
                        for (int j = 0; j < 8; j++)
                        {
                            int tx = cx + DX[j], ty = cy + DY[j];
                            if (!skel.InBounds(tx, ty) || skel[tx, ty] != 1) continue;
                            if (tx == px && ty == py) continue;
                            sx = tx; sy = ty; break;
                        }
                        if (sx < 0) break;
                        usedStep.Add(StepKey(cx, cy, sx, sy, w));
                        px = cx; py = cy; cx = sx; cy = sy;
                    }
                    edges.Add(new Edge { Pixels = pix });
                }
            }
        }

        // Pure loops: degree-2 pixels never touched above.
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (skel[x, y] != 1 || visited[y * w + x] || deg[y * w + x] != 2) continue;
                var pix = new List<(int X, int Y)>();
                int px = -1, py = -1, cx = x, cy = y;
                while (true)
                {
                    pix.Add((cx, cy));
                    visited[cy * w + cx] = true;
                    int sx = -1, sy = -1;
                    for (int j = 0; j < 8; j++)
                    {
                        int tx = cx + DX[j], ty = cy + DY[j];
                        if (!skel.InBounds(tx, ty) || skel[tx, ty] != 1) continue;
                        if (tx == px && ty == py) continue;
                        if (visited[ty * w + tx]) continue;
                        sx = tx; sy = ty; break;
                    }
                    if (sx < 0) { pix.Add((x, y)); break; }
                    px = cx; py = cy; cx = sx; cy = sy;
                }
                edges.Add(new Edge { Pixels = pix, IsLoop = true });
            }
        }
        return edges;
    }

    private static List<(int X, int Y)> Reversed(List<(int X, int Y)> p)
    {
        var r = new List<(int X, int Y)>(p);
        r.Reverse();
        return r;
    }

    private static int NodeKey((int X, int Y) p, int w) => p.Y * w + p.X;

    private static int NeighbourCount(BinaryMask m, int x, int y)
    {
        int c = 0;
        for (int k = 0; k < 8; k++)
        {
            int nx = x + DX[k], ny = y + DY[k];
            if (m.InBounds(nx, ny) && m[nx, ny] == 1) c++;
        }
        return c;
    }

    private static long StepKey(int x1, int y1, int x2, int y2, int w)
    {
        long a = (long)y1 * w + x1, b = (long)y2 * w + x2;
        return a < b ? (a << 32) | b : (b << 32) | a;
    }

    private static int MinX(List<(int X, int Y)> p)
    {
        int m = int.MaxValue;
        foreach (var (x, _) in p) if (x < m) m = x;
        return m;
    }

    private static int MinY(List<(int X, int Y)> p)
    {
        int m = int.MaxValue;
        foreach (var (_, y) in p) if (y < m) m = y;
        return m;
    }
}
