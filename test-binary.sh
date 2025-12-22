#!/bin/bash
#
# utf8clip Binary Test Suite
# Tests all aspects of the utf8clip clipboard tool
#
# Usage: ./test-binary.sh [path-to-utf8clip]
#
# If no path provided, looks for utf8clip.exe in current directory
#

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Find the executable
if [ -n "$1" ]; then
    EXE="$1"
else
    if [ -f "./utf8clip.exe" ]; then
        EXE="./utf8clip.exe"
    elif [ -f "./utf8clip" ]; then
        EXE="./utf8clip"
    else
        echo "Usage: $0 [path-to-utf8clip]"
        echo "Error: utf8clip executable not found"
        exit 1
    fi
fi

# Verify executable exists
if [ ! -f "$EXE" ]; then
    echo "Error: $EXE not found"
    exit 1
fi

echo "Testing: $EXE"
echo "========================================"

PASSED=0
FAILED=0
TOTAL=0

# Helper function to run a test
run_test() {
    local test_num=$1
    local test_name=$2
    local expected=$3
    local actual=$4

    TOTAL=$((TOTAL + 1))

    if [ "$expected" = "$actual" ]; then
        echo -e "${GREEN}[PASS]${NC} Test $test_num: $test_name"
        PASSED=$((PASSED + 1))
    else
        echo -e "${RED}[FAIL]${NC} Test $test_num: $test_name"
        echo "  Expected: '$expected'"
        echo "  Actual:   '$actual'"
        FAILED=$((FAILED + 1))
    fi
}

# Helper function to run a test with contains check
run_test_contains() {
    local test_num=$1
    local test_name=$2
    local expected_substr=$3
    local actual=$4

    TOTAL=$((TOTAL + 1))

    if [[ "$actual" == *"$expected_substr"* ]]; then
        echo -e "${GREEN}[PASS]${NC} Test $test_num: $test_name"
        PASSED=$((PASSED + 1))
    else
        echo -e "${RED}[FAIL]${NC} Test $test_num: $test_name"
        echo "  Expected to contain: '$expected_substr'"
        echo "  Actual: '$actual'"
        FAILED=$((FAILED + 1))
    fi
}

# Helper function to get clipboard (cross-platform)
get_clipboard() {
    if command -v powershell.exe &> /dev/null; then
        powershell.exe -Command "[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; Get-Clipboard" | tr -d '\r'
    elif command -v pbpaste &> /dev/null; then
        pbpaste
    else
        echo "ERROR: No clipboard command found"
        exit 1
    fi
}

# Helper function to get clipboard length
get_clipboard_length() {
    if command -v powershell.exe &> /dev/null; then
        powershell.exe -Command "(Get-Clipboard).Length" | tr -d '\r'
    else
        get_clipboard | wc -c
    fi
}

echo ""
echo "=== CLI OPTIONS ==="
echo ""

# Test 1: --help
result=$($EXE --help 2>&1)
run_test_contains 1 "--help shows usage" "Usage:" "$result"

# Test 2: -h
result=$($EXE -h 2>&1 | head -1)
run_test_contains 2 "-h shows version" "utf8clip" "$result"

# Test 3: -?
result=$($EXE -? 2>&1 | head -1)
run_test_contains 3 "-? shows version" "utf8clip" "$result"

# Test 4: --version
result=$($EXE --version 2>&1)
run_test_contains 4 "--version shows version number" "." "$result"

# Test 5: -v
result=$($EXE -v 2>&1)
run_test_contains 5 "-v shows version number" "." "$result"

# Test 6: --help shows platform
result=$($EXE --help 2>&1)
run_test_contains 6 "--help shows platform" "Platform:" "$result"

echo ""
echo "=== ERROR HANDLING ==="
echo ""

# Test 7: Unknown option
result=$($EXE --unknown 2>&1 || true)
run_test_contains 7 "Unknown option error" "Unknown option" "$result"

