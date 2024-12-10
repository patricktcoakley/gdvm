using GDVM.Godot;

namespace GDVM.Test.Godot;

public class ReleaseTypeTests
{
    [Theory]
    [InlineData("stable", "stable", null)]
    [InlineData("rc1", "rc", 1)]
    [InlineData("beta2", "beta", 2)]
    [InlineData("alpha3", "alpha", 3)]
    [InlineData("dev4", "dev", 4)]
    [InlineData("beta10", "beta", 10)]
    [InlineData("4.3-rc9-mono", "rc", 9)]
    public void TryParse_ValidInput_ReturnsReleaseType(string input, string expectedType, int? expectedVersion)
    {
        var result = ReleaseType.TryParse(input);
        Assert.NotNull(result);
        Assert.Equal(expectedType, result.Value);
        Assert.Equal(expectedVersion, result.Version);
    }

    [Theory]
    [InlineData("stable1", null)]
    [InlineData("rc", null)]
    [InlineData("beta-", null)]
    [InlineData("alpha4x", null)]
    [InlineData("dev2.1", null)]
    [InlineData("gamma1", null)]
    [InlineData("", null)]
    [InlineData("rc-1", null)]
    [InlineData("beta2beta", null)]
    public void TryParse_InvalidInput_ReturnsNull(string input, ReleaseType? expected)
    {
        var result = ReleaseType.TryParse(input);
        Assert.Equal(expected, result);
    }
}
