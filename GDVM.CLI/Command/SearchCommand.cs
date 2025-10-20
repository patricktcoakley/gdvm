using ConsoleAppFramework;
using GDVM.Environment;
using GDVM.Error;
using GDVM.Godot;
using GDVM.ViewModels;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Text.Json.Serialization;
using ZLogger;

namespace GDVM.Command;

public sealed class SearchCommand(
    IReleaseManager releaseManager,
    IPathService pathService,
    IAnsiConsole console,
    ILogger<SearchCommand> logger)
{
    /// <summary>
    ///     A remote search that takes an optional query and displays a filtered list of Godot releases available to download.
    /// </summary>
    /// <param name="query">Optional query arguments.</param>
    /// <param name="cancellationToken"></param>
    public async Task Search([Argument] string[]? query = null, CancellationToken cancellationToken = default) =>
        await SearchCore(query, false, cancellationToken);

    // NOTE: A hack around ConsoleAppFramework's issues with Argument and flags.
    [Command("search --json")]
    public async Task SearchJson([Argument] string[]? query = null, CancellationToken cancellationToken = default) =>
        await SearchCore(query, true, cancellationToken);

    private async Task SearchCore(string[]? query, bool json, CancellationToken cancellationToken)
    {
        try
        {
            var searchQuery = query ?? [];

            // Use SearchRemoteReleases for both empty and non-empty queries to get chronological sorting
            var filteredReleaseNames = await releaseManager.SearchRemoteReleases(searchQuery, cancellationToken);
            var releases = filteredReleaseNames
                .Select(name => new RemoteReleaseView(name))
                .ToList();

            if (json)
            {
                console.WriteLine(releases.ToJson());
                return;
            }

            console.Write(releases.ToPanel(searchQuery));
        }

        catch (Exception e)
        {
            logger.ZLogError($"Error searching releases: {e.Message}");
            console.MarkupLine(
                Messages.SomethingWentWrong("when trying to search releases", pathService)
            );

            throw;
        }
    }
}

internal readonly record struct RemoteReleaseView([property: JsonPropertyName("name")] string Name) : IJsonView<RemoteReleaseView>;

internal static class RemoteReleaseViewExtensions
{
    public static Panel ToPanel(this IReadOnlyList<RemoteReleaseView> releases, IReadOnlyList<string> query)
    {
        var content = releases.Count > 0
            ? string.Join("\n", releases.Select(r => r.Name))
            : "[dim]No releases found.[/]";

        var header = query.Count == 0
            ? Messages.AvailableVersionsHeader
            : "[green]List Of Available Versions[/]";

        return new Panel(content)
        {
            Header = new PanelHeader(header),
            Width = 40
        };
    }
}
