using System.Text;
using SixLabors.ImageSharp.PixelFormats;

namespace TerImageSharp;
public static class SixelEncoder
{
    public static string Encode(int width, int height, byte[] indices, List<Rgb24> palette)
    {
        var sb = new StringBuilder(width * height / 4 + 256);

        // DCS start: "q" enters sixel mode. Aspect ratio 1:1 (params "0;0").
        sb.Append("\x1bP0;0;0q");

        // Raster attributes: pixel aspect 1:1, image dimensions.
        sb.Append('"').Append("1;1;").Append(width).Append(';').Append(height);

        // Palette definitions: #index;2;R%;G%;B% (percentages 0-100, sixel's native color scale).
        for (var i = 0; i < palette.Count; i++)
        {
            var c = palette[i];
            sb.Append('#').Append(i).Append(';').Append(2).Append(';')
              .Append(ToPercent(c.R)).Append(';')
              .Append(ToPercent(c.G)).Append(';')
              .Append(ToPercent(c.B));
        }

        var bandBuffer = new StringBuilder(width);

        for (var bandTop = 0; bandTop < height; bandTop += 6)
        {
            var bandHeight = Math.Min(6, height - bandTop);
            var colorsInBand = CollectColorsInBand(indices, width, bandTop, bandHeight, palette.Count);

            var first = true;
            foreach (var colorIndex in colorsInBand)
            {
                bandBuffer.Clear();
                for (var x = 0; x < width; x++)
                {
                    var bits = 0;
                    for (var row = 0; row < bandHeight; row++)
                    {
                        var pixelIndex = (bandTop + row) * width + x;
                        if (indices[pixelIndex] == colorIndex)
                            bits |= 1 << row;
                    }
                    bandBuffer.Append((char)(63 + bits));
                }

                if (!first) sb.Append('$'); // carriage return: overlay next color on same band
                first = false;

                sb.Append('#').Append(colorIndex);
                AppendRunLengthEncoded(sb, bandBuffer);
            }

            sb.Append('-'); // move down to next band (6 more rows)
        }

        sb.Append("\x1b\\"); // ST: end DCS / sixel sequence

        return sb.ToString();
    }

    private static List<int> CollectColorsInBand(byte[] indices, int width, int bandTop, int bandHeight, int paletteSize)
    {
        var present = new bool[paletteSize];
        for (var row = 0; row < bandHeight; row++)
        {
            var rowOffset = (bandTop + row) * width;
            for (var x = 0; x < width; x++)
                present[indices[rowOffset + x]] = true;
        }

        var result = new List<int>();
        for (var i = 0; i < paletteSize; i++)
            if (present[i]) result.Add(i);
        return result;
    }

    private static void AppendRunLengthEncoded(StringBuilder sb, StringBuilder band)
    {
        var i = 0;
        while (i < band.Length)
        {
            var c = band[i];
            var runLength = 1;
            while (i + runLength < band.Length && band[i + runLength] == c)
                runLength++;

            if (runLength >= 4)
            {
                sb.Append('!').Append(runLength).Append(c);
            }
            else
            {
                for (var k = 0; k < runLength; k++) sb.Append(c);
            }
            i += runLength;
        }
    }

    private static int ToPercent(byte channel) => (int)Math.Round(channel / 255.0 * 100.0);
}