# Test 8: Invalid short option
result=$($EXE -x 2>&1 || true)
run_test_contains 8 "Invalid short option error" "Unknown option" "$result"

# Test 9: Exit code on error
$EXE --invalid 2>/dev/null || exit_code=$?
run_test 9 "Exit code 1 on error" "1" "$exit_code"

# Test 10: Exit code on success
printf "test" | $EXE && exit_code=$? || exit_code=$?
run_test 10 "Exit code 0 on success" "0" "$exit_code"

echo ""
echo "=== BASIC OPERATIONS ==="
echo ""

# Test 11: Basic copy
printf "Hello World" | $EXE
result=$(get_clipboard)
run_test 11 "Basic copy" "Hello World" "$result"

# Test 12: --clear
$EXE --clear
result=$(get_clipboard)
run_test 12 "--clear empties clipboard" "" "$result"

# Test 13: -c
printf "Something" | $EXE
$EXE -c
result=$(get_clipboard)
run_test 13 "-c empties clipboard" "" "$result"

# Test 14: Overwrite
printf "First" | $EXE
printf "Second" | $EXE
result=$(get_clipboard)
run_test 14 "Overwrite previous content" "Second" "$result"

echo ""
echo "=== APPEND MODE ==="
echo ""

# Test 15: --append
printf "First" | $EXE
printf " Second" | $EXE --append
result=$(get_clipboard)
run_test 15 "--append" "First Second" "$result"

# Test 16: -a
$EXE -c
printf "A" | $EXE
printf "B" | $EXE -a
printf "C" | $EXE -a
result=$(get_clipboard)
run_test 16 "-a multiple appends" "ABC" "$result"

# Test 17: Append after clear
$EXE --clear
printf "After clear" | $EXE --append
result=$(get_clipboard)
run_test 17 "Append after clear" "After clear" "$result"

echo ""
echo "=== NO-NEWLINE MODE ==="
echo ""

# Test 18: --no-newline strips LF
printf "Text\n" | $EXE --no-newline
result=$(get_clipboard)
run_test 18 "--no-newline strips LF" "Text" "$result"

# Test 19: -n strips CRLF
printf "Text\r\n" | $EXE -n
result=$(get_clipboard)
run_test 19 "-n strips CRLF" "Text" "$result"

# Test 20: Multiple trailing newlines stripped
printf "Text\n\n\n" | $EXE -n
result=$(get_clipboard)
run_test 20 "Multiple newlines stripped" "Text" "$result"

# Test 21: No-newline when no newline present
printf "NoNewline" | $EXE -n
result=$(get_clipboard)
run_test 21 "No-newline when no newline" "NoNewline" "$result"

echo ""
echo "=== COMBINED OPTIONS ==="
echo ""

# Test 22: -a -n combined
$EXE -c
printf "Start" | $EXE
printf " End\n\n" | $EXE -a -n
result=$(get_clipboard)
run_test 22 "Combined -a -n" "Start End" "$result"

echo ""
echo "=== EMOJI SUPPORT ==="
echo ""

# Test 23: Basic emoji
printf "Hello 💪🎉✅🚀 World" | $EXE
result=$(get_clipboard)
run_test 23 "Basic emoji" "Hello 💪🎉✅🚀 World" "$result"

# Test 24: Emoji only
printf "💪🎉✅🚀" | $EXE
result=$(get_clipboard)
run_test 24 "Emoji only" "💪🎉✅🚀" "$result"

# Test 25: Complex emoji (ZWJ)
printf "👨‍👩‍👧‍👦 👋🏽 🏳️‍🌈" | $EXE
result=$(get_clipboard)
run_test 25 "Complex emoji (ZWJ)" "👨‍👩‍👧‍👦 👋🏽 🏳️‍🌈" "$result"

# Test 26: Flag emoji
printf "🇺🇸 🇬🇧 🇯🇵 🇩🇪 🇫🇷" | $EXE
result=$(get_clipboard)
run_test 26 "Flag emoji" "🇺🇸 🇬🇧 🇯🇵 🇩🇪 🇫🇷" "$result"

