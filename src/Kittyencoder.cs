using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace TerImageSharp;

public static class KittyEncoder
{
    private const int ChunkSize = 4096; // bytes of base64 payload per escape sequence, per the kitty spec

    public static string Encode(Image<Rgba32> image)
    {
        var width = image.Width;
        var height = image.Height;
        var raw = new byte[width * height * 4];

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < height; y++)
            {
                var row = accessor.GetRowSpan(y);
                var offset = y * width * 4;
                for (var x = 0; x < width; x++)
                {
                    var p = row[x];
                    raw[offset + x * 4 + 0] = p.R;
                    raw[offset + x * 4 + 1] = p.G;
                    raw[offset + x * 4 + 2] = p.B;
                    raw[offset + x * 4 + 3] = p.A;
                }
            }
        });

        var base64 = Convert.ToBase64String(raw);
        var sb = new StringBuilder(base64.Length + base64.Length / ChunkSize * 64 + 64);

        var offset2 = 0;
        var first = true;
        while (offset2 < base64.Length)
        {
            var remaining = base64.Length - offset2;
            var take = Math.Min(ChunkSize, remaining);
            var isLastChunk = offset2 + take >= base64.Length;
            var chunk = base64.Substring(offset2, take);

            sb.Append("\x1b_G");
            if (first)
            {
                // a=T: transmit + display immediately. f=32: RGBA. s/v: pixel dimensions.
                sb.Append("a=T,f=32,s=").Append(width).Append(",v=").Append(height);
                first = false;
            }
            sb.Append(",m=").Append(isLastChunk ? 0 : 1);
            sb.Append(';').Append(chunk);
            sb.Append("\x1b\\");

            offset2 += take;
        }

        return sb.ToString();
    }
}