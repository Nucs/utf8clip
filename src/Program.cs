using System.Text;

namespace BlueMarsh.Utf8Clip;

public static class Program
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
                Clipboard.SetText(text);
            }
            else if (Console.IsOutputRedirected)
            {
                var text = Clipboard.GetText();
                if (text != null)
                {
                    using var writer = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false));
                    writer.Write(text);
                }
            }
            else
            {
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
        Console.WriteLine("utf8clip 2.0.0 - Cross-platform UTF-8 clipboard tool");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  <input> | utf8clip    Copy stdin to clipboard");
        Console.WriteLine("  utf8clip              Print clipboard to stdout");
        Console.WriteLine("  utf8clip > file       Save clipboard to file");
        Console.WriteLine();
        Console.WriteLine($"Platform: {Clipboard.GetBackendName()}");
    }
}
