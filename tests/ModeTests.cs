using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Nucs.Utf8Clip;

namespace utf8clip.Tests;

/// <summary>
/// Tests for CLI mode flags (-i/--input, -o/--output) and auto-detection.
/// Tests the v2.1.0 explicit mode control feature.
/// </summary>
public class ModeTests : IDisposable
{
    private readonly string _projectPath;
    private readonly string _publishPath;
    private readonly string _executableName;
    private bool _isBuilt;

    public ModeTests()
    {
        var testDir = Path.GetDirectoryName(typeof(ModeTests).Assembly.Location)!;
        _projectPath = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", "..", "src", "utf8clip.csproj"));
        _publishPath = Path.GetFullPath(Path.Combine(testDir, "publish-mode-tests"));
        _executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "utf8clip.exe" : "utf8clip";
    }

    private void EnsureBuilt()
    {
        if (_isBuilt) return;

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"publish \"{_projectPath}\" -c Release -o \"{_publishPath}\" --nologo -v q",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo)!;
        process.WaitForExit(60000);

        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"Failed to build utf8clip: {error}");
        }

        _isBuilt = true;
    }

    private string GetExecutablePath()
    {
        EnsureBuilt();
        return Path.Combine(_publishPath, _executableName);
    }

    private (int exitCode, string stdout, string stderr) RunUtf8Clip(string? input = null, string args = "")
    {
        var exePath = GetExecutablePath();

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = args,
            RedirectStandardInput = input != null,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        using var process = Process.Start(startInfo)!;

        if (input != null)
        {
            using var writer = new StreamWriter(process.StandardInput.BaseStream, new UTF8Encoding(false));
            writer.Write(input);
        }

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(10000);

        return (process.ExitCode, stdout, stderr);
    }

    #region -i / --input flag tests (explicit copy mode)

    [Fact]
    public void InputFlag_Short_CopiesStdinToClipboard()
    {
        var testText = $"InputFlag_Short_{Guid.NewGuid()}";

        var (exitCode, stdout, stderr) = RunUtf8Clip(input: testText, args: "-i");

        Assert.Equal(0, exitCode);
        Assert.Empty(stdout);
        Assert.Equal(testText, Clipboard.GetText());
    }

    [Fact]
    public void InputFlag_Long_CopiesStdinToClipboard()
    {
        var testText = $"InputFlag_Long_{Guid.NewGuid()}";

        var (exitCode, stdout, stderr) = RunUtf8Clip(input: testText, args: "--input");

        Assert.Equal(0, exitCode);
        Assert.Empty(stdout);
        Assert.Equal(testText, Clipboard.GetText());
    }

    [Fact]
    public void InputFlag_Dash_CopiesStdinToClipboard()
    {
        var testText = $"InputFlag_Dash_{Guid.NewGuid()}";

        var (exitCode, stdout, stderr) = RunUtf8Clip(input: testText, args: "-");

        Assert.Equal(0, exitCode);
        Assert.Empty(stdout);
        Assert.Equal(testText, Clipboard.GetText());
    }

    [Fact]
    public void InputFlag_WithNoNewline_StripsTrailingNewline()
    {
        var (exitCode, stdout, stderr) = RunUtf8Clip(input: "TestText\n", args: "-i -n");

        Assert.Equal(0, exitCode);
        Assert.Equal("TestText", Clipboard.GetText());
    }

    [Fact]
    public void InputFlag_WithAppend_AppendsToClipboard()
    {
        Clipboard.SetText("First");

        var (exitCode, stdout, stderr) = RunUtf8Clip(input: " Second", args: "-i -a");

        Assert.Equal(0, exitCode);
        Assert.Equal("First Second", Clipboard.GetText());
    }

    [Fact]
    public void InputFlag_WithEmoji_PreservesUnicode()
    {
        var testText = "Emoji: 🎉💪🚀✅";

        var (exitCode, stdout, stderr) = RunUtf8Clip(input: testText, args: "-i");

        Assert.Equal(0, exitCode);
        Assert.Equal(testText, Clipboard.GetText());
    }

    [Fact]
    public void InputFlag_WithMultiLine_PreservesNewlines()
    {
        var testText = "Line 1\nLine 2\r\nLine 3";

        var (exitCode, stdout, stderr) = RunUtf8Clip(input: testText, args: "-i");

        Assert.Equal(0, exitCode);
        Assert.Equal(testText, Clipboard.GetText());
    }

    [Fact]
    public void InputFlag_WithSpecialChars_PreservesAll()
    {
        var testText = "Special: `backticks` $dollars 'single' \"double\" <angle> {braces}";

        var (exitCode, stdout, stderr) = RunUtf8Clip(input: testText, args: "-i");

        Assert.Equal(0, exitCode);
        Assert.Equal(testText, Clipboard.GetText());
    }

    #endregion

    #region -o / --output flag tests (explicit paste mode)

    [Fact]
    public void OutputFlag_Short_OutputsClipboard()
    {
        var testText = $"OutputFlag_Short_{Guid.NewGuid()}";
        Clipboard.SetText(testText);

        var (exitCode, stdout, stderr) = RunUtf8Clip(args: "-o");

        Assert.Equal(0, exitCode);
        Assert.Equal(testText, stdout);
    }

    [Fact]
    public void OutputFlag_Long_OutputsClipboard()
    {
        var testText = $"OutputFlag_Long_{Guid.NewGuid()}";
        Clipboard.SetText(testText);

        var (exitCode, stdout, stderr) = RunUtf8Clip(args: "--output");

        Assert.Equal(0, exitCode);
        Assert.Equal(testText, stdout);
    }

    [Fact]
    public void OutputFlag_WithEmoji_PreservesUnicode()
    {
        var testText = "Emoji: 🎉💪🚀✅";
        Clipboard.SetText(testText);

        var (exitCode, stdout, stderr) = RunUtf8Clip(args: "-o");

        Assert.Equal(0, exitCode);
        Assert.Equal(testText, stdout);
    }

    [Fact]
    public void OutputFlag_WithMultiLine_PreservesNewlines()
    {
        var testText = "Line 1\nLine 2\r\nLine 3";
        Clipboard.SetText(testText);

        var (exitCode, stdout, stderr) = RunUtf8Clip(args: "-o");

        Assert.Equal(0, exitCode);
        Assert.Equal(testText, stdout);
    }

    [Fact]
    public void OutputFlag_WithEmptyClipboard_OutputsNothing()
    {
        Clipboard.Clear();

        var (exitCode, stdout, stderr) = RunUtf8Clip(args: "-o");

        Assert.Equal(0, exitCode);
        Assert.Empty(stdout);
    }

    [Fact]
    public void OutputFlag_WithUnicode_PreservesCJK()
    {
        var testText = "中文 日本語 한국어";
        Clipboard.SetText(testText);

        var (exitCode, stdout, stderr) = RunUtf8Clip(args: "-o");

        Assert.Equal(0, exitCode);
        Assert.Equal(testText, stdout);
    }

    [Fact]
    public void OutputFlag_IgnoresStdin()
    {
        // Even with stdin provided, -o should output clipboard
        var testText = $"Clipboard_{Guid.NewGuid()}";
        Clipboard.SetText(testText);

        var (exitCode, stdout, stderr) = RunUtf8Clip(input: "This should be ignored", args: "-o");

        Assert.Equal(0, exitCode);
        Assert.Equal(testText, stdout);
        // Clipboard should remain unchanged
        Assert.Equal(testText, Clipboard.GetText());
    }

    #endregion

    #region Flag combinations

    [Fact]
    public void InputFlag_WithAppendAndNoNewline_CombinesBoth()
    {
        Clipboard.SetText("Existing");

        var (exitCode, stdout, stderr) = RunUtf8Clip(input: " Appended\n", args: "-i -a -n");

        Assert.Equal(0, exitCode);
        Assert.Equal("Existing Appended", Clipboard.GetText());
    }

    [Fact]
    public void InputFlag_MultipleFlags_LastWins()
    {
        // -i then -o should end up in paste mode (last wins)
        var testText = $"Clipboard_{Guid.NewGuid()}";
        Clipboard.SetText(testText);

        var (exitCode, stdout, stderr) = RunUtf8Clip(input: "Stdin content", args: "-i -o");

        Assert.Equal(0, exitCode);
        // Last flag wins, so paste mode
        Assert.Equal(testText, stdout);
    }

    [Fact]
    public void OutputFlag_ThenInputFlag_LastWins()
    {
        // -o then -i should end up in copy mode (last wins)
        var testText = $"CopyThis_{Guid.NewGuid()}";
        Clipboard.SetText("Original");

        var (exitCode, stdout, stderr) = RunUtf8Clip(input: testText, args: "-o -i");

        Assert.Equal(0, exitCode);
        // Last flag wins, so copy mode
        Assert.Equal(testText, Clipboard.GetText());
    }

    #endregion

    #region Help and version with new flags

    [Fact]
    public void Help_DocumentsInputFlag()
    {
        var (exitCode, stdout, stderr) = RunUtf8Clip(args: "--help");

        Assert.Equal(0, exitCode);
        Assert.Contains("-i", stdout);
        Assert.Contains("--input", stdout);
        Assert.Contains("copy", stdout.ToLower());
    }

    [Fact]
    public void Help_DocumentsOutputFlag()
    {
        var (exitCode, stdout, stderr) = RunUtf8Clip(args: "--help");

        Assert.Equal(0, exitCode);
        Assert.Contains("-o", stdout);
        Assert.Contains("--output", stdout);
        Assert.Contains("paste", stdout.ToLower());
    }

    [Fact]
    public void Help_DocumentsModeDetection()
    {
        var (exitCode, stdout, stderr) = RunUtf8Clip(args: "--help");

        Assert.Equal(0, exitCode);
        // Check for mode detection documentation
        Assert.Contains("Mode", stdout);
        Assert.Contains("-i/-o", stdout);
    }

    #endregion

    #region RoundTrip tests with explicit flags

    [Fact]
    public void RoundTrip_InputThenOutput_PreservesContent()
    {
        var testText = $"RoundTrip_{Guid.NewGuid()}_🎉";

        // Copy with -i
        var (copyExit, _, _) = RunUtf8Clip(input: testText, args: "-i");
        Assert.Equal(0, copyExit);

        // Paste with -o
        var (pasteExit, stdout, _) = RunUtf8Clip(args: "-o");
        Assert.Equal(0, pasteExit);

        Assert.Equal(testText, stdout);
    }

    [Fact]
    public void RoundTrip_LargeContent_PreservesAll()
    {
        var testText = new string('X', 100 * 1024); // 100KB

        var (copyExit, _, _) = RunUtf8Clip(input: testText, args: "-i");
        Assert.Equal(0, copyExit);

        var (pasteExit, stdout, _) = RunUtf8Clip(args: "-o");
        Assert.Equal(0, pasteExit);

        Assert.Equal(testText, stdout);
    }

    [Fact]
    public void RoundTrip_ComplexMarkdown_PreservesFormatting()
    {
        var testText = @"# Heading

## Subheading

- List item 1
- List item 2
  - Nested item

```csharp
var x = ""hello"";
Console.WriteLine(x);
```

> Blockquote with 'quotes'

**Bold** and *italic* and `code`

| Col1 | Col2 |
|------|------|
| A    | B    |

Done! 🎉
";

        var (copyExit, _, _) = RunUtf8Clip(input: testText, args: "-i");
        Assert.Equal(0, copyExit);

        var (pasteExit, stdout, _) = RunUtf8Clip(args: "-o");
        Assert.Equal(0, pasteExit);

        Assert.Equal(testText, stdout);
    }

    #endregion

    #region Edge cases

    [Fact]
    public void InputFlag_WithEmptyStdin_SetsEmptyClipboard()
    {
        Clipboard.SetText("Previous content");

        var (exitCode, stdout, stderr) = RunUtf8Clip(input: "", args: "-i");

        Assert.Equal(0, exitCode);
        Assert.Equal("", Clipboard.GetText());
    }

    [Fact]
    public void InputFlag_WhitespaceOnly_PreservesWhitespace()
    {
        var testText = "   \t\n   ";

        var (exitCode, stdout, stderr) = RunUtf8Clip(input: testText, args: "-i");

        Assert.Equal(0, exitCode);
        Assert.Equal(testText, Clipboard.GetText());
    }

    [Fact]
    public void InputFlag_NoNewline_StripsMixedNewlines()
    {
        var (exitCode, stdout, stderr) = RunUtf8Clip(input: "Text\r\n", args: "-i -n");

        Assert.Equal(0, exitCode);
        Assert.Equal("Text", Clipboard.GetText());
    }

    [Fact]
    public void InputFlag_NoNewline_PreservesInternalNewlines()
    {
        var (exitCode, stdout, stderr) = RunUtf8Clip(input: "Line1\nLine2\n", args: "-i -n");

        Assert.Equal(0, exitCode);
        Assert.Equal("Line1\nLine2", Clipboard.GetText());
    }

    [Fact]
    public void ClearFlag_TakesPrecedenceOverMode()
    {
        Clipboard.SetText("Content");

        // -c should clear regardless of -i or -o
        var (exitCode, stdout, stderr) = RunUtf8Clip(args: "-c -i");

        Assert.Equal(0, exitCode);
        Assert.Equal("", Clipboard.GetText());
    }

    [Fact]
    public void Version_OutputsToStdout()
    {
        var (exitCode, stdout, stderr) = RunUtf8Clip(args: "--version");

        Assert.Equal(0, exitCode);
        Assert.Matches(@"^\d+\.\d+\.\d+", stdout.Trim());
    }

    #endregion

    public void Dispose()
    {
        if (Directory.Exists(_publishPath))
        {
            try
            {
                Directory.Delete(_publishPath, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
