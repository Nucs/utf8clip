using System.Reflection;
using System.Text;

namespace Nucs.Utf8Clip;

public static class Program
{
    private static string Version =>
        typeof(Program).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "unknown";

    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            // Parse flags
            bool append = false, clear = false, noNewline = false;
            bool? explicitMode = null; // true = copy (input), false = paste (output)

            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "-h": case "--help": case "-?":
                        PrintHelp();
                        return 0;
                    case "-v": case "--version":
                        Console.WriteLine(Version);
                        return 0;
                    case "-a": case "--append":
                        append = true;
                        break;
                    case "-c": case "--clear":
                        clear = true;
                        break;
                    case "-n": case "--no-newline":
                        noNewline = true;
                        break;
                    case "-i": case "--input": case "-":
                        explicitMode = true; // copy mode
                        break;
                    case "-o": case "--output":
                        explicitMode = false; // paste mode
                        break;
                    default:
                        Console.Error.WriteLine($"Unknown option: {arg}");
                        Console.Error.WriteLine("Use --help for usage information.");
                        return 1;
                }
            }

            // Handle --clear
            if (clear)
            {
                Clipboard.Clear();
                return 0;
            }

            // Determine mode: explicit flag > auto-detection
            // Auto-detection: stdin redirected (and stdout not) = copy, otherwise = paste
            bool copyMode;
            if (explicitMode.HasValue)
            {
                copyMode = explicitMode.Value;
            }
            else
            {
                // Auto-detect: copy mode only if stdin is redirected AND stdout is NOT
                copyMode = Console.IsInputRedirected && !Console.IsOutputRedirected;
            }

            if (copyMode)
            {
                // Copy mode: stdin -> clipboard
                using var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);
                var text = reader.ReadToEnd();

                if (noNewline)
                    text = text.TrimEnd('\r', '\n');

                if (append)
                {
                    var existing = Clipboard.GetText() ?? "";
                    text = existing + text;
                }

                Clipboard.SetText(text);
            }
            else
            {
                // Paste mode: clipboard -> stdout
                OutputClipboard(Console.IsOutputRedirected);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static void OutputClipboard(bool toFile)
    {
        var text = Clipboard.GetText();
        if (text == null)
            return;

        if (toFile)
        {
            // Write to redirected stdout with UTF-8 no BOM
            using var writer = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false));
            writer.Write(text);
        }
        else
        {
            // Write to terminal with proper encoding
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

    private static void PrintHelp()
    {
        Console.WriteLine($"utf8clip {Version} - Cross-platform UTF-8 clipboard tool");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  <input> | utf8clip [-a] [-n]  Copy stdin to clipboard");
        Console.WriteLine("  utf8clip                      Print clipboard to stdout");
        Console.WriteLine("  utf8clip -c                   Clear clipboard");
        Console.WriteLine("  utf8clip --version            Show version");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -i, --input, -     Force copy mode (stdin -> clipboard)");
        Console.WriteLine("  -o, --output       Force paste mode (clipboard -> stdout)");
        Console.WriteLine("  -a, --append       Append to clipboard instead of replacing");
        Console.WriteLine("  -c, --clear        Clear clipboard contents");
        Console.WriteLine("  -n, --no-newline   Strip trailing newline from input");
        Console.WriteLine("  -v, --version      Show version");
        Console.WriteLine("  -h, --help         Show this help");
        Console.WriteLine();
        Console.WriteLine("Mode auto-detection: copy if stdin redirected and stdout not;");
        Console.WriteLine("otherwise paste. Use -i/-o to override in ambiguous cases.");
        Console.WriteLine();
        Console.WriteLine($"Platform: {Clipboard.GetBackendName()}");
    }
}
