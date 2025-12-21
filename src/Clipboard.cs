using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Nucs.Utf8Clip;

/// <summary>
/// Cross-platform clipboard operations with UTF-8 support.
/// </summary>
public static class Clipboard
{
    /// <summary>
    /// Gets text from the system clipboard.
    /// </summary>
    /// <returns>Clipboard text, or null if empty/unavailable.</returns>
    public static string? GetText()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return WindowsClipboard.GetText();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return LinuxClipboard.GetText();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return MacOSClipboard.GetText();
        else
            throw new PlatformNotSupportedException("Unsupported platform");
    }

    /// <summary>
    /// Sets text to the system clipboard.
    /// </summary>
    /// <param name="text">Text to copy to clipboard.</param>
    public static void SetText(string text)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            WindowsClipboard.SetText(text);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            LinuxClipboard.SetText(text);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            MacOSClipboard.SetText(text);
        else
            throw new PlatformNotSupportedException("Unsupported platform");
    }

    /// <summary>
    /// Clears the system clipboard.
    /// </summary>
    public static void Clear()
    {
        SetText(string.Empty);
    }

    /// <summary>
    /// Gets the current platform's clipboard backend name.
    /// </summary>
    public static string GetBackendName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "Windows (user32.dll)";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "Linux (xclip/xsel)";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "macOS (NSPasteboard)";
        else
            return "Unknown";
    }
}

// ===== Windows Implementation =====
internal static partial class WindowsClipboard
{
    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 0x0002;

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenClipboard(nint hWndNewOwner);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseClipboard();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EmptyClipboard();

    [LibraryImport("user32.dll")]
    private static partial nint GetClipboardData(uint uFormat);

    [LibraryImport("user32.dll")]
    private static partial nint SetClipboardData(uint uFormat, nint hMem);

    [LibraryImport("kernel32.dll")]
    private static partial nint GlobalAlloc(uint uFlags, nuint dwBytes);

    [LibraryImport("kernel32.dll")]
    private static partial nint GlobalLock(nint hMem);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GlobalUnlock(nint hMem);

