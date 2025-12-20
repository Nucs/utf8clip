# utf8clip

Cross-platform UTF-8 clipboard tool with full emoji support.

Fork of [bluemarsh/utf8clip](https://github.com/bluemarsh/utf8clip) with:
- Cross-platform support (Windows, Linux, macOS)
- Native AOT binaries (no runtime required)
- Full UTF-8 and emoji support (💪🎉✅🚀)

## Downloads

| Platform | File | Runtime Required |
|----------|------|------------------|
| Cross-platform | `utf8clip-<version>-dotnet.zip` | .NET 10 |
| Windows x64 | `utf8clip-<version>-win-x64.zip` | None |
| Linux x64 | `utf8clip-<version>-linux-x64.zip` | None (needs xclip/xsel) |
| macOS ARM64 | `utf8clip-<version>-osx-arm64.zip` | None |

Download from [Releases](https://github.com/Nucs/utf8clip/releases).

## Usage

Copy to clipboard:
```bash
echo "Hello 💪" | ./utf8clip
cat file.txt | ./utf8clip
```

Paste from clipboard:
```bash
./utf8clip
./utf8clip > output.txt
```

Help:
```bash
./utf8clip --help
```

### Cross-platform DLL

Requires .NET 10 runtime installed:
```bash
# Extract utf8clip.dll and utf8clip.runtimeconfig.json
unzip utf8clip-<version>-dotnet.zip

# Use
echo "Hello 💪" | dotnet utf8clip.dll
dotnet utf8clip.dll > output.txt
```

## Platform Requirements

| Platform | Clipboard Backend | Notes |
|----------|-------------------|-------|
| Cross-platform (.NET) | Depends on OS | Requires .NET 10 runtime |
| Windows | Native (user32.dll) | No dependencies |
| Linux | `xclip` or `xsel` | Install one (tries xclip first) |
| macOS | `pbcopy`/`pbpaste` | Built-in, no dependencies |

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

# AOT for specific platform (must build ON that platform)
dotnet publish src/utf8clip.csproj -c Release -r win-x64 -p:PublishAot=true
dotnet publish src/utf8clip.csproj -c Release -r linux-x64 -p:PublishAot=true
dotnet publish src/utf8clip.csproj -c Release -r osx-arm64 -p:PublishAot=true

# Cross-platform DLL (runs anywhere with .NET 10)
dotnet build src/utf8clip.csproj -c Release
```

## License

MIT License - see [LICENSE](LICENSE)

## Credits

- Original: [Aaron Meyers / bluemarsh](https://github.com/bluemarsh/utf8clip)
- Cross-platform fork: [Nucs](https://github.com/Nucs/utf8clip)
