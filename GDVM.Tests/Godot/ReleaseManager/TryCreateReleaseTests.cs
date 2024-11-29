using GDVM.Environment;
using GDVM.Godot;
using System.Runtime.InteropServices;
using RuntimeEnvironment = GDVM.Godot.RuntimeEnvironment;

namespace GDVM.Test.Godot.ReleaseManager;

using static TestData;

public class TryCreateReleaseTests
{
    [Theory]
    [MemberData(nameof(GetExecNameTestData), MemberType = typeof(TestData))]
    public void ExecName_ShouldReturnTheCorrectExecutable(
        OS os,
        Architecture arch,
        string versionInfo,
        string expectedExecName)
    {
        var releaseManager = new ReleaseManagerBuilder()
            .WithOSAndArch(os, arch)
            .Build();

        var release = releaseManager.TryCreateRelease(versionInfo);

        Assert.NotNull(release);
        Assert.Equal(expectedExecName, release.ExecName);
    }

    [Theory]
    [MemberData(nameof(DetermineFullNameTestData), MemberType = typeof(TestData))]
    public void Release_FileName_ReturnsExpectedFileName(string version, RuntimeEnvironment runtimeEnvironment, OS os, Architecture arch, string expected)
    {
        var releaseManager = new ReleaseManagerBuilder()
            .WithOSAndArch(os, arch)
            .Build();

        var release = releaseManager.TryCreateRelease($"{version}-{runtimeEnvironment.Name()}");

        Assert.NotNull(release);
        Assert.Equal(runtimeEnvironment, release.RuntimeEnvironment);
        Assert.Equal(expected, release.FileName);
    }

    [Theory]
    [MemberData(nameof(GetReleasePropertyTestData), MemberType = typeof(TestData))]
    public void Release_Properties_ShouldBeCorrectlySet(
        OS os,
        Architecture arch,
        string versionString,
        RuntimeEnvironment expectedRuntime,
        string expectedExecName,
        string expectedFileName)
    {
        var releaseManager = new ReleaseManagerBuilder()
            .WithOSAndArch(os, arch)
            .Build();

        var release = releaseManager.TryCreateRelease(versionString);

        Assert.NotNull(release);
        Assert.Equal(expectedRuntime, release.RuntimeEnvironment);
        Assert.Equal(expectedExecName, release.ExecName);
        Assert.Equal(expectedFileName, release.ZipFileName);
    }
}