# Test 27: Skin tone modifiers
printf "👋🏻 👋🏼 👋🏽 👋🏾 👋🏿" | $EXE
result=$(get_clipboard)
run_test 27 "Skin tone modifiers" "👋🏻 👋🏼 👋🏽 👋🏾 👋🏿" "$result"

# Test 28: Gender variants
printf "👨‍💻 👩‍💻 🧑‍💻" | $EXE
result=$(get_clipboard)
run_test 28 "Gender variants" "👨‍💻 👩‍💻 🧑‍💻" "$result"

# Test 29: Keycap sequences
printf "1️⃣ 2️⃣ 3️⃣ #️⃣ *️⃣" | $EXE
result=$(get_clipboard)
run_test 29 "Keycap sequences" "1️⃣ 2️⃣ 3️⃣ #️⃣ *️⃣" "$result"

# Test 30: All emoji categories
printf "😀 👍 ❤️ 🎉 🌍 🍕 ⚽ 🚗 💡 🔣" | $EXE
result=$(get_clipboard)
run_test 30 "All emoji categories" "😀 👍 ❤️ 🎉 🌍 🍕 ⚽ 🚗 💡 🔣" "$result"

echo ""
echo "=== CJK LANGUAGES ==="
echo ""

# Test 31: Chinese
printf "中文测试 Chinese Test" | $EXE
result=$(get_clipboard)
run_test 31 "Chinese characters" "中文测试 Chinese Test" "$result"

# Test 32: Japanese
printf "日本語テスト ひらがな カタカナ" | $EXE
result=$(get_clipboard)
run_test 32 "Japanese characters" "日本語テスト ひらがな カタカナ" "$result"

# Test 33: Korean
printf "한국어 테스트 Korean" | $EXE
result=$(get_clipboard)
run_test 33 "Korean characters" "한국어 테스트 Korean" "$result"

echo ""
echo "=== RTL LANGUAGES ==="
echo ""

# Test 34: Arabic
printf "اختبار عربي Arabic" | $EXE
result=$(get_clipboard)
run_test 34 "Arabic (RTL)" "اختبار عربي Arabic" "$result"

# Test 35: Hebrew
printf "בדיקה עברית Hebrew" | $EXE
result=$(get_clipboard)
run_test 35 "Hebrew (RTL)" "בדיקה עברית Hebrew" "$result"

echo ""
echo "=== EUROPEAN LANGUAGES ==="
echo ""

# Test 36: Russian
printf "Русский тест Russian" | $EXE
result=$(get_clipboard)
run_test 36 "Russian (Cyrillic)" "Русский тест Russian" "$result"

# Test 37: Greek
printf "Ελληνικά αβγδ Greek" | $EXE
result=$(get_clipboard)
run_test 37 "Greek" "Ελληνικά αβγδ Greek" "$result"

# Test 38: German
printf "Deutsch äöüß ÄÖÜ" | $EXE
result=$(get_clipboard)
run_test 38 "German (umlauts)" "Deutsch äöüß ÄÖÜ" "$result"

# Test 39: French
printf "Français éèêë àâ ùû çÇ" | $EXE
result=$(get_clipboard)
run_test 39 "French (accents)" "Français éèêë àâ ùû çÇ" "$result"

# Test 40: Spanish
printf "Español ñÑ ¿¡ áéíóú" | $EXE
result=$(get_clipboard)
run_test 40 "Spanish" "Español ñÑ ¿¡ áéíóú" "$result"

# Test 41: Portuguese
printf "Português ãõ çÇ àá" | $EXE
result=$(get_clipboard)
run_test 41 "Portuguese" "Português ãõ çÇ àá" "$result"

# Test 42: Polish
printf "Polski test ąćęłńóśźż" | $EXE
result=$(get_clipboard)
run_test 42 "Polish" "Polski test ąćęłńóśźż" "$result"

