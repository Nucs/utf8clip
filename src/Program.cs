using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace BlueMarsh.Utf8Clip;

public static partial class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length > 0 && (args[0] == "-h" || args[0] == "--help" || args[0] == "-?"))
            {
                PrintHelp();
                return 0;
            }

            if (Console.IsInputRedirected)
            {
                using var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);
                var text = reader.ReadToEnd();
                SetClipboardText(text);
            }
            else if (Console.IsOutputRedirected)
            {
                var text = GetClipboardText();
                if (text != null)
                {
                    using var writer = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false));
                    writer.Write(text);
                }
            }
            else
            {
                var text = GetClipboardText();
                if (text != null)
                {
                    var originalEncoding = Console.OutputEncoding;
                    try
                    {
                        Console.OutputEncoding = new UTF8Encoding(false);
                        Console.Write(text);
                    }
                    finally
                    {
                        Console.OutputEncoding = originalEncoding;
                    }
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("utf8clip 2.0.0 - Cross-platform UTF-8 clipboard tool");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  <input> | utf8clip    Copy stdin to clipboard");
        Console.WriteLine("  utf8clip              Print clipboard to stdout");
        Console.WriteLine("  utf8clip > file       Save clipboard to file");
        Console.WriteLine();
        Console.WriteLine("Platforms: Windows (native), Linux (xclip), macOS (pbcopy)");
    }

    private static string? GetClipboardText()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Windows.GetClipboardText();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return Linux.GetClipboardText();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return MacOS.GetClipboardText();
        else
            throw new PlatformNotSupportedException("Unsupported platform");
    }

    private static void SetClipboardText(string text)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Windows.SetClipboardText(text);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Linux.SetClipboardText(text);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            MacOS.SetClipboardText(text);
        else
            throw new PlatformNotSupportedException("Unsupported platform");
    }

    // ===== Windows Implementation =====
    private static partial class Windows
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

        public static string? GetClipboardText()
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

        public static void SetClipboardText(string text)
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
    private static class Linux
    {
        public static string? GetClipboardText()
        {
            // Try xclip first, then xsel
            var (exitCode, output) = RunProcess("xclip", "-selection clipboard -o");
            if (exitCode == 0)
                return output;

            (exitCode, output) = RunProcess("xsel", "--clipboard --output");
            if (exitCode == 0)
                return output;

            throw new InvalidOperationException("Install xclip or xsel for clipboard support");
        }

        public static void SetClipboardText(string text)
        {
            // Try xclip first, then xsel
            var exitCode = RunProcessWithInput("xclip", "-selection clipboard", text);
            if (exitCode == 0)
                return;

            exitCode = RunProcessWithInput("xsel", "--clipboard --input", text);
            if (exitCode == 0)
                return;

            throw new InvalidOperationException("Install xclip or xsel for clipboard support");
        }
    }

    // ===== macOS Implementation (pbcopy/pbpaste) =====
    private static class MacOS
    {
        public static string? GetClipboardText()
        {
            var (exitCode, output) = RunProcess("pbpaste", "");
            if (exitCode == 0)
                return output;

            throw new InvalidOperationException("pbpaste not available");
        }

        public static void SetClipboardText(string text)
        {
            var exitCode = RunProcessWithInput("pbcopy", "", text);
            if (exitCode != 0)
                throw new InvalidOperationException("pbcopy not available");
        }
    }

    // ===== Helper methods =====
    private static (int exitCode, string output) RunProcess(string fileName, string arguments)
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

    private static int RunProcessWithInput(string fileName, string arguments, string input)
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
