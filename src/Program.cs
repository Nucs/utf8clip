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

            if (Console.IsInputRedirected)
            {
                // Copy mode: stdin -> clipboard
                using var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);
                var text = reader.ReadToEnd();

                // Strip trailing newline if requested
                if (noNewline)
                    text = text.TrimEnd('\r', '\n');

                // Append to existing clipboard content if requested
                if (append)
                {
                    var existing = Clipboard.GetText() ?? "";
                    text = existing + text;
                }

                Clipboard.SetText(text);
            }
            else if (Console.IsOutputRedirected)
            {
                // Paste mode: clipboard -> file
                var text = Clipboard.GetText();
                if (text != null)
                {
                    using var writer = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false));
                    writer.Write(text);
                }
            }
            else
            {
                // Paste mode: clipboard -> terminal
                var text = Clipboard.GetText();
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
        Console.WriteLine($"utf8clip {Version} - Cross-platform UTF-8 clipboard tool");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  <input> | utf8clip [-a] [-n]  Copy stdin to clipboard");
        Console.WriteLine("  utf8clip                      Print clipboard to stdout");
        Console.WriteLine("  utf8clip -c                   Clear clipboard");
        Console.WriteLine("  utf8clip --version            Show version");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -a, --append       Append to clipboard instead of replacing");
        Console.WriteLine("  -c, --clear        Clear clipboard contents");
        Console.WriteLine("  -n, --no-newline   Strip trailing newline from input");
        Console.WriteLine("  -v, --version      Show version");
        Console.WriteLine("  -h, --help         Show this help");
        Console.WriteLine();
        Console.WriteLine($"Platform: {Clipboard.GetBackendName()}");
    }
}