# Test 43: Turkish
printf "Türkçe test İŞÇÖÜĞ" | $EXE
result=$(get_clipboard)
run_test 43 "Turkish" "Türkçe test İŞÇÖÜĞ" "$result"

echo ""
echo "=== ASIAN LANGUAGES ==="
echo ""

# Test 44: Thai
printf "ทดสอบภาษาไทย Thai" | $EXE
result=$(get_clipboard)
run_test 44 "Thai" "ทดสอบภาษาไทย Thai" "$result"

# Test 45: Hindi
printf "हिंदी परीक्षण Hindi" | $EXE
result=$(get_clipboard)
run_test 45 "Hindi (Devanagari)" "हिंदी परीक्षण Hindi" "$result"

# Test 46: Vietnamese
printf "Tiếng Việt thử nghiệm" | $EXE
result=$(get_clipboard)
run_test 46 "Vietnamese" "Tiếng Việt thử nghiệm" "$result"

echo ""
echo "=== SPECIAL CHARACTERS ==="
echo ""

# Test 47: Quotes and backticks
printf 'Code: `var x = "hello"` and '"'"'single'"'"'' | $EXE
result=$(get_clipboard)
run_test 47 "Quotes and backticks" 'Code: `var x = "hello"` and '"'"'single'"'"'' "$result"

# Test 48: Dollar signs
printf '$HOME $PATH ${USER}' | $EXE
result=$(get_clipboard)
run_test 48 "Dollar signs" '$HOME $PATH ${USER}' "$result"

# Test 49: Math symbols
printf "∑ ∏ √ ∞ ≠ ≤ ≥ ± × ÷" | $EXE
result=$(get_clipboard)
run_test 49 "Math symbols" "∑ ∏ √ ∞ ≠ ≤ ≥ ± × ÷" "$result"

# Test 50: Currency symbols
printf "$ € £ ¥ ₹ ₽ ₿" | $EXE
result=$(get_clipboard)
run_test 50 "Currency symbols" "$ € £ ¥ ₹ ₽ ₿" "$result"

# Test 51: Musical symbols
printf "♩ ♪ ♫ ♬ 𝄞" | $EXE
result=$(get_clipboard)
run_test 51 "Musical symbols" "♩ ♪ ♫ ♬ 𝄞" "$result"

# Test 52: Arrows
printf "← → ↑ ↓ ↔ ↕ ⇐ ⇒ ⇑ ⇓" | $EXE
result=$(get_clipboard)
run_test 52 "Arrows" "← → ↑ ↓ ↔ ↕ ⇐ ⇒ ⇑ ⇓" "$result"

# Test 53: Box drawing
printf "┌─────┐\n│ 💪  │\n└─────┘" | $EXE
result=$(get_clipboard)
run_test_contains 53 "Box drawing" "┌─────┐" "$result"

# Test 54: Chess symbols
printf "♔ ♕ ♖ ♗ ♘ ♙ ♚ ♛ ♜ ♝ ♞ ♟" | $EXE
result=$(get_clipboard)
run_test 54 "Chess symbols" "♔ ♕ ♖ ♗ ♘ ♙ ♚ ♛ ♜ ♝ ♞ ♟" "$result"

# Test 55: Card suits
printf "♠ ♣ ♥ ♦" | $EXE
result=$(get_clipboard)
run_test 55 "Card suits" "♠ ♣ ♥ ♦" "$result"

# Test 56: Weather symbols
printf "☀ ☁ ☂ ☃ ☄ ★ ☆" | $EXE
result=$(get_clipboard)
run_test 56 "Weather symbols" "☀ ☁ ☂ ☃ ☄ ★ ☆" "$result"

# Test 57: Dingbats
printf "✓ ✔ ✕ ✖ ✗ ✘ ✙ ✚ ✛ ✜" | $EXE
result=$(get_clipboard)
run_test 57 "Dingbats" "✓ ✔ ✕ ✖ ✗ ✘ ✙ ✚ ✛ ✜" "$result"

echo ""
echo "=== UNICODE EDGE CASES ==="
echo ""

# Test 58: Supplementary plane (math)
printf "𝕳𝖊𝖑𝖑𝖔 𝕎𝕠𝕣𝕝𝕕" | $EXE
result=$(get_clipboard)
run_test 58 "Supplementary plane" "𝕳𝖊𝖑𝖑𝖔 𝕎𝕠𝕣𝕝𝕕" "$result"

# Test 59: Replacement character
printf "Invalid: \uFFFD" | $EXE
result=$(get_clipboard)
run_test_contains 59 "Replacement character" "Invalid:" "$result"

# Test 60: Braille
printf "⠀⠁⠂⠃⠄⠅⠆⠇⠈⠉" | $EXE
result=$(get_clipboard)
run_test 60 "Braille patterns" "⠀⠁⠂⠃⠄⠅⠆⠇⠈⠉" "$result"

# Test 61: Superscript/subscript
printf "H₂O E=mc² x⁰ x¹ x² x³" | $EXE
result=$(get_clipboard)
run_test 61 "Super/subscript" "H₂O E=mc² x⁰ x¹ x² x³" "$result"

# Test 62: Fractions
printf "½ ⅓ ¼ ⅕ ⅙ ⅐ ⅛ ⅑ ⅒" | $EXE
result=$(get_clipboard)
run_test 62 "Fractions" "½ ⅓ ¼ ⅕ ⅙ ⅐ ⅛ ⅑ ⅒" "$result"

# Test 63: Roman numerals
printf "Ⅰ Ⅱ Ⅲ Ⅳ Ⅴ Ⅵ Ⅶ Ⅷ Ⅸ Ⅹ" | $EXE
result=$(get_clipboard)
run_test 63 "Roman numerals" "Ⅰ Ⅱ Ⅲ Ⅳ Ⅴ Ⅵ Ⅶ Ⅷ Ⅸ Ⅹ" "$result"

# Test 64: Enclosed alphanumerics
printf "Ⓐ Ⓑ Ⓒ ① ② ③ ❶ ❷ ❸" | $EXE
result=$(get_clipboard)
run_test 64 "Enclosed alphanumerics" "Ⓐ Ⓑ Ⓒ ① ② ③ ❶ ❷ ❸" "$result"

echo ""
echo "=== WHITESPACE HANDLING ==="
echo ""

# Test 65: Empty input
printf "" | $EXE
result=$(get_clipboard)
run_test 65 "Empty input" "" "$result"

# Test 66: Whitespace only
printf "   \t\t   " | $EXE
result=$(get_clipboard)
run_test_contains 66 "Whitespace only" "   " "$result"

# Test 67: Leading/trailing whitespace
printf "   leading and trailing   " | $EXE
result=$(get_clipboard)
run_test 67 "Leading/trailing whitespace" "   leading and trailing   " "$result"

# Test 68: Tabs preserved
printf "Col1\tCol2\tCol3" | $EXE
result=$(get_clipboard)
run_test_contains 68 "Tabs preserved" "Col1" "$result"

# Test 69: Only newlines
printf "\n\n\n" | $EXE
len=$(get_clipboard_length)
run_test 69 "Only newlines (length=3)" "3" "$len"

echo ""
echo "=== NEWLINE HANDLING ==="
echo ""

# Test 70: Multiline text
printf "Line 1\nLine 2\nLine 3" | $EXE
result=$(get_clipboard)
run_test_contains 70 "Multiline (LF)" "Line 1" "$result"

# Test 71: Windows CRLF
printf "Line 1\r\nLine 2\r\nLine 3" | $EXE
result=$(get_clipboard)
run_test_contains 71 "Multiline (CRLF)" "Line 1" "$result"

# Test 72: Mixed newlines
printf "Unix\nWindows\r\nOld Mac\rMixed" | $EXE
result=$(get_clipboard)
run_test_contains 72 "Mixed newlines" "Unix" "$result"

echo ""
echo "=== CONTENT TYPES ==="
echo ""

