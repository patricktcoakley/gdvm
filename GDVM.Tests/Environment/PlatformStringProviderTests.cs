using GDVM.Environment;
using GDVM.Godot;
using System.Runtime.InteropServices;
using RuntimeEnvironment = GDVM.Godot.RuntimeEnvironment;

namespace GDVM.Test.Environment;

public class PlatformStringProviderTests
{
    [Theory]
    [InlineData(RuntimeEnvironment.Standard, 1, Architecture.X86, "osx.32")]
    [InlineData(RuntimeEnvironment.Standard, 2, Architecture.X86, "osx32", 0, 4)]
    [InlineData(RuntimeEnvironment.Standard, 2, Architecture.X64, "osx.fat", 0, 4)]
    [InlineData(RuntimeEnvironment.Standard, 3, Architecture.X64, "osx.64", 2)]
    [InlineData(RuntimeEnvironment.Standard, 3, Architecture.Arm64, "osx.universal", 3)]
    [InlineData(RuntimeEnvironment.Mono, 3, Architecture.X64, "mono_osx64", 0)]
    [InlineData(RuntimeEnvironment.Mono, 3, Architecture.X64, "mono_osx.64", 2)]
    [InlineData(RuntimeEnvironment.Mono, 3, Architecture.Arm64, "mono_osx.universal", 5)]
    [InlineData(RuntimeEnvironment.Mono, 3, Architecture.Arm, null)] // Should throw
    [InlineData(RuntimeEnvironment.Mono, 4, Architecture.Arm64, "mono_macos.universal")]
    [InlineData(RuntimeEnvironment.Standard, 4, Architecture.Arm64, "macos.universal")]
    public void MacOS_ShouldReturnCorrectPlatformString(
        RuntimeEnvironment runtimeEnvironment,
        int major,
        Architecture arch,
        string? expected,
        int minor = 0,
        int? patch = null)
    {
        var platformStringProvider = new PlatformStringProvider(new SystemInfo(OS.MacOS, arch));
        var release = new Release(major, minor, patch: patch, runtimeEnvironment: runtimeEnvironment);

        if (expected is null)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                platformStringProvider.GetPlatformString(release));

            return;
        }

        var result = platformStringProvider.GetPlatformString(release);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(RuntimeEnvironment.Mono, 3, Architecture.X64, "mono_x11_64")]
    [InlineData(RuntimeEnvironment.Mono, 3, Architecture.X86, "mono_x11_32")]
    [InlineData(RuntimeEnvironment.Mono, 3, Architecture.Arm, null)] // Should throw
    [InlineData(RuntimeEnvironment.Mono, 3, Architecture.Arm64, null)] // Should throw
    [InlineData(RuntimeEnvironment.Standard, 3, Architecture.X64, "x11.64")]
    [InlineData(RuntimeEnvironment.Standard, 3, Architecture.Arm, "linux.arm32")]
    [InlineData(RuntimeEnvironment.Standard, 3, Architecture.Arm64, "linux.arm64")]
    [InlineData(RuntimeEnvironment.Mono, 4, Architecture.X64, "mono_linux_x86_64")]
    [InlineData(RuntimeEnvironment.Mono, 4, Architecture.Arm, "mono_linux_arm32")]
    [InlineData(RuntimeEnvironment.Mono, 4, Architecture.Arm64, "mono_linux_arm64")]
    [InlineData(RuntimeEnvironment.Standard, 4, Architecture.Arm64, "linux.arm64")]
    public void Linux_ShouldReturnCorrectPlatformString(
        RuntimeEnvironment runtime,
        int major,
        Architecture arch,
        string? expected)
    {
        var platformStringProvider = new PlatformStringProvider(new SystemInfo(OS.Linux, arch));
        var release = new Release(major, 0, runtimeEnvironment: runtime);

        if (expected is null)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                platformStringProvider.GetPlatformString(release));

            return;
        }

        var result = platformStringProvider.GetPlatformString(release);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(RuntimeEnvironment.Mono, Architecture.X64, "mono_win64")]
    [InlineData(RuntimeEnvironment.Mono, Architecture.X86, "mono_win32")]
    [InlineData(RuntimeEnvironment.Standard, Architecture.X64, "win64.exe")]
    [InlineData(RuntimeEnvironment.Standard, Architecture.X86, "win32.exe")]
    [InlineData(RuntimeEnvironment.Mono, Architecture.Arm64, "mono_windows_arm64", 4, 3)]
    [InlineData(RuntimeEnvironment.Standard, Architecture.Arm64, "windows_arm64.exe", 4, 3)]
    [InlineData(RuntimeEnvironment.Mono, Architecture.Arm64, null, 3, 0)]
    [InlineData(RuntimeEnvironment.Standard, Architecture.Arm64, null, 3, 0)]
    [InlineData(RuntimeEnvironment.Mono, Architecture.Arm64, null, 4, 2)]
    [InlineData(RuntimeEnvironment.Standard, Architecture.Arm64, null, 4, 2)]
    public void Windows_ShouldReturnCorrectPlatformString(
        RuntimeEnvironment runtime,
        Architecture arch,
        string? expected,
        int major = 3,
        int minor = 0)
    {
        var platformStringProvider = new PlatformStringProvider(new SystemInfo(OS.Windows, arch));
        var release = new Release(major, minor, runtimeEnvironment: runtime);

        if (expected is null)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                platformStringProvider.GetPlatformString(release));

            return;
        }

        var result = platformStringProvider.GetPlatformString(release);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(OS.Unknown)]
    [InlineData(OS.FreeBSD)]
    public void UnsupportedOS_ShouldThrowNotSupportedException(OS os)
    {
        var platformStringProvider = new PlatformStringProvider(new SystemInfo(os, Architecture.X64));
        var release = new Release(4, 3);
        Assert.Throws<ArgumentException>(() => platformStringProvider.GetPlatformString(release));
    }
}
