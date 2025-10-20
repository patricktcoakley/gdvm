using GDVM.Command;
using GDVM.Environment;
using GDVM.Godot;
using GDVM.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Spectre.Console.Testing;
using System.Text.Json;

namespace GDVM.Test.Command;

public sealed class SearchCommandTests
{
    private static readonly JsonSerializerOptions SerializerOptions = JsonView.Options;

    [Fact]
    public async Task SearchCommand_WritesJsonOutput()
    {
        var releases = new[] { "4.5-stable-standard", "4.5-stable-mono" };
        var releaseManager = new Mock<IReleaseManager>();
        releaseManager.Setup(x => x.SearchRemoteReleases(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(releases);

        var command = CreateCommand(releaseManager.Object, out var console);

        await command.SearchJson([], CancellationToken.None);

        var json = console.Output.Trim();
        Assert.False(string.IsNullOrWhiteSpace(json));

        var entries = JsonSerializer.Deserialize<List<RemoteReleaseView>>(json, SerializerOptions);
        Assert.NotNull(entries);
        Assert.Equal(releases.Length, entries.Count);
        Assert.Contains(entries, entry => entry.Name == "4.5-stable-standard");
    }

    [Fact]
    public async Task SearchCommand_WritesPanelOutput()
    {
        var releases = new[] { "4.5-stable-standard" };
        var releaseManager = new Mock<IReleaseManager>();
        releaseManager.Setup(x => x.SearchRemoteReleases(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(releases);

        var command = CreateCommand(releaseManager.Object, out var console);

        await command.Search([], CancellationToken.None);

        var output = console.Output;
        Assert.Contains("4.5-stable-standard", output);
        Assert.Contains("List Of Available Versions", output);
    }

    private static SearchCommand CreateCommand(IReleaseManager releaseManager, out TestConsole console)
    {
        var pathServiceMock = new Mock<IPathService>();
        var rootPath = Path.Combine(Path.GetTempPath(), "gdvm-search-tests");
        pathServiceMock.SetupGet(x => x.RootPath).Returns(rootPath);
        pathServiceMock.SetupGet(x => x.ConfigPath).Returns(Path.Combine(rootPath, "gdvm.ini"));
        pathServiceMock.SetupGet(x => x.ReleasesPath).Returns(Path.Combine(rootPath, ".releases"));
        pathServiceMock.SetupGet(x => x.BinPath).Returns(Path.Combine(rootPath, "bin"));
        pathServiceMock.SetupGet(x => x.SymlinkPath).Returns(Path.Combine(rootPath, "bin", "godot"));
        pathServiceMock.SetupGet(x => x.MacAppSymlinkPath).Returns(Path.Combine(rootPath, "bin", "Godot.app"));
        pathServiceMock.SetupGet(x => x.LogPath).Returns(Path.Combine(rootPath, ".log"));

        console = new TestConsole();
        var logger = NullLogger<SearchCommand>.Instance;
        return new SearchCommand(releaseManager, pathServiceMock.Object, console, logger);
    }
}
