using GDVM.Godot;
using System.Runtime.InteropServices;
using RuntimeEnvironment = GDVM.Godot.RuntimeEnvironment;

namespace GDVM.Environment;

public class PlatformStringProvider(SystemInfo systemInfo)
{
    /// <summary>
    ///     A messy but simple way to implement platform matrixing. Godot versions, prior to 3.3, differ in how they labeled
    ///     their release assets, so there are minor differences you have to account for, such as `osx` vs `macos` vs `osx.64` and the lack
    ///     of parity between platform support. At this point only a few are tested on actual hardware but they "should work" in theory.
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public string GetPlatformString(Release release)
    {
        return systemInfo.CurrentOS switch
        {
            OS.MacOS => GetMacOSPlatformString(release, systemInfo.CurrentArch),
            OS.Linux => GetLinuxPlatformString(release, systemInfo.CurrentArch),
            OS.Windows => GetWindowsPlatformString(release, systemInfo.CurrentArch),
            _ => throw new ArgumentException($"{systemInfo.CurrentOS} is not supported.")
        };
    }

    private static string GetMacOSPlatformString(Release release, Architecture arch)
    {
        return (release.RuntimeEnvironment, release.Major, arch) switch
        {
            (RuntimeEnvironment.Standard, 1, Architecture.X86) => "osx.32",

            (RuntimeEnvironment.Standard, 2, Architecture.X86) when release is { Minor: >= 0, Patch: >= 4 } => "osx32",
            (RuntimeEnvironment.Standard, 2, Architecture.X86 or Architecture.X64) when release is { Minor: >= 0, Patch: >= 4 } => "osx.fat",

            // Mono support started with v3
            (RuntimeEnvironment.Standard, 3, Architecture.X64) when release.Minor < 3 => "osx.64",
            (RuntimeEnvironment.Standard, 3, Architecture.Arm64 or Architecture.X64) when release.Minor >= 3 => "osx.universal",

            (RuntimeEnvironment.Mono, 3, Architecture.X64) when release.Minor == 0 => "mono_osx64",
            (RuntimeEnvironment.Mono, 3, Architecture.X64) when release.Minor <= 2 => "mono_osx.64",
            (RuntimeEnvironment.Mono, 3, Architecture.Arm64 or Architecture.X64) => "mono_osx.universal",

            (RuntimeEnvironment.Mono, 4, Architecture.Arm64 or Architecture.X64) => "mono_macos.universal",
            (RuntimeEnvironment.Standard, 4, Architecture.Arm64 or Architecture.X64) => "macos.universal",
            _ => throw new ArgumentOutOfRangeException(nameof(arch), $"Architecture `{arch}` is not supported on {release.ReleaseName} for macOS.")
        };
    }

    private static string GetLinuxPlatformString(Release release, Architecture arch)
    {
        return (release.RuntimeEnvironment, release.Major, arch) switch
        {
            (RuntimeEnvironment.Mono, 3, Architecture.X64) => "mono_x11_64",
            (RuntimeEnvironment.Mono, 3, Architecture.X86) => "mono_x11_32",

            (RuntimeEnvironment.Standard, 3, Architecture.X64) => "x11.64",
            (RuntimeEnvironment.Standard, 3, Architecture.X86) => "x11.32",
            (RuntimeEnvironment.Standard, 3, Architecture.Arm) => "linux.arm32",
            (RuntimeEnvironment.Standard, 3, Architecture.Arm64) => "linux.arm64",

            (RuntimeEnvironment.Mono, 4, Architecture.X64) => "mono_linux_x86_64",
            (RuntimeEnvironment.Mono, 4, Architecture.X86) => "mono_linux_x86_32",
            (RuntimeEnvironment.Mono, 4, Architecture.Arm) => "mono_linux_arm32",
            (RuntimeEnvironment.Mono, 4, Architecture.Arm64) => "mono_linux_arm64",

            (RuntimeEnvironment.Standard, 4, Architecture.X64) => "linux.x86_64",
            (RuntimeEnvironment.Standard, 4, Architecture.X86) => "linux.x86_32",
            (RuntimeEnvironment.Standard, 4, Architecture.Arm) => "linux.arm32",
            (RuntimeEnvironment.Standard, 4, Architecture.Arm64) => "linux.arm64",

            _ => throw new ArgumentOutOfRangeException(nameof(arch), $"{arch} is not supported on {release.ReleaseName} for Linux.")
        };
    }

    private static string GetWindowsPlatformString(Release release, Architecture arch)
    {
        return (release.RuntimeEnvironment, arch) switch
        {
            (RuntimeEnvironment.Mono, Architecture.X64) => "mono_win64",
            (RuntimeEnvironment.Mono, Architecture.X86) => "mono_win32",
            (RuntimeEnvironment.Mono, Architecture.Arm64) when release is { Major: 4, Minor: >= 3 } => "mono_windows_arm64",

            (RuntimeEnvironment.Standard, Architecture.X64) => "win64.exe",
            (RuntimeEnvironment.Standard, Architecture.X86) => "win32.exe",
            (RuntimeEnvironment.Standard, Architecture.Arm64) when release is { Major: 4, Minor: >= 3 } => "windows_arm64.exe",

            _ => throw new ArgumentOutOfRangeException(nameof(arch), $"{arch} is not supported on {release.ReleaseName} for Windows.")
        };
    }
}
