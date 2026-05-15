namespace SyntheticPen.Vectorize;

/// <summary>
/// Exact Euclidean distance transform (Felzenszwalb &amp; Huttenlocher, 2004):
/// a separable lower-envelope-of-parabolas pass over columns then rows, O(n).
/// For every ink pixel it yields the distance (in raster pixels) to the
/// nearest background pixel — i.e. the local stroke radius, which the pipeline
/// reuses as a pressure proxy.
/// </summary>
public static class EuclideanDistanceTransform
{
    /// <summary>Distances in raster pixels. Background pixels are 0; ink
    /// pixels hold the radius to the nearest background pixel.</summary>
    public static float[] Compute(BinaryMask mask)
    {
        int w = mask.Width, h = mask.Height;
        const float INF = 1e20f;

        // f = 0 at background (the "sites"), +inf at ink.
        var grid = new float[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                grid[y * w + x] = mask[x, y] == 0 ? 0f : INF;

        // Pass 1: along columns.
        var col = new float[h];
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++) col[y] = grid[y * w + x];
            var d = Edt1D(col);
            for (int y = 0; y < h; y++) grid[y * w + x] = d[y];
        }

        // Pass 2: along rows. Result is squared distance.
        var row = new float[w];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++) row[x] = grid[y * w + x];
            var d = Edt1D(row);
            for (int x = 0; x < w; x++) grid[y * w + x] = d[x];
        }

        var outp = new float[w * h];
        for (int i = 0; i < outp.Length; i++)
            outp[i] = MathF.Sqrt(grid[i]);
        return outp;
    }

    /// <summary>1D squared-distance transform of a sampled function (lower
    /// envelope of parabolas).</summary>
    private static float[] Edt1D(float[] f)
    {
        int n = f.Length;
        var d = new float[n];
        var v = new int[n];      // locations of parabolas in the lower envelope
        var z = new float[n + 1]; // boundaries between parabolas
        int k = 0;
        v[0] = 0;
        z[0] = float.NegativeInfinity;
        z[1] = float.PositiveInfinity;

        for (int q = 1; q < n; q++)
        {
            float s;
            while (true)
            {
                int vk = v[k];
                s = ((f[q] + q * q) - (f[vk] + (float)vk * vk)) / (2f * q - 2f * vk);
                if (s <= z[k]) k--;
                else break;
            }
            k++;
            v[k] = q;
            z[k] = s;
            z[k + 1] = float.PositiveInfinity;
        }

        k = 0;
        for (int q = 0; q < n; q++)
        {
            while (z[k + 1] < q) k++;
            int vk = v[k];
            float dx = q - vk;
            d[q] = dx * dx + f[vk];
        }
        return d;
    }
}
