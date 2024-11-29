using GDVM.Godot;
using RuntimeEnvironment = GDVM.Godot.RuntimeEnvironment;

namespace GDVM.Test.Godot;

public class ReleaseTests
{
    [Theory]
    [InlineData("4.2-stable", 4, 2, null, "stable", RuntimeEnvironment.Standard)]
    [InlineData("4.2.1-stable", 4, 2, 1, "stable", RuntimeEnvironment.Standard)]
    [InlineData("4.2-rc1", 4, 2, null, "rc1", RuntimeEnvironment.Standard)]
    [InlineData("3.6.1-beta2", 3, 6, 1, "beta2", RuntimeEnvironment.Standard)]
    [InlineData("4.2-dev3", 4, 2, null, "dev3", RuntimeEnvironment.Standard)]
    [InlineData("4.2-stable-mono", 4, 2, null, "stable", RuntimeEnvironment.Mono)]
    [InlineData("3.5.1-alpha1-mono", 3, 5, 1, "alpha1", RuntimeEnvironment.Mono)]
    [InlineData("4.3.1-rc4-mono", 4, 3, 1, "rc4", RuntimeEnvironment.Mono)]
    public void TryParse_ValidVersions_ReturnsTrue(
        string input,
        int expectedMajor,
        int expectedMinor,
        int? expectedPatch,
        string expectedReleaseType,
        RuntimeEnvironment expectedRuntimeEnvironment)
    {
        var release = Release.TryParse(input);

        Assert.NotNull(release);
        Assert.Equal(expectedMajor, release.Major);
        Assert.Equal(expectedMinor, release.Minor);
        Assert.Equal(expectedPatch, release.Patch);
        Assert.Equal(expectedReleaseType, release.Type);
        Assert.Equal(expectedRuntimeEnvironment, release.RuntimeEnvironment);
    }

    [Theory]
    [InlineData("")]
    [InlineData("4")]
    [InlineData("4.x")]
    [InlineData("invalid")]
    [InlineData("4.2-invalid")]
    public void TryParse_InvalidVersions_ReturnsFalse(string input)
    {
        var release = Release.TryParse(input);

        Assert.Null(release);
    }

    [Theory]
    [InlineData("4.2-stable")]
    [InlineData("4.2-stable-mono")]
    public void TryParse_WithRuntimeOverride_UsesProvidedRuntime(string input)
    {
        var release = Release.TryParse(input);
        var expectedRuntime = input.Contains("mono") ? RuntimeEnvironment.Mono : RuntimeEnvironment.Standard;
        Assert.NotNull(release);
        Assert.Equal(expectedRuntime, release.RuntimeEnvironment);
    }
}
