using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace TerImageSharp;

// Loads an image (PNG/JPEG/BMP/GIF/TGA/PBM - anything ImageSharp's default)
public static class ImageRenderer
{
    public static void Run(CliOptions options)
    {
        if (options.ImagePath is null)
            throw new ArgumentException("No image path given.");

        // INTERCEPT THE PIPE: If the path is a dash, switch to streaming mode
        if (options.ImagePath == "-")
        {
            StreamFromPipelines(options);
            return;
        }

        // Existing static image/GIF logic continues below...
        var protocol = ResolveProtocol(options.Protocol);
        using var image = Image.Load<Rgba32>(options.ImagePath);

        if (options.ShowInfo)
        {
            PrintInfo(options.ImagePath, image, protocol);
            return;
        }

        ApplySizing(image, options);

        if (image.Frames.Count > 1)
            RenderAnimated(image, options, protocol);
        else
            RenderStatic(image, options, protocol);
    }

    private static GraphicsProtocol ResolveProtocol(ProtocolChoice choice)
    {
        if (choice == ProtocolChoice.Sixel) return GraphicsProtocol.Sixel;
        if (choice == ProtocolChoice.Kitty) return GraphicsProtocol.Kitty;
        return TerminalCapabilities.Detect() ?? GraphicsProtocol.Sixel;
    }

