using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace TerImageSharp;
public static class FloydSteinbergDitherer
{
    public static byte[] Dither(Image<Rgba32> image, List<Rgb24> palette, bool enabled = true)
    {
        var width = image.Width;
        var height = image.Height;
        var indices = new byte[width * height];

        if (!enabled)
        {
            image.ProcessPixelRows(accessor =>
            {
                for (var y = 0; y < height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    var rowOffset = y * width;
                    for (var x = 0; x < width; x++)
                    {
                        var p = row[x];
                        indices[rowOffset + x] = (byte)MedianCutQuantizer.NearestIndex(palette, p.R, p.G, p.B);
                    }
                }
            });
            return indices;
        }

        var r = new float[width * height];
        var g = new float[width * height];
        var b = new float[width * height];

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < height; y++)
            {
                var row = accessor.GetRowSpan(y);
                var rowOffset = y * width;
                for (var x = 0; x < width; x++)
                {
                    var p = row[x];
                    r[rowOffset + x] = p.R;
                    g[rowOffset + x] = p.G;
                    b[rowOffset + x] = p.B;
                }
            }
        });

        for (var y = 0; y < height; y++)
        {
            var rowOffset = y * width;
            for (var x = 0; x < width; x++)
            {
                var i = rowOffset + x;

                var oldR = Clamp(r[i]);
                var oldG = Clamp(g[i]);
                var oldB = Clamp(b[i]);

                var paletteIndex = MedianCutQuantizer.NearestIndex(palette, oldR, oldG, oldB);
                indices[i] = (byte)paletteIndex;

                var chosen = palette[paletteIndex];
                var errR = oldR - chosen.R;
                var errG = oldG - chosen.G;
                var errB = oldB - chosen.B;

                Distribute(r, g, b, width, height, x + 1, y, errR, errG, errB, 7f / 16f);
                Distribute(r, g, b, width, height, x - 1, y + 1, errR, errG, errB, 3f / 16f);
                Distribute(r, g, b, width, height, x, y + 1, errR, errG, errB, 5f / 16f);
                Distribute(r, g, b, width, height, x + 1, y + 1, errR, errG, errB, 1f / 16f);
            }
        }

        return indices;
    }

    private static void Distribute(float[] r, float[] g, float[] b, int width, int height,
        int x, int y, float errR, float errG, float errB, float factor)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        var i = y * width + x;
        r[i] += errR * factor;
        g[i] += errG * factor;
        b[i] += errB * factor;
    }

    private static int Clamp(float v) => v < 0 ? 0 : v > 255 ? 255 : (int)MathF.Round(v);
}