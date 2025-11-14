using System.Runtime.InteropServices;

namespace GDVM.Environment;

/// <summary>
///     Gives us a constant value we can use for things like pattern matching where `OSPlatform` would not work
///     since it isn't available at compile time.
/// </summary>
/// <returns></returns>
public enum OS
{
    Windows,
    Linux,
    MacOS,
    FreeBSD,
    Unknown
}

public static class OSExtensions
{
    extension(OS os)
    {
        public string ToDisplayString() => os switch
        {
            OS.Windows => "Windows",
            OS.Linux => "Linux",
            OS.MacOS => "macOS",
            OS.FreeBSD => "FreeBSD",
            _ => "Unknown OS"
        };
    }

    extension(Architecture arch)
    {
        public string ToDisplayString() => arch switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "ARM64",
            Architecture.Arm => "ARM32",
            _ => arch.ToString()
        };
    }
}
