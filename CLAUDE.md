# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build (cross-platform DLL, requires .NET 10)
dotnet build src/utf8clip.csproj -c Release

# Publish AOT binary for current platform
dotnet publish src/utf8clip.csproj -c Release -p:PublishAot=true

# Publish AOT for specific platform (must build ON that platform)
dotnet publish src/utf8clip.csproj -c Release -r win-x64 -p:PublishAot=true
dotnet publish src/utf8clip.csproj -c Release -r osx-arm64 -p:PublishAot=true

# Run tests
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~ClipboardTests.SetText_GetText_BasicRoundtrip"
```

## Architecture

Two-file codebase in `src/` (namespace: `Nucs.Utf8Clip`):
- **Program.cs**: CLI entry point with 3-way mode detection
- **Clipboard.cs**: Platform-specific implementations via `RuntimeInformation.IsOSPlatform()` dispatch

Platform backends:
- **Windows**: Native P/Invoke to user32.dll + kernel32.dll (CF_UNICODETEXT = UTF-16 format)
- **macOS**: Native P/Invoke to Objective-C runtime + NSPasteboard (preserves BOM and all Unicode)

Internal classes:
- `WindowsClipboard`: P/Invoke with retry logic (10 attempts, 10ms delay)
- `MacOSClipboard`: P/Invoke to libobjc.A.dylib, calls NSPasteboard via objc_msgSend
- `ProcessHelper`: Runs external processes for macOS pbcopy/pbpaste fallback

## CLI Behavior

Mode detection (in priority order):
1. `-h/--help/-?` or `-v/--version` → print and exit immediately
2. `-c/--clear` → clear clipboard via `SetText(string.Empty)` and exit
3. `stdin redirected` → **Copy mode**: reads stdin to clipboard
4. `stdout redirected` → **Paste mode (file)**: writes clipboard to stdout with UTF-8 no BOM
5. `neither redirected` → **Paste mode (terminal)**: temporarily sets `Console.OutputEncoding` to UTF-8

Options:
- `-a/--append`: In copy mode, prepends existing clipboard content to new input
- `-c/--clear`: Clears clipboard contents
- `-n/--no-newline`: Strips trailing `\r` and `\n` from input via `TrimEnd('\r', '\n')`
- `-v/--version`: Prints `AssemblyInformationalVersionAttribute` (format: `version+commit`)
- `-h/--help/-?`: Prints usage including current platform backend name

Exit codes: `0` success, `1` error. Errors written to stderr as `Error: {message}`.

## Key Design Decisions

- `[STAThread]` on Main required for Windows clipboard access
- `Clear()` implemented as `SetText(string.Empty)`
- Write operations use `UTF8Encoding(false)` (no BOM); Windows clipboard uses UTF-16 internally
- `GetText()` returns `null` on Windows if clipboard unavailable (no exception)

## Testing

Test files in `tests/` using xUnit:
- **ClipboardTests.cs**: Basic roundtrip, whitespace, special chars
- **UnicodeTests.cs**: Emoji, CJK, RTL scripts, supplementary plane chars
- **EdgeCaseTests.cs**: Large text (1MB), embedded nulls, control chars, normalization
- **IntegrationTests.cs**: End-to-end binary execution tests

Notes:
- Tests interact with real system clipboard
- Some integration tests skipped: "Console.IsOutputRedirected detection differs when run from test harness"
- Embedded `\0` may truncate text (Windows null-terminated format)
