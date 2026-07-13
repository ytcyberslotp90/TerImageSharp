using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace TerImageSharp;
public static class MedianCutQuantizer
{
    public static List<Rgb24> BuildPalette(Image<Rgba32> image, int maxColors = 256)
    {
        if (maxColors is < 2 or > 256)
            throw new ArgumentOutOfRangeException(nameof(maxColors), "Palette size must be between 2 and 256.");

        var samples = new List<(byte R, byte G, byte B)>(image.Width * image.Height);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                foreach (var p in row)
                    samples.Add((p.R, p.G, p.B));
            }
        });

        if (samples.Count == 0)
            return new List<Rgb24> { new(0, 0, 0) };

        var buckets = new List<List<(byte R, byte G, byte B)>> { samples };

        while (buckets.Count < maxColors)
        {
            var splitIndex = FindLargestBucket(buckets);
            if (splitIndex < 0) break;

            var bucket = buckets[splitIndex];
            var (channel, _) = WidestChannel(bucket);

            bucket.Sort((a, b) => channel switch
            {
                0 => a.R.CompareTo(b.R),
                1 => a.G.CompareTo(b.G),
                _ => a.B.CompareTo(b.B),
            });

            var mid = bucket.Count / 2;
            var lower = bucket.GetRange(0, mid);
            var upper = bucket.GetRange(mid, bucket.Count - mid);

            buckets[splitIndex] = lower;
            buckets.Add(upper);
        }

        var palette = new List<Rgb24>(buckets.Count);
        foreach (var bucket in buckets)
        {
            if (bucket.Count == 0) continue;
            long r = 0, g = 0, b = 0;
            foreach (var c in bucket) { r += c.R; g += c.G; b += c.B; }
            palette.Add(new Rgb24(
                (byte)(r / bucket.Count),
                (byte)(g / bucket.Count),
                (byte)(b / bucket.Count)));
        }
        return palette;
    }

    public static int NearestIndex(List<Rgb24> palette, int r, int g, int b)
    {
        var bestIndex = 0;
        var bestDist = int.MaxValue;
        for (var i = 0; i < palette.Count; i++)
        {
            var c = palette[i];
            var dr = c.R - r;
            var dg = c.G - g;
            var db = c.B - b;
            var dist = dr * dr + dg * dg + db * db;
            if (dist < bestDist)
            {
                bestDist = dist;
                bestIndex = i;
                if (dist == 0) break;
            }
        }
        return bestIndex;
    }

    private static int FindLargestBucket(List<List<(byte R, byte G, byte B)>> buckets)
    {
        var bestIndex = -1;
        var bestRange = 0;
        for (var i = 0; i < buckets.Count; i++)
        {
            if (buckets[i].Count < 2) continue;
            var (_, range) = WidestChannel(buckets[i]);
            if (range > bestRange)
            {
                bestRange = range;
                bestIndex = i;
            }
        }
        return bestIndex;
    }

    private static (int Channel, int Range) WidestChannel(List<(byte R, byte G, byte B)> bucket)
    {
        byte minR = 255, maxR = 0, minG = 255, maxG = 0, minB = 255, maxB = 0;
        foreach (var (r, g, b) in bucket)
        {
            if (r < minR) minR = r;
            if (r > maxR) maxR = r;
            if (g < minG) minG = g;
            if (g > maxG) maxG = g;
            if (b < minB) minB = b;
            if (b > maxB) maxB = b;
        }
        var rRange = maxR - minR;
        var gRange = maxG - minG;
        var bRange = maxB - minB;

        if (rRange >= gRange && rRange >= bRange) return (0, rRange);
        if (gRange >= bRange) return (1, gRange);
        return (2, bRange);
    }
}