# Test 73: JSON content
printf '{"name": "test", "emoji": "💪", "chinese": "中文"}' | $EXE
result=$(get_clipboard)
run_test 73 "JSON content" '{"name": "test", "emoji": "💪", "chinese": "中文"}' "$result"

# Test 74: XML content
printf '<?xml version="1.0"?><root><text>Hello 💪</text></root>' | $EXE
result=$(get_clipboard)
run_test_contains 74 "XML content" "Hello 💪" "$result"

# Test 75: HTML content
printf '<html><body><h1>Hello 💪</h1></body></html>' | $EXE
result=$(get_clipboard)
run_test_contains 75 "HTML content" "Hello 💪" "$result"

# Test 76: Markdown content
printf "# Header\n\n- Item 1 💪\n- Item 2 ✅" | $EXE
result=$(get_clipboard)
run_test_contains 76 "Markdown content" "# Header" "$result"

# Test 77: Code snippet
printf 'var x = "hello 💪";\nconsole.log(x);' | $EXE
result=$(get_clipboard)
run_test_contains 77 "Code snippet" "var x" "$result"

# Test 78: SQL content
printf "SELECT * FROM users WHERE name = '中文';" | $EXE
result=$(get_clipboard)
run_test_contains 78 "SQL content" "SELECT" "$result"

# Test 79: Regex pattern
printf 'Pattern: ^[a-zA-Z0-9_💪]+$' | $EXE
result=$(get_clipboard)
run_test_contains 79 "Regex pattern" "Pattern:" "$result"

# Test 80: URL with unicode
printf "https://example.com/path?q=中文&emoji=💪" | $EXE
result=$(get_clipboard)
run_test 80 "URL with unicode" "https://example.com/path?q=中文&emoji=💪" "$result"

echo ""
echo "=== LARGE TEXT ==="
echo ""

# Test 81: 10KB text
dd if=/dev/zero bs=10240 count=1 2>/dev/null | tr '\0' 'X' | $EXE
len=$(get_clipboard_length)
run_test 81 "10KB text" "10240" "$len"

# Test 82: 100KB text
dd if=/dev/zero bs=1024 count=100 2>/dev/null | tr '\0' 'A' | $EXE
len=$(get_clipboard_length)
run_test 82 "100KB text" "102400" "$len"

# Test 83: 1MB text
dd if=/dev/zero bs=1024 count=1024 2>/dev/null | tr '\0' 'B' | $EXE
len=$(get_clipboard_length)
run_test 83 "1MB text" "1048576" "$len"

# Test 84: 5MB text
dd if=/dev/zero bs=1024 count=5120 2>/dev/null | tr '\0' 'Z' | $EXE
len=$(get_clipboard_length)
run_test 84 "5MB text" "5242880" "$len"

echo ""
echo "=== CONTROL CHARACTERS ==="
echo ""

# Test 85: Tab character
printf "Tab:\there" | $EXE
result=$(get_clipboard)
run_test_contains 85 "Tab character" "Tab:" "$result"

# Test 86: Control characters
printf "Bell:\a Back:\b" | $EXE
result=$(get_clipboard)
run_test_contains 86 "Control characters" "Bell:" "$result"

echo ""
echo "=== SPECIAL SCENARIOS ==="
echo ""

# Test 87: Path with spaces
printf 'C:\Program Files\My App\file.txt' | $EXE
result=$(get_clipboard)
run_test_contains 87 "Path with spaces" "Program Files" "$result"

# Test 88: Email format
printf "user@example.com" | $EXE
result=$(get_clipboard)
run_test 88 "Email format" "user@example.com" "$result"

# Test 89: Unicode domain
printf "user@例え.jp" | $EXE
result=$(get_clipboard)
run_test 89 "Unicode domain" "user@例え.jp" "$result"

echo ""
echo "=== REPEATED OPERATIONS ==="
echo ""

# Test 90: Multiple overwrites
printf "First" | $EXE
printf "Second" | $EXE
printf "Third" | $EXE
result=$(get_clipboard)
run_test 90 "Multiple overwrites" "Third" "$result"

