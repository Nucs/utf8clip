using System.Runtime.InteropServices;
using BlueMarsh.Utf8Clip;

namespace utf8clip.Tests;

/// <summary>
/// Tests for clipboard operations.
/// These tests interact with the real system clipboard.
/// </summary>
public class ClipboardTests
{
    [Fact]
    public void GetBackendName_ReturnsNonEmpty()
    {
        var backend = Clipboard.GetBackendName();

        Assert.NotNull(backend);
        Assert.NotEmpty(backend);
    }

    [Fact]
    public void GetBackendName_MatchesPlatform()
    {
        var backend = Clipboard.GetBackendName();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.Contains("Windows", backend);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Assert.Contains("Linux", backend);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Assert.Contains("macOS", backend);
    }

    [Fact]
    public void SetText_GetText_BasicRoundtrip()
    {
        var testText = $"Test_{Guid.NewGuid()}";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_EmptyString()
    {
        Clipboard.SetText("");
        var result = Clipboard.GetText();

        Assert.Equal("", result);
    }

    [Fact]
    public void SetText_GetText_Whitespace()
    {
        var testText = "   \t\n   ";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_MultipleLines()
    {
        var testText = "Line 1\nLine 2\nLine 3";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_WindowsNewlines()
    {
        var testText = "Line 1\r\nLine 2\r\nLine 3";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_SpecialCharacters()
    {
        var testText = "Special: !@#$%^&*()_+-=[]{}|;':\",./<>?`~";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_Quotes()
    {
        var testText = "Single 'quotes' and double \"quotes\" mixed";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_Backticks()
    {
        var testText = "Code with `backticks` and ```triple backticks```";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_DollarSigns()
    {
        var testText = "Variables: $HOME $PATH ${USER}";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }
}
