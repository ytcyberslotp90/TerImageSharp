using TerImageSharp;

try
{
    var options = CliOptions.Parse(args);

    if (options.ShowHelp)
    {
        CliOptions.PrintHelp();
        return 0;
    }

    if (!WindowsConsoleSupport.TryEnable())
    {
        Console.Error.WriteLine(
            "Warning: couldn't enable VT/ANSI processing on this console — Sixel/Kitty " +
            "output will likely print as garbled text instead of an image. Run this " +
            "inside Windows Terminal (wt.exe), not classic Command Prompt/conhost, " +
            "which does not support Sixel graphics at all.");
    }

    ImageRenderer.Run(options);
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}