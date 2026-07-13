namespace TerImageSharp;

public enum GraphicsProtocol { Sixel, Kitty }
public static class TerminalCapabilities
{
    public static GraphicsProtocol? Detect()
    {
        var termProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM") ?? "";
        var term = Environment.GetEnvironmentVariable("TERM") ?? "";
        var kittyWindowId = Environment.GetEnvironmentVariable("KITTY_WINDOW_ID");

        // Kitty-protocol terminals
        if (!string.IsNullOrEmpty(kittyWindowId)) return GraphicsProtocol.Kitty;
        if (term.Contains("kitty", StringComparison.OrdinalIgnoreCase)) return GraphicsProtocol.Kitty;
        if (termProgram.Equals("ghostty", StringComparison.OrdinalIgnoreCase)) return GraphicsProtocol.Kitty;
        if (termProgram.Equals("WezTerm", StringComparison.OrdinalIgnoreCase)) return GraphicsProtocol.Kitty;
        if (termProgram.Equals("konsole", StringComparison.OrdinalIgnoreCase)) return GraphicsProtocol.Kitty;
        if (Environment.GetEnvironmentVariable("KONSOLE_VERSION") is not null) return GraphicsProtocol.Kitty;

        // Sixel-protocol terminals
        if (term.Contains("mlterm", StringComparison.OrdinalIgnoreCase)) return GraphicsProtocol.Sixel;
        if (term.Contains("foot", StringComparison.OrdinalIgnoreCase)) return GraphicsProtocol.Sixel;
        if (term.Contains("contour", StringComparison.OrdinalIgnoreCase)) return GraphicsProtocol.Sixel;
        if (term.StartsWith("xterm", StringComparison.OrdinalIgnoreCase)) return GraphicsProtocol.Sixel; // only true if built with --enable-sixel-graphics
        if (termProgram.Equals("mintty", StringComparison.OrdinalIgnoreCase)) return GraphicsProtocol.Sixel;
        if (termProgram.Equals("iTerm.app", StringComparison.OrdinalIgnoreCase)) return GraphicsProtocol.Sixel;
        if (termProgram.Equals("rio", StringComparison.OrdinalIgnoreCase)) return GraphicsProtocol.Sixel;

        return null; // unknown — caller decides the fallback
    }
}