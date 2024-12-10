using System.Runtime.InteropServices;

namespace GDVM.Environment;

public sealed class SystemInfo(OS currentOs, Architecture currentArch)
{
    public SystemInfo() : this(DetermineOS(), RuntimeInformation.ProcessArchitecture)
    {
        var isValidOS = CurrentOS is OS.Windows or OS.Linux or OS.MacOS;
        var isValidArch = CurrentArch is Architecture.X64 or Architecture.X86 or Architecture.Arm64 or Architecture.Arm;

        if (!isValidOS || !isValidArch)
        {
            throw new PlatformNotSupportedException(
                $"Platform combination not supported: OS={CurrentOS}, Architecture={CurrentArch}");
        }
    }

    public OS CurrentOS { get; } = currentOs;
    public Architecture CurrentArch { get; } = currentArch;

    private static OS DetermineOS()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return OS.Linux;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return OS.Windows;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return OS.MacOS;
        }

        // TODO: investigate FreeBSD support
        if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
        {
            return OS.FreeBSD;
        }

        return OS.Unknown;
    }
}
