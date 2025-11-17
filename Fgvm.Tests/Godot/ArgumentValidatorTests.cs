using Fgvm.Godot;

namespace Fgvm.Tests.Godot;

public class ArgumentValidatorTests
{
    [Theory]
    [InlineData("4", true)]
    [InlineData("4.2", true)]
    [InlineData("4.2.1", true)]
    [InlineData("4.3-beta2", true)]
    [InlineData("mono", true)]
    [InlineData("standard", true)]
    [InlineData("latest", true)]
    [InlineData("stable", true)]
    [InlineData("rc1", true)]
    [InlineData("beta5", true)]
    [InlineData("alpha2", true)]
    [InlineData("dev3", true)]
    [InlineData("invalidarg", false)]
    [InlineData("five", false)]
    [InlineData("xi", false)]
    [InlineData("badruntime", false)]
    public void GetInvalidArguments_SingleArgument_ValidatesCorrectly(string arg, bool isValid)
    {
        var result = ArgumentValidator.GetInvalidArguments([arg]);

        if (isValid)
        {
            Assert.Empty(result);
        }
        else
        {
            Assert.Contains(arg, result);
        }
    }

    [Fact]
    public void GetInvalidArguments_MultipleArgs_ReturnsOnlyInvalidOnes()
    {
        var query = new[] { "4.2", "stable", "mono", "invalidarg", "five" };
        var result = ArgumentValidator.GetInvalidArguments(query);

        Assert.Equal(2, result.Count);
        Assert.Contains("invalidarg", result);
        Assert.Contains("five", result);
    }

    [Fact]
    public void GetInvalidArguments_AllValidArgs_ReturnsEmpty()
    {
        var query = new[] { "4.2", "stable", "mono" };
        var result = ArgumentValidator.GetInvalidArguments(query);

        Assert.Empty(result);
    }
}
