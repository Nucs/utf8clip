# utf8clip

UTF-8 clipboard tool for Windows and macOS with full emoji support.

Fork of [bluemarsh/utf8clip](https://github.com/bluemarsh/utf8clip) with:
- Windows and macOS support
- Native AOT binaries (no runtime required) or cross-platform .NET DLL
- Full UTF-8 and emoji support (💪🎉✅🚀)

## Why utf8clip?

**Windows** `clip.exe` mangles UTF-8/emoji. **macOS** `pbcopy`/`pbpaste` work but lack options.

| | clip.exe | pbcopy/pbpaste | utf8clip |
|-|----------|----------------|----------|
| UTF-8/Emoji | ❌ Corrupts | ✓ | ✓ |
| Paste to stdout | ❌ | ✓ | ✓ |
| Clear clipboard | ❌ | ❌ | ✓ |
| Append mode | ❌ | ❌ | ✓ |
| Strip newline | ❌ | ❌ | ✓ |

## Downloads

| Platform | File | Runtime Required |
|----------|------|------------------|
| Windows x64 | `utf8clip-<version>-win-x64.zip` | None |
| macOS ARM64 | `utf8clip-<version>-osx-arm64.zip` | None |
| Cross-platform | `utf8clip-<version>-dotnet.zip` | .NET 10 |

Download from [Releases](https://github.com/Nucs/utf8clip/releases).

## Usage

```bash
# Copy to clipboard
echo "Hello 💪" | utf8clip
cat file.txt | utf8clip

# Paste from clipboard
utf8clip                    # Print to terminal
utf8clip > output.txt       # Save to file

# Options
utf8clip --help             # or -h or -?
utf8clip --version          # Shows version+commit (e.g., 2.1.0+abc1234)
utf8clip --clear            # Clear clipboard
echo "text" | utf8clip -n   # Strip trailing newlines (--no-newline)
echo "more" | utf8clip -a   # Append to clipboard (--append)
echo "text" | utf8clip -a -n  # Combine options
```

> **Note**: On Unix/macOS use `./utf8clip` if not in PATH. On Windows use `utf8clip.exe` or `.\utf8clip.exe` in PowerShell.

### Cross-platform DLL

Requires .NET 10 runtime installed:
```bash
# Extract utf8clip.dll and utf8clip.runtimeconfig.json
unzip utf8clip-<version>-dotnet.zip

# Use
echo "Hello 💪" | dotnet utf8clip.dll
dotnet utf8clip.dll > output.txt
```

## Behavior

**Mode detection** (in priority order):
1. `--clear` flag → clears clipboard and exits
2. Stdin redirected → **copy mode** (stdin → clipboard)
3. Stdout redirected → **paste mode** (clipboard → file, UTF-8 no BOM)
4. Neither redirected → **paste mode** (clipboard → terminal)

**Exit codes**: `0` success, `1` error (errors written to stderr)

**Empty clipboard**: Returns empty string (no error)

## Platform Requirements

| Platform | Clipboard Backend | Notes |
|----------|-------------------|-------|
| Windows | Native (user32.dll) | Retry logic (10 attempts) for locked clipboard |
| macOS | Native (NSPasteboard) | Falls back to pbcopy/pbpaste in headless environments |
| Cross-platform (.NET) | Depends on OS | Requires .NET 10 runtime |

## Building

```bash
# AOT for current platform
dotnet publish src/utf8clip.csproj -c Release -p:PublishAot=true

# AOT for specific platform (must build ON that platform)
dotnet publish src/utf8clip.csproj -c Release -r win-x64 -p:PublishAot=true
dotnet publish src/utf8clip.csproj -c Release -r osx-arm64 -p:PublishAot=true

# Cross-platform DLL (runs on Windows/macOS with .NET 10)
dotnet build src/utf8clip.csproj -c Release

# Run tests
dotnet test
```

## License

MIT License - see [LICENSE](LICENSE)

## Credits

- Original: [Aaron Meyers / bluemarsh](https://github.com/bluemarsh/utf8clip)
- Cross-platform fork: [Nucs](https://github.com/Nucs/utf8clip)
