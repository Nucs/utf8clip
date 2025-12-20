# utf8clip

Cross-platform UTF-8 clipboard tool with full emoji support.

Fork of [bluemarsh/utf8clip](https://github.com/bluemarsh/utf8clip) with:
- Native AOT binaries (no runtime required)
- Cross-platform support (Windows, Linux, macOS)
- Full UTF-8 and emoji support (💪🎉✅🚀)

## Downloads

| Platform | Binary | Size | Runtime Required |
|----------|--------|------|------------------|
| Windows x64 | `utf8clip-win-x64.exe` | ~1.6MB | None |
| Linux x64 | `utf8clip-linux-x64` | ~1.5MB | None (needs xclip/xsel) |
| macOS x64 | `utf8clip-osx-x64` | ~1.5MB | None |
| macOS ARM64 | `utf8clip-osx-arm64` | ~1.5MB | None |
| Cross-platform | `utf8clip-dotnet.zip` | ~12KB | .NET 10 |

Download from [Releases](https://github.com/Nucs/utf8clip/releases).

## Usage

Copy to clipboard:
```bash
echo "Hello 💪" | utf8clip
cat file.txt | utf8clip
```

Paste from clipboard:
```bash
utf8clip
utf8clip > output.txt
```

Cross-platform DLL (requires .NET 10):
```bash
echo "Hello 💪" | dotnet utf8clip.dll
dotnet utf8clip.dll > output.txt
```

## Platform Requirements

| Platform | Clipboard Backend |
|----------|-------------------|
| Windows | Native (user32.dll) |
| Linux | `xclip` or `xsel` (install one) |
| macOS | Built-in `pbcopy`/`pbpaste` |

### Linux Setup
```bash
# Ubuntu/Debian
sudo apt install xclip

# Fedora
sudo dnf install xclip

# Arch
sudo pacman -S xclip
```

## Building

```bash
# AOT for current platform
dotnet publish src/utf8clip.csproj -c Release -p:PublishAot=true

# AOT for specific platform
dotnet publish src/utf8clip.csproj -c Release -r linux-x64 -p:PublishAot=true
dotnet publish src/utf8clip.csproj -c Release -r osx-arm64 -p:PublishAot=true

# Cross-platform DLL
dotnet build src/utf8clip.csproj -c Release
```

## License

MIT License - see [LICENSE](LICENSE)

## Credits

- Original: [Aaron Meyers / bluemarsh](https://github.com/bluemarsh/utf8clip)
- Cross-platform fork: [Nucs](https://github.com/Nucs/utf8clip)
