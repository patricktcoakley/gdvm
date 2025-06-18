using Moq;

namespace GDVM.Test.Godot.ReleaseManager;

using static TestData;

public class QueryTests
{
    [Theory]
    [MemberData(nameof(SearchReleaseTestCases), MemberType = typeof(TestData))]
    public async Task SearchReleases_Query_ReturnsExpectedResults(string[] query, string[] expected)
    {
        var releaseManager = new ReleaseManagerBuilder().Build();
        var result = await releaseManager.SearchRemoteReleases(query, CancellationToken.None);
        Assert.Equal(expected, result);
    }

    [InlineData(new[] { "latest" }, "4.2-stable-standard")]
    [InlineData(new[] { "latest", "mono" }, "4.2-stable-mono")]
    [InlineData(new[] { "latest", "standard" }, "4.2-stable-standard")]
    [InlineData(new[] { "latest", "rc" }, "4.1-rc2-standard")]
    [InlineData(new[] { "latest", "beta" }, "4.0-beta3-standard")]
    [Theory]
    public void FindReleaseByQuery_Latest_ReturnsCorrectVersion(string[] query, string expected)
    {
        var releaseManager = new ReleaseManagerBuilder().Build();
        var result = releaseManager.TryFindReleaseByQuery(query, TestReleases.ToArray());
        Assert.NotNull(result);
        Assert.Equal(expected, result.ReleaseNameWithRuntime);
    }


    [InlineData("latest")]
    [InlineData("latest", "standard")]
    [Theory]
    public void FindReleaseByQuery_Latest_ShouldReturnLatestStableWithStandard(params string[] query)
    {
        var releaseManager = new ReleaseManagerBuilder().Build();
        var result = releaseManager.TryFindReleaseByQuery(query, TestReleases.ToArray());

        Assert.NotNull(result);
        Assert.Equal("4.2-stable-standard", result.ReleaseNameWithRuntime);
    }

    [InlineData(new[] { "3.1" }, "3.1.2-rc1-standard")]
    [InlineData(new[] { "4.4" }, "4.4-dev5-standard")]
    [InlineData(new[] { "4" }, "4.2-stable-standard")]
    [InlineData(new[] { "4", "mono" }, "4.2-stable-mono")]
    [InlineData(new[] { "3" }, "3.5-stable-standard")]
    [InlineData(new[] { "4.2", "standard" }, "4.2-stable-standard")]
    [InlineData(new[] { "3.5", "standard" }, "3.5-stable-standard")]
    [InlineData(new[] { "standard", "4.2" }, "4.2-stable-standard")]
    [InlineData(new[] { "standard", "3.5" }, "3.5-stable-standard")]
    [InlineData(new[] { "4.2", "mono" }, "4.2-stable-mono")]
    [InlineData(new[] { "4", "stable" }, "4.2-stable-standard")]
    [InlineData(new[] { "4", "rc", "mono" }, "4.1-rc2-mono")]
    [InlineData(new[] { "rc", "mono" }, "4.1-rc2-mono")]
    [InlineData(new[] { "3.5", "mono" }, "3.5-stable-mono")]
    [InlineData(new[] { "4", "beta" }, "4.0-beta3-standard")]
    [InlineData(new[] { "3", "alpha4" }, "3.2-alpha4-standard")]
    [InlineData(new[] { "4.4", "mono", "dev5" }, "4.4-dev5-mono")]
    [InlineData(new[] { "alpha4" }, "3.2-alpha4-standard")]
    [Theory]
    public void FindReleaseByQuery_SpecificVersion_ShouldParseVersionCorrectly(string[] query, string expected)
    {
        var releaseManager = new ReleaseManagerBuilder().Build();
        var result = releaseManager.TryFindReleaseByQuery(query, TestReleases.ToArray());

        Assert.NotNull(result);
        Assert.Equal(expected, result.ReleaseNameWithRuntime);
    }

    [InlineData]
    [InlineData("5.1", "mono")]
    [InlineData("5.1", "standard")]
    [Theory]
    public void TryFindReleaseByQuery_InvalidVersion_ShouldReturnNull(params string[] query)
    {
        var releaseManager = new ReleaseManagerBuilder().Build();

        if (query.Length == 0)
        {
            Assert.Throws<ArgumentException>(() => releaseManager.TryFindReleaseByQuery(query, TestReleases.ToArray()));
        }
        else
        {
            Assert.Null(releaseManager.TryFindReleaseByQuery(query, TestReleases.ToArray()));
        }
    }


    [InlineData]
    [InlineData("latest", "mono", "standard")]
    [InlineData("latest", "stable", "rc")]
    [Theory]
    public void TryFindReleaseByQuery_InvalidLatestQuery_ShouldReturnNull(params string[] query)
    {
        var releaseManager = new ReleaseManagerBuilder().Build();

        if (query.Length == 0)
        {
            Assert.Throws<ArgumentException>(() => releaseManager.TryFindReleaseByQuery(query, TestReleases.ToArray()));
        }
        else
        {
            Assert.Null(releaseManager.TryFindReleaseByQuery(query, TestReleases.ToArray()));
        }
    }

    [Fact]
    public async Task SearchRemoteReleases_WithEmptyResults_ReturnsEmptyList()
    {
        var releaseManager = new ReleaseManagerBuilder()
            .WithReleases(new List<string>())
            .Build();

        var result = await releaseManager.SearchRemoteReleases(["stable"], CancellationToken.None);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchRemoteReleases_WithCancellation_ThrowsOperationCanceledException()
    {
        var cts = new CancellationTokenSource();
        var releaseManager = new ReleaseManagerBuilder()
            .ConfigureDownloadClient(mock =>
            {
                mock.Setup(x => x.ListReleases(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new OperationCanceledException());
            })
            .Build();

        await Assert.ThrowsAsync<OperationCanceledException>(() => releaseManager.SearchRemoteReleases(["stable"], cts.Token));
    }
}
