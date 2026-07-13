using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace TerImageSharp;
public static class WindowsConsoleSupport
{
    private const int STD_OUTPUT_HANDLE = -11;
    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    public static bool TryEnable()
    {
        if (!OperatingSystem.IsWindows()) return true; // non-Windows terminals interpret VT sequences natively

        return EnableOnWindows();
    }

    [SupportedOSPlatform("windows")]
    private static bool EnableOnWindows()
    {
        var handle = GetStdHandle(STD_OUTPUT_HANDLE);
        if (handle == IntPtr.Zero || handle == new IntPtr(-1)) return false;

        if (!GetConsoleMode(handle, out var mode)) return false;
        if ((mode & ENABLE_VIRTUAL_TERMINAL_PROCESSING) != 0) return true; // already on (e.g. Windows Terminal)

        mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
        return SetConsoleMode(handle, mode);
    }
}