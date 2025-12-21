using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Nucs.Utf8Clip;

/// <summary>
/// Cross-platform clipboard operations with UTF-8 support.
/// </summary>
public static class Clipboard
{
    /// <summary>
    /// Gets text from the system clipboard.
    /// </summary>
    /// <returns>Clipboard text, or null if empty/unavailable.</returns>
    public static string? GetText()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return WindowsClipboard.GetText();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return LinuxClipboard.GetText();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return MacOSClipboard.GetText();
        else
            throw new PlatformNotSupportedException("Unsupported platform");
    }

    /// <summary>
    /// Sets text to the system clipboard.
    /// </summary>
    /// <param name="text">Text to copy to clipboard.</param>
    public static void SetText(string text)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            WindowsClipboard.SetText(text);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            LinuxClipboard.SetText(text);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            MacOSClipboard.SetText(text);
        else
            throw new PlatformNotSupportedException("Unsupported platform");
    }

    /// <summary>
    /// Clears the system clipboard.
    /// </summary>
    public static void Clear()
    {
        SetText(string.Empty);
    }

    /// <summary>
    /// Gets the current platform's clipboard backend name.
    /// </summary>
    public static string GetBackendName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "Windows (user32.dll)";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "Linux (xclip/xsel)";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "macOS (pbcopy/pbpaste)";
        else
            return "Unknown";
    }
}

// ===== Windows Implementation =====
internal static partial class WindowsClipboard
{
    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 0x0002;

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenClipboard(nint hWndNewOwner);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseClipboard();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EmptyClipboard();

    [LibraryImport("user32.dll")]
    private static partial nint GetClipboardData(uint uFormat);

    [LibraryImport("user32.dll")]
    private static partial nint SetClipboardData(uint uFormat, nint hMem);

    [LibraryImport("kernel32.dll")]
    private static partial nint GlobalAlloc(uint uFlags, nuint dwBytes);

    [LibraryImport("kernel32.dll")]
    private static partial nint GlobalLock(nint hMem);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GlobalUnlock(nint hMem);

    private static bool TryOpenClipboard(int maxRetries = 10)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            if (OpenClipboard(0))
                return true;
            Thread.Sleep(10);
        }
        return false;
    }

    public static string? GetText()
    {
        if (!TryOpenClipboard())
            return null;

        try
        {
            var hGlobal = GetClipboardData(CF_UNICODETEXT);
            if (hGlobal == 0)
                return null;

            var lpData = GlobalLock(hGlobal);
            if (lpData == 0)
                return null;

            try
            {
                return Marshal.PtrToStringUni(lpData);
            }
            finally
            {
                GlobalUnlock(hGlobal);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    public static void SetText(string text)
    {
        if (!TryOpenClipboard())
            throw new InvalidOperationException("Cannot open clipboard");

        try
        {
            EmptyClipboard();

            var bytes = Encoding.Unicode.GetBytes(text + '\0');
            var hGlobal = GlobalAlloc(GMEM_MOVEABLE, (nuint)bytes.Length);
            if (hGlobal == 0)
                throw new OutOfMemoryException("Cannot allocate clipboard memory");

            var lpData = GlobalLock(hGlobal);
            if (lpData == 0)
                throw new InvalidOperationException("Cannot lock clipboard memory");

            try
            {
                Marshal.Copy(bytes, 0, lpData, bytes.Length);
            }
            finally
            {
                GlobalUnlock(hGlobal);
            }

            if (SetClipboardData(CF_UNICODETEXT, hGlobal) == 0)
                throw new InvalidOperationException("Cannot set clipboard data");
        }
        finally
        {
            CloseClipboard();
        }
    }
}

// ===== Linux Implementation (xclip/xsel) =====
internal static class LinuxClipboard
{
    public static string? GetText()
    {
        // Try xclip first, then xsel
        var (exitCode, output) = ProcessHelper.Run("xclip", "-selection clipboard -o");
        if (exitCode == 0)
            return output;

        (exitCode, output) = ProcessHelper.Run("xsel", "--clipboard --output");
        if (exitCode == 0)
            return output;

        throw new InvalidOperationException("Install xclip or xsel for clipboard support");
    }

    public static void SetText(string text)
    {
        // Try xclip first, then xsel
        var exitCode = ProcessHelper.RunWithInput("xclip", "-selection clipboard", text);
        if (exitCode == 0)
            return;

        exitCode = ProcessHelper.RunWithInput("xsel", "--clipboard --input", text);
        if (exitCode == 0)
            return;

        throw new InvalidOperationException("Install xclip or xsel for clipboard support");
    }
}

// ===== macOS Implementation (pbcopy/pbpaste) =====
internal static class MacOSClipboard
{
    public static string? GetText()
    {
        var (exitCode, output) = ProcessHelper.Run("pbpaste", "");
        if (exitCode == 0)
            return output;

        throw new InvalidOperationException("pbpaste not available");
    }

    public static void SetText(string text)
    {
        var exitCode = ProcessHelper.RunWithInput("pbcopy", "", text);
        if (exitCode != 0)
            throw new InvalidOperationException("pbcopy not available");
    }
}

// ===== Process Helper =====
internal static class ProcessHelper
{
    public static (int exitCode, string output) Run(string fileName, string arguments)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return (process.ExitCode, output);
        }
        catch
        {
            return (-1, "");
        }
    }

    public static int RunWithInput(string fileName, string arguments, string input)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            using (var writer = new StreamWriter(process.StandardInput.BaseStream, new UTF8Encoding(false)))
            {
                writer.Write(input);
            }
            process.WaitForExit();

            return process.ExitCode;
        }
        catch
        {
            return -1;
        }
    }
}