    private static void ApplySizing(Image<Rgba32> image, CliOptions options)
    {
        if (options.Scale is { } scale && scale > 0 && scale != 1.0)
        {
            var w = Math.Max(1, (int)Math.Round(image.Width * scale));
            var h = Math.Max(1, (int)Math.Round(image.Height * scale));
            image.Mutate(ctx => ctx.Resize(w, h));
            return;
        }

        if (options.TargetWidth is { } tw && options.TargetHeight is { } th)
        {
            image.Mutate(ctx => ctx.Resize(tw, th));
        }
        else if (options.TargetWidth is { } twOnly)
        {
            image.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new Size(twOnly, 0),
                Mode = ResizeMode.Max,
            }));
        }
        else if (options.TargetHeight is { } thOnly)
        {
            image.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new Size(0, thOnly),
                Mode = ResizeMode.Max,
            }));
        }
    }

    private static void RenderStatic(Image<Rgba32> image, CliOptions options, GraphicsProtocol protocol)
    {
        var frame = new FrameSnapshot(image, 0);
        Console.Out.Write(EncodeFrame(frame, options, protocol));
        Console.Out.Write('\n');
    }

    private static void RenderAnimated(Image<Rgba32> image, CliOptions options, GraphicsProtocol protocol)
    {
        var frameCount = image.Frames.Count;
        var delaysMs = new int[frameCount];
        for (var i = 0; i < frameCount; i++)
            delaysMs[i] = GetFrameDelayMs(image.Frames[i], options.FpsOverride);

        var loopCount = options.LoopOverride
            ?? image.Metadata.GetGifMetadata()?.RepeatCount
            ?? 0; // 0 == infinite, matching the GIF spec's own convention

        var playOnce = options.PlayOnce || loopCount == 1;
        var infinite = !playOnce && loopCount == 0;

        // Pre-render every frame's escape sequence up front so playback timing
        // isn't skewed by quantization/encoding work happening mid-animation.
        var encodedFrames = new string[frameCount];
        for (var i = 0; i < frameCount; i++)
            encodedFrames[i] = EncodeFrame(new FrameSnapshot(image, i), options, protocol);

        Console.CancelKeyPress += (_, e) =>
        {
            Console.Out.Write("\x1b[?25h"); // restore cursor visibility on Ctrl+C
        };

        Console.Out.Write("\x1b[?25l");
        Console.Out.Write("\x1b[2J");
        Console.Out.Write("\x1b[H");

        try
        {
            var iterations = infinite ? long.MaxValue : Math.Max(1, loopCount);
            for (long loop = 0; loop < iterations; loop++)
            {
                for (var i = 0; i < frameCount; i++)
                {
                    Console.Out.Write("\x1b[H"); // CUP: move to top-left, no scroll dependency
                    Console.Out.Write(encodedFrames[i]);
                    Thread.Sleep(delaysMs[i]);
                }
            }
        }
        finally
        {
            Console.Out.Write("\x1b[?25h"); // show cursor again
        }
    }

    private static int GetFrameDelayMs(ImageFrame<Rgba32> frame, double? fpsOverride)
    {
        if (fpsOverride is { } fps && fps > 0)
            return Math.Max(1, (int)Math.Round(1000.0 / fps));

        var gifMeta = frame.Metadata.GetGifMetadata();
        var centiseconds = gifMeta?.FrameDelay ?? 10; // default ~10fps if metadata is missing
        if (centiseconds <= 0) centiseconds = 10;      // guard against 0-delay GIFs spinning the CPU
        return centiseconds * 10;
    }

    private static string EncodeFrame(FrameSnapshot snapshot, CliOptions options, GraphicsProtocol protocol)
    {
        using var frameImage = snapshot.ToImage();

        if (protocol == GraphicsProtocol.Kitty)
            return KittyEncoder.Encode(frameImage); // real alpha preserved, no compositing needed

        // Sixel has no alpha channel — flatten transparency onto the chosen background first.
        frameImage.Mutate(ctx => ctx.BackgroundColor(new Color(options.Background)));

        var palette = MedianCutQuantizer.BuildPalette(frameImage, options.Colors);
        var indices = FloydSteinbergDitherer.Dither(frameImage, palette, options.Dither);
        return SixelEncoder.Encode(frameImage.Width, frameImage.Height, indices, palette);
    }

    private static void PrintInfo(string path, Image<Rgba32> image, GraphicsProtocol protocol)
    {
        Console.WriteLine($"File:      {path}");
        Console.WriteLine($"Size:      {image.Width}x{image.Height}");
        Console.WriteLine($"Frames:    {image.Frames.Count}{(image.Frames.Count > 1 ? " (animated)" : "")}");
        if (image.Frames.Count > 1)
        {
            var repeat = image.Metadata.GetGifMetadata()?.RepeatCount ?? 0;
            Console.WriteLine($"Loop:      {(repeat == 0 ? "infinite" : repeat.ToString())}");
        }
        Console.WriteLine($"Protocol:  {protocol} (would be used for rendering)");
    }

    public static void StreamFromPipelines(CliOptions options)
    {
        // For raw video, we MUST know the dimensions ahead of time
        int width = options.TargetWidth ?? 640;
        int height = options.TargetHeight ?? 480;

        // Allocate a buffer exactly large enough for 1 frame of RGBA data (4 bytes per pixel)
        byte[] frameBuffer = new byte[width * height * 4];
        using var stdin = Console.OpenStandardInput();

        var protocol = ResolveProtocol(options.Protocol);

        // Setup terminal screen
        Console.Out.Write("\x1b[?25l"); // Hide cursor
        Console.Out.Write("\x1b[2J");   // Clear screen

        try
        {
            while (true)
            {
                int bytesRead = 0;
                // Ensure we read a complete frame from the pipe before processing
                while (bytesRead < frameBuffer.Length)
                {
                    int read = stdin.Read(frameBuffer, bytesRead, frameBuffer.Length - bytesRead);
                    if (read <= 0) return; // Stream ended or broken pipe
                    bytesRead += read;
                }

                // Wrap the raw byte array instantly into an ImageSharp object
                using var frameImage = Image.LoadPixelData<Rgba32>(frameBuffer, width, height);

                // Encode using your existing protocols
                string escapeSequence;
                if (protocol == GraphicsProtocol.Kitty)
                {
                    escapeSequence = KittyEncoder.Encode(frameImage);
                }
                else
                {
                    // Sixel logic requires a quick quantization pass
                    var palette = MedianCutQuantizer.BuildPalette(frameImage, options.Colors);
                    var indices = FloydSteinbergDitherer.Dither(frameImage, palette, options.Dither);
                    escapeSequence = SixelEncoder.Encode(frameImage.Width, frameImage.Height, indices, palette);
                }

                // Reset cursor to top-left and overwrite frame
                Console.Out.Write("\x1b[H");
                Console.Out.Write(escapeSequence);
            }
        }
        finally
        {
            Console.Out.Write("\x1b[?25h"); // Safeguard cursor restore
        }
    }

    private readonly struct FrameSnapshot(Image<Rgba32> source, int frameIndex)
    {
        public Image<Rgba32> ToImage() => source.Frames.CloneFrame(frameIndex);
    }
}