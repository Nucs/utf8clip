using Nucs.Utf8Clip;

namespace utf8clip.Tests;

/// <summary>
/// Tests for edge cases and boundary conditions.
/// </summary>
public class EdgeCaseTests
{
    [Fact]
    public void SetText_GetText_LargeText()
    {
        // 100KB of text
        var testText = new string('A', 100 * 1024);

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_VeryLargeText()
    {
        // 1MB of text
        var testText = new string('B', 1024 * 1024);

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_RepeatedOperations()
    {
        for (int i = 0; i < 10; i++)
        {
            var testText = $"Iteration_{i}_{Guid.NewGuid()}";

            Clipboard.SetText(testText);
            var result = Clipboard.GetText();

            Assert.Equal(testText, result);
        }
    }

    [Fact]
    public void SetText_OverwritesPrevious()
    {
        var first = "First text";
        var second = "Second text";

        Clipboard.SetText(first);
        Clipboard.SetText(second);
        var result = Clipboard.GetText();

        Assert.Equal(second, result);
    }

    [Fact]
    public void SetText_GetText_NullCharacter()
    {
        // Text with embedded null (should stop at null in some implementations)
        var testText = "Before\0After";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        // The result might be truncated at null on some platforms
        Assert.StartsWith("Before", result);
    }

    [Fact]
    public void SetText_GetText_TabsAndSpaces()
    {
        var testText = "Col1\tCol2\tCol3\n  Indented\t\tDouble Tab";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_LeadingTrailingWhitespace()
    {
        var testText = "   leading and trailing   ";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_OnlyNewlines()
    {
        var testText = "\n\n\n";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_MixedNewlines()
    {
        var testText = "Line1\nLine2\r\nLine3\rLine4";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_ControlCharacters()
    {
        // Bell, backspace, form feed, vertical tab
        var testText = "Control: \a\b\f\v";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_UnicodeNormalization()
    {
        // Same character in different Unicode normalizations
        // é as single codepoint vs e + combining accent
        var composed = "caf\u00E9";    // café with é as single char
        var decomposed = "cafe\u0301"; // café with e + combining acute

        Clipboard.SetText(composed);
        var result1 = Clipboard.GetText();

        Clipboard.SetText(decomposed);
        var result2 = Clipboard.GetText();

        // Both should roundtrip correctly (but may not equal each other)
        Assert.Equal(composed, result1);
        Assert.Equal(decomposed, result2);
    }

    [Fact]
    public void SetText_GetText_ZeroWidthCharacters()
    {
        // Zero-width space, zero-width non-joiner, zero-width joiner
        var testText = "Zero\u200Bwidth\u200Cspaces\u200Dhere";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_BOM()
    {
        // Byte Order Mark at start
        var testText = "\uFEFFText with BOM";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_PrivateUseArea()
    {
        // Private Use Area characters (often used for custom fonts/icons)
        var testText = "Private: \uE000\uE001\uE002";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_ReplacementCharacter()
    {
        // Unicode replacement character
        var testText = "Invalid: \uFFFD";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_DirectionalMarks()
    {
        // Left-to-right and right-to-left marks
        var testText = "LTR\u200Etext\u200Fand RTL\u200F";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }
}
