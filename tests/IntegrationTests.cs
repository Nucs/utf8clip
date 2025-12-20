using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Nucs.Utf8Clip;

namespace utf8clip.Tests;

/// <summary>
/// Integration tests that test the actual binary behavior.
/// These tests build and run the utf8clip executable.
/// </summary>
public class IntegrationTests : IDisposable
{
    private readonly string _projectPath;
    private readonly string _publishPath;
    private readonly string _executableName;
    private bool _isBuilt;

    public IntegrationTests()
    {
        // Find project path relative to test assembly
        var testDir = Path.GetDirectoryName(typeof(IntegrationTests).Assembly.Location)!;
        _projectPath = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", "..", "src", "utf8clip.csproj"));
        _publishPath = Path.GetFullPath(Path.Combine(testDir, "publish"));
        _executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "utf8clip.exe" : "utf8clip";
    }

    private void EnsureBuilt()
    {
        if (_isBuilt) return;

        // Build the project
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
        process.WaitForExit(60000); // 60 second timeout

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
        process.WaitForExit(10000); // 10 second timeout

        return (process.ExitCode, stdout, stderr);
    }

    [Fact]
    public void Help_ReturnsZeroExitCode()
    {
        var (exitCode, stdout, stderr) = RunUtf8Clip(args: "--help");

        Assert.Equal(0, exitCode);
        Assert.Contains("utf8clip", stdout);
        Assert.Contains("Usage", stdout);
    }

    [Fact]
    public void Help_ShowsPlatform()
    {
        var (exitCode, stdout, stderr) = RunUtf8Clip(args: "--help");

        Assert.Equal(0, exitCode);
        Assert.Contains("Platform:", stdout);
    }

    [Fact]
    public void PipeInput_SetsClipboard()
    {
        var testText = $"Integration_{Guid.NewGuid()}";

        var (exitCode, stdout, stderr) = RunUtf8Clip(input: testText);

        Assert.Equal(0, exitCode);
        Assert.Empty(stdout); // No output when setting clipboard

        // Verify via API
        var result = Clipboard.GetText();
        Assert.Equal(testText, result);
    }

    [Fact(Skip = "Console.IsOutputRedirected detection differs when run from test harness")]
    public void RedirectOutput_GetsClipboard()
    {
        var testText = $"Output_{Guid.NewGuid()}";
        Clipboard.SetText(testText);

        // Run without input but with output redirection (simulated by reading stdout)
        var exePath = GetExecutablePath();
        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        using var process = Process.Start(startInfo)!;
        var stdout = process.StandardOutput.ReadToEnd();
        process.WaitForExit(10000);

        Assert.Equal(0, process.ExitCode);
        Assert.Equal(testText, stdout);
    }

    [Fact]
    public void PipeInput_Emoji_PreservesCorrectly()
    {
        var testText = "Emoji test: 💪🎉✅🚀";

        var (exitCode, stdout, stderr) = RunUtf8Clip(input: testText);

        Assert.Equal(0, exitCode);

        var result = Clipboard.GetText();
        Assert.Equal(testText, result);
    }

    [Fact]
    public void PipeInput_MultiLine_PreservesCorrectly()
    {
        var testText = "Line 1\nLine 2\nLine 3";

        var (exitCode, stdout, stderr) = RunUtf8Clip(input: testText);

        Assert.Equal(0, exitCode);

        var result = Clipboard.GetText();
        Assert.Equal(testText, result);
    }

    [Fact]
    public void PipeInput_SpecialChars_PreservesCorrectly()
    {
        var testText = "Special: `backticks` $dollars 'quotes' \"double\"";

        var (exitCode, stdout, stderr) = RunUtf8Clip(input: testText);

        Assert.Equal(0, exitCode);

        var result = Clipboard.GetText();
        Assert.Equal(testText, result);
    }

    [Fact]
    public void PipeInput_Unicode_PreservesCorrectly()
    {
        var testText = "Unicode: 中文 日本語 한국어 Русский العربية";

        var (exitCode, stdout, stderr) = RunUtf8Clip(input: testText);

        Assert.Equal(0, exitCode);

        var result = Clipboard.GetText();
        Assert.Equal(testText, result);
    }

    [Fact(Skip = "Console.IsOutputRedirected detection differs when run from test harness")]
    public void RoundTrip_ViaProcess()
    {
        var testText = $"RoundTrip_{Guid.NewGuid()}_💪";

        // Set via process
        RunUtf8Clip(input: testText);

        // Get via process
        var exePath = GetExecutablePath();
        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        using var process = Process.Start(startInfo)!;
        var result = process.StandardOutput.ReadToEnd();
        process.WaitForExit(10000);

        Assert.Equal(testText, result);
    }

    public void Dispose()
    {
        // Cleanup publish directory
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
