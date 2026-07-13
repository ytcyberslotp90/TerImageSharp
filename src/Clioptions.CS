using SixLabors.ImageSharp.PixelFormats;

namespace TerImageSharp;

public enum ProtocolChoice { Auto, Sixel, Kitty }

public sealed class CliOptions
{
    public string? ImagePath;
    public ProtocolChoice Protocol = ProtocolChoice.Auto;
    public int Colors = 256;
    public bool Dither = true;
    public int? TargetWidth;
    public int? TargetHeight;
    public double? Scale;
    public Rgb24 Background = new(0, 0, 0);
    public int? LoopOverride;
    public double? FpsOverride;
    public bool PlayOnce;
    public bool ShowInfo;
    public bool ShowHelp;

    public static CliOptions Parse(string[] args)
    {
        var o = new CliOptions();
        if (args.Length == 0) { o.ShowHelp = true; return o; }

        var i = 0;
        if (!args[0].StartsWith("--"))
        {
            o.ImagePath = args[0];
            i = 1;
        }

        for (; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h":
                case "--help":
                    o.ShowHelp = true;
                    break;

                case "--sixel":
                    o.Protocol = ProtocolChoice.Sixel;
                    break;

                case "--kitty":
                    o.Protocol = ProtocolChoice.Kitty;
                    break;

                case "--auto":
                    o.Protocol = ProtocolChoice.Auto;
                    break;

                case "--colors":
                    o.Colors = ReadInt(args, ref i, "--colors", 256);
                    if (o.Colors is < 2 or > 256)
                        throw new ArgumentException("--colors must be between 2 and 256 (Sixel's palette ceiling).");
                    break;

                case "--no-dither":
                    o.Dither = false;
                    break;

                case "--width":
                    o.TargetWidth = ReadInt(args, ref i, "--width", 0);
                    break;

                case "--height":
                    o.TargetHeight = ReadInt(args, ref i, "--height", 0);
                    break;

                case "--scale":
                    if (i + 1 < args.Length && double.TryParse(args[i + 1], out var s))
                    {
                        o.Scale = s;
                        i++;
                    }
                    break;

                case "--bg":
                    o.Background = ParseColor(ReadString(args, ref i, "--bg"));
                    break;

                case "--loop":
                    o.LoopOverride = ReadInt(args, ref i, "--loop", 0);
                    break;

                case "--fps":
                    if (i + 1 < args.Length && double.TryParse(args[i + 1], out var fps))
                    {
                        o.FpsOverride = fps;
                        i++;
                    }
                    break;

                case "--once":
                    o.PlayOnce = true;
                    break;

                case "--info":
                    o.ShowInfo = true;
                    break;

                default:
                    if (o.ImagePath is null && !args[i].StartsWith("--"))
                        o.ImagePath = args[i];
                    else
                        throw new ArgumentException($"Unknown option: {args[i]}");
                    break;
            }
        }

        return o;
    }

    private static int ReadInt(string[] args, ref int i, string flag, int fallback)
    {
        if (i + 1 < args.Length && int.TryParse(args[i + 1], out var v))
        {
            i++;
            return v;
        }
        throw new ArgumentException($"{flag} requires a numeric value.");
    }

    private static string ReadString(string[] args, ref int i, string flag)
    {
        if (i + 1 < args.Length)
        {
            i++;
            return args[i];
        }
        throw new ArgumentException($"{flag} requires a value.");
    }

    private static Rgb24 ParseColor(string text)
    {
        text = text.Trim();
        if (text.StartsWith('#')) text = text[1..];

        if (text.Contains(','))
        {
            var parts = text.Split(',');
            if (parts.Length == 3 &&
                byte.TryParse(parts[0], out var r) &&
                byte.TryParse(parts[1], out var g) &&
                byte.TryParse(parts[2], out var b))
                return new Rgb24(r, g, b);
        }
        else if (text.Length == 6 && byte.TryParse(text[..2], System.Globalization.NumberStyles.HexNumber, null, out var hr)
                                   && byte.TryParse(text[2..4], System.Globalization.NumberStyles.HexNumber, null, out var hg)
                                   && byte.TryParse(text[4..6], System.Globalization.NumberStyles.HexNumber, null, out var hb))
        {
            return new Rgb24(hr, hg, hb);
        }

        throw new ArgumentException($"Couldn't parse color '{text}'. Use \"R,G,B\" or a hex code like 1a1a1a.");
    }

    public static void PrintHelp()
    {
        Console.WriteLine("""
        ────────────────────────────────────────────────────────────────────────────────────────────
        TerImageSharp | real pixel graphics for the terminal (Sixel + Kitty protocol)
        ────────────────────────────────────────────────────────────────────────────────────────────
        Usage:
          TerImageSharp <image> [options]

        Protocol:
          --auto              Auto-detect terminal (default). Falls back to --sixel if unknown.
          --sixel             Force Sixel output.
          --kitty             Force Kitty graphics protocol output (truecolor, no quantization).

        Sixel-only:
          --colors N          Palette size, 2-256 (default 256).
          --no-dither         Disable Floyd-Steinberg dithering (nearest-color only).
          --bg R,G,B | #hex   Background used to flatten transparency (default black).

        Sizing:
          --width N           Resize to exact pixel width (aspect kept if height omitted).
          --height N          Resize to exact pixel height (aspect kept if width omitted).
          --scale F           Scale factor applied to the image's native size, e.g. 0.5.

        Animated GIFs:
          --loop N            Override loop count (0 = infinite).
          --fps F             Override playback speed, ignoring each frame's own delay.
          --once              Play through once and stop, ignoring the GIF's loop count.

        Other:
          --info              Print format/dimensions/frame count and exit (no rendering).
          -h, --help           Show this help.

        Supported formats:
          PNG, JPEG, BMP, GIF (animated), TGA, PBM

        ────────────────────────────────────────────────────────────────────────────────────────────
        TerImageSharp v1.0
        Created by Cyber Slot • Made in India.
        Thanks for trying TerImageSharp!
        """);
    }
}