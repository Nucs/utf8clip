using Nucs.Utf8Clip;

namespace utf8clip.Tests;

/// <summary>
/// Tests for Unicode and emoji support.
/// </summary>
public class UnicodeTests
{
    [Fact]
    public void SetText_GetText_BasicEmoji()
    {
        var testText = "Hello 💪 World";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_MultipleEmojis()
    {
        var testText = "Emojis: 💪🎉✅🚀😀🔥💯👍";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_EmojiOnly()
    {
        var testText = "💪🎉✅🚀";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_ComplexEmoji()
    {
        // Emoji with skin tone modifiers and ZWJ sequences
        var testText = "👨‍👩‍👧‍👦 👋🏽 🏳️‍🌈";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_FlagEmoji()
    {
        var testText = "Flags: 🇺🇸 🇬🇧 🇯🇵 🇩🇪 🇫🇷";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_ChineseCharacters()
    {
        var testText = "中文测试 Chinese Test";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_JapaneseCharacters()
    {
        var testText = "日本語テスト Japanese Test ひらがな カタカナ";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_KoreanCharacters()
    {
        var testText = "한국어 테스트 Korean Test";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_ArabicCharacters()
    {
        var testText = "اختبار عربي Arabic Test";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_HebrewCharacters()
    {
        var testText = "בדיקה עברית Hebrew Test";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_RussianCharacters()
    {
        var testText = "Русский тест Russian Test";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_GreekCharacters()
    {
        var testText = "Ελληνικά Greek Test αβγδ";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_ThaiCharacters()
    {
        var testText = "ทดสอบภาษาไทย Thai Test";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_MathSymbols()
    {
        var testText = "Math: ∑ ∏ √ ∞ ≠ ≤ ≥ ± × ÷";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_CurrencySymbols()
    {
        var testText = "Currency: $ € £ ¥ ₹ ₽ ₿";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_MixedContent()
    {
        var testText = @"# Mixed Content Test 💪

## Features
- UTF-8 support ✅
- Emoji support 🎉
- 中文支持
- Special chars: !@#$%^&*()

```code
var x = ""hello"";
```

> Quote with 'single' and ""double"" quotes

Price: $100 / €85 / £75
";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_SupplementaryPlane()
    {
        // Characters outside Basic Multilingual Plane (require surrogate pairs in UTF-16)
        var testText = "𝕳𝖊𝖑𝖑𝖔 𝕎𝕠𝕣𝕝𝕕"; // Mathematical symbols

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }

    [Fact]
    public void SetText_GetText_MusicalSymbols()
    {
        var testText = "Music: 𝄞 𝄢 𝅗𝅥 ♩ ♪ ♫ ♬";

        Clipboard.SetText(testText);
        var result = Clipboard.GetText();

        Assert.Equal(testText, result);
    }
}