    private static bool TryOpenClipboard(int maxRetries = 10)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            if (OpenClipboard(0))
                return true;
            Thread.Sleep(10);
        }
        return false;
    }

    public static string? GetText()
    {
        if (!TryOpenClipboard())
            return null;

        try
        {
            var hGlobal = GetClipboardData(CF_UNICODETEXT);
            if (hGlobal == 0)
                return null;

            var lpData = GlobalLock(hGlobal);
            if (lpData == 0)
                return null;

            try
            {
                return Marshal.PtrToStringUni(lpData);
            }
            finally
            {
                GlobalUnlock(hGlobal);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    public static void SetText(string text)
    {
        if (!TryOpenClipboard())
            throw new InvalidOperationException("Cannot open clipboard");

        try
        {
            EmptyClipboard();

            var bytes = Encoding.Unicode.GetBytes(text + '\0');
            var hGlobal = GlobalAlloc(GMEM_MOVEABLE, (nuint)bytes.Length);
            if (hGlobal == 0)
                throw new OutOfMemoryException("Cannot allocate clipboard memory");

            var lpData = GlobalLock(hGlobal);
            if (lpData == 0)
                throw new InvalidOperationException("Cannot lock clipboard memory");

            try
            {
                Marshal.Copy(bytes, 0, lpData, bytes.Length);
            }
            finally
            {
                GlobalUnlock(hGlobal);
            }

            if (SetClipboardData(CF_UNICODETEXT, hGlobal) == 0)
                throw new InvalidOperationException("Cannot set clipboard data");
        }
        finally
        {
            CloseClipboard();
        }
    }
}

// ===== Linux Implementation (xclip/xsel) =====
internal static class LinuxClipboard
{
    public static string? GetText()
    {
        // Try xclip first, then xsel
        var (exitCode, output) = ProcessHelper.Run("xclip", "-selection clipboard -o");
        if (exitCode == 0)
            return output;

        (exitCode, output) = ProcessHelper.Run("xsel", "--clipboard --output");
        if (exitCode == 0)
            return output;

        throw new InvalidOperationException("Install xclip or xsel for clipboard support");
    }

    public static void SetText(string text)
    {
        // Try xclip first, then xsel
        var exitCode = ProcessHelper.RunWithInput("xclip", "-selection clipboard", text);
        if (exitCode == 0)
            return;

        exitCode = ProcessHelper.RunWithInput("xsel", "--clipboard --input", text);
        if (exitCode == 0)
            return;

        throw new InvalidOperationException("Install xclip or xsel for clipboard support");
    }
}

// ===== macOS Implementation (Native NSPasteboard) =====
internal static partial class MacOSClipboard
{
    // Objective-C runtime imports
    private const string ObjCRuntime = "/usr/lib/libobjc.A.dylib";
    private const string AppKit = "/System/Library/Frameworks/AppKit.framework/AppKit";

    [LibraryImport(ObjCRuntime, EntryPoint = "objc_getClass", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint objc_getClass(string name);

    [LibraryImport(ObjCRuntime, EntryPoint = "sel_registerName", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint sel_registerName(string name);

    // objc_msgSend overloads for different signatures
    [LibraryImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
    private static partial nint objc_msgSend(nint receiver, nint selector);

    [LibraryImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
    private static partial nint objc_msgSend(nint receiver, nint selector, nint arg1);

    [LibraryImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
    private static partial nint objc_msgSend(nint receiver, nint selector, nint arg1, nint arg2);

    [LibraryImport(ObjCRuntime, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool objc_msgSend_bool(nint receiver, nint selector, nint arg1, nint arg2);

    // NSString helpers
    [LibraryImport(ObjCRuntime, EntryPoint = "objc_msgSend", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint objc_msgSend_initWithUTF8String(nint receiver, nint selector, string str);

    // Cache selectors and classes for performance
    private static nint _nsPasteboardClass;
    private static nint _nsStringClass;
    private static nint _selGeneralPasteboard;
    private static nint _selStringForType;
    private static nint _selClearContents;
    private static nint _selSetStringForType;
    private static nint _selUTF8String;
    private static nint _selAlloc;
    private static nint _selInitWithUTF8String;
    private static nint _selRelease;
    private static nint _nsStringPboardType;

    private static void EnsureInitialized()
    {
        if (_nsPasteboardClass != 0)
            return;

        // Get classes
        _nsPasteboardClass = objc_getClass("NSPasteboard");
        _nsStringClass = objc_getClass("NSString");

        // Get selectors
        _selGeneralPasteboard = sel_registerName("generalPasteboard");
        _selStringForType = sel_registerName("stringForType:");
        _selClearContents = sel_registerName("clearContents");
        _selSetStringForType = sel_registerName("setString:forType:");
        _selUTF8String = sel_registerName("UTF8String");
        _selAlloc = sel_registerName("alloc");
        _selInitWithUTF8String = sel_registerName("initWithUTF8String:");
        _selRelease = sel_registerName("release");

        // Get NSPasteboardTypeString constant
        // This is defined as NSString* in AppKit, we need to load it
        _nsStringPboardType = GetNSPasteboardTypeString();
    }

    private static nint GetNSPasteboardTypeString()
    {
        // NSPasteboardTypeString is "public.utf8-plain-text"
        // We create an NSString with this value
        var alloc = objc_msgSend(_nsStringClass, _selAlloc);
        return objc_msgSend_initWithUTF8String(alloc, _selInitWithUTF8String, "public.utf8-plain-text");
    }

    private static nint CreateNSString(string text)
    {
        var alloc = objc_msgSend(_nsStringClass, _selAlloc);
        return objc_msgSend_initWithUTF8String(alloc, _selInitWithUTF8String, text);
    }

    private static string? NSStringToString(nint nsString)
    {
        if (nsString == 0)
            return null;

        var utf8Ptr = objc_msgSend(nsString, _selUTF8String);
        if (utf8Ptr == 0)
            return null;

        return Marshal.PtrToStringUTF8(utf8Ptr);
    }

    public static string? GetText()
    {
        EnsureInitialized();

        // Get general pasteboard: [NSPasteboard generalPasteboard]
        var pasteboard = objc_msgSend(_nsPasteboardClass, _selGeneralPasteboard);
        if (pasteboard == 0)
            return null;

        // Get string: [pasteboard stringForType:NSPasteboardTypeString]
        var nsString = objc_msgSend(pasteboard, _selStringForType, _nsStringPboardType);
        return NSStringToString(nsString);
    }

    public static void SetText(string text)
    {
        EnsureInitialized();

        // Get general pasteboard
        var pasteboard = objc_msgSend(_nsPasteboardClass, _selGeneralPasteboard);
        if (pasteboard == 0)
            throw new InvalidOperationException("Cannot get pasteboard");

        // Clear contents: [pasteboard clearContents]
        objc_msgSend(pasteboard, _selClearContents);

        // Create NSString from text
        var nsString = CreateNSString(text);
        if (nsString == 0)
            throw new InvalidOperationException("Cannot create NSString");

        try
        {
            // Set string: [pasteboard setString:nsString forType:NSPasteboardTypeString]
            var success = objc_msgSend_bool(pasteboard, _selSetStringForType, nsString, _nsStringPboardType);
            if (!success)
                throw new InvalidOperationException("Cannot set clipboard text");
        }
        finally
        {
            // Release the NSString we created
            objc_msgSend(nsString, _selRelease);
        }
    }
}

// ===== Process Helper =====
internal static class ProcessHelper
{
    public static (int exitCode, string output) Run(string fileName, string arguments)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return (process.ExitCode, output);
        }
        catch
        {
            return (-1, "");
        }
    }

    public static int RunWithInput(string fileName, string arguments, string input)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            using (var writer = new StreamWriter(process.StandardInput.BaseStream, new UTF8Encoding(false)))
            {
                writer.Write(input);
            }
            process.WaitForExit();

            return process.ExitCode;
        }
        catch
        {
            return -1;
        }
    }
}