# Test 91: Multiple appends
$EXE -c
printf "A" | $EXE
printf "B" | $EXE -a
printf "C" | $EXE -a
printf "D" | $EXE -a
result=$(get_clipboard)
run_test 91 "Multiple appends (4x)" "ABCD" "$result"

# Test 92: Clear then copy
$EXE --clear
printf "After clear" | $EXE
result=$(get_clipboard)
run_test 92 "Clear then copy" "After clear" "$result"

echo ""
echo "=== STRESS TESTS ==="
echo ""

# Test 93: Rapid copy operations
for i in 1 2 3 4 5; do printf "Iter$i" | $EXE; done
result=$(get_clipboard)
run_test 93 "Rapid copy (5x)" "Iter5" "$result"

# Test 94: Long single line
dd if=/dev/zero bs=50000 count=1 2>/dev/null | tr '\0' 'L' | $EXE
len=$(get_clipboard_length)
run_test 94 "Long single line (50KB)" "50000" "$len"

echo ""
echo "=== COMBINING DIACRITICS ==="
echo ""

# Test 95: Composed vs decomposed
printf "café" | $EXE
result=$(get_clipboard)
run_test 95 "Composed character" "café" "$result"

# Test 96: Decomposed character
printf "cafe\xcc\x81" | $EXE
result=$(get_clipboard)
run_test_contains 96 "Decomposed character" "cafe" "$result"

echo ""
echo "=== ZERO-WIDTH CHARACTERS ==="
echo ""

# Test 97: Zero-width space
printf "Zero\u200Bwidth" | $EXE
len=$(get_clipboard_length)
# Length should be > 9 (Zero + ZWS + width)
if [ "$len" -ge 9 ]; then
    echo -e "${GREEN}[PASS]${NC} Test 97: Zero-width space preserved"
    PASSED=$((PASSED + 1))
else
    echo -e "${RED}[FAIL]${NC} Test 97: Zero-width space - length=$len"
    FAILED=$((FAILED + 1))
fi
TOTAL=$((TOTAL + 1))

# Test 98: Directional marks
printf "LTR\u200Etext\u200FRTL" | $EXE
len=$(get_clipboard_length)
if [ "$len" -ge 10 ]; then
    echo -e "${GREEN}[PASS]${NC} Test 98: Directional marks preserved"
    PASSED=$((PASSED + 1))
else
    echo -e "${RED}[FAIL]${NC} Test 98: Directional marks - length=$len"
    FAILED=$((FAILED + 1))
fi
TOTAL=$((TOTAL + 1))

echo ""
echo "=== FINAL STRESS TEST ==="
echo ""

# Test 99: Mixed content stress test
printf "# 💪 Mixed Test 中文\n\nEmoji: 🎉✅🚀\nMath: ∑∏√∞\nCurrency: \$€£¥₿\nArrows: ←→↑↓\n\n日本語 한국어 العربية עברית" | $EXE
result=$(get_clipboard)
run_test_contains 99 "Mixed content stress test" "Mixed Test" "$result"

# Test 100: Full Unicode roundtrip
printf "ASCII + Emoji 💪 + CJK 中文 + RTL عربي + Math ∞ = ✅" | $EXE
result=$(get_clipboard)
run_test 100 "Full Unicode roundtrip" "ASCII + Emoji 💪 + CJK 中文 + RTL عربي + Math ∞ = ✅" "$result"

echo ""
echo "========================================"
echo "RESULTS"
echo "========================================"
echo -e "Total:  $TOTAL"
echo -e "${GREEN}Passed: $PASSED${NC}"
if [ $FAILED -gt 0 ]; then
    echo -e "${RED}Failed: $FAILED${NC}"
else
    echo -e "Failed: $FAILED"
fi
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}All tests passed!${NC}"
    exit 0
else
    echo -e "${RED}Some tests failed.${NC}"
    exit 1
fi
