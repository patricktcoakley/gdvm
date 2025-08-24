namespace GDVM.Test.Godot.ReleaseManager;

using static TestData;

public class TryFindReleaseByQueryTests
{
    [Fact]
    public void TryFindReleaseByQuery_ValidArguments_DoesNotThrow()
    {
        var releaseManager = new ReleaseManagerBuilder().Build();
        string[][] testCases =
        [
            ["4", "stable", "mono"],
            ["4.2", "rc1"],
            ["4.2.1"],
            ["latest", "stable"],
            ["4.3-beta2", "standard"],
            ["mono"],
            ["standard"]
        ];

        foreach (var query in testCases)
        {
            var exception = Record.Exception(() =>
                releaseManager.TryFindReleaseByQuery(query, TestReleases.ToArray()));

            Assert.Null(exception);
        }
    }

    [Fact]
    public void TryFindReleaseByQuery_InvalidArguments_ThrowsArgumentException()
    {
        var releaseManager = new ReleaseManagerBuilder().Build();
        string[][] testCases =
        [
            ["4", "invalidarg"],
            ["4", "five", "xi"],
            ["invalidversion", "mono"],
            ["4.2", "unknowntype"],
            ["stable", "badruntime"]
        ];

        foreach (var query in testCases)
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                releaseManager.TryFindReleaseByQuery(query, TestReleases.ToArray()));

            Assert.Contains("Invalid arguments:", exception.Message);
        }
    }
}
