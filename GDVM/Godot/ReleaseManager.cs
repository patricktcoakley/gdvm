using GDVM.Environment;

namespace GDVM.Godot;

public interface IReleaseManager
{
    Task<IEnumerable<string>> ListReleases(CancellationToken cancellationToken);
    Task<IEnumerable<string>> SearchRemoteReleases(string[] query, CancellationToken cancellationToken);
    Task<string> GetSha512(Release release, CancellationToken cancellationToken);
    Task<HttpResponseMessage> GetZipFile(string filename, Release release, CancellationToken cancellationToken);

    Release? TryFindReleaseByQuery(string[] query, string[] releaseNames);
    IEnumerable<string> FilterReleasesByQuery(string[] query, string[] releaseNames);

    Release? TryCreateRelease(string versionString);
}

public sealed class ReleaseManager(IHostSystem hostSystem, PlatformStringProvider platformStringProvider, IDownloadClient downloadClient) : IReleaseManager
{
    public async Task<IEnumerable<string>> ListReleases(CancellationToken cancellationToken) =>
        await downloadClient.ListReleases(cancellationToken);

    public async Task<IEnumerable<string>> SearchRemoteReleases(string[] query, CancellationToken cancellationToken)
    {
        var releaseNames = await ListReleases(cancellationToken);
        return FilterReleasesByQuery(query, releaseNames.ToArray());
    }

    public async Task<string> GetSha512(Release release, CancellationToken cancellationToken) =>
        await downloadClient.GetSha512(release, cancellationToken);

    public async Task<HttpResponseMessage> GetZipFile(string filename, Release release, CancellationToken cancellationToken) =>
        await downloadClient.GetZipFile(filename, release, cancellationToken);

    /// <summary>
    ///     A way to handle the various argument possibilities for filtering releases by query
    /// </summary>
    /// <param name="query"></param>
    /// <param name="releaseNames"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public Release? TryFindReleaseByQuery(string[] query, string[] releaseNames)
    {
        return query switch
        {
            // We handle empty query at the call site
            [] => throw new ArgumentException("Empty query"),
            // Handle latest stable standard
            ["latest"] => TryFilterLatest("stable", "standard", releaseNames),
            // Handle latest stable by with runtime
            ["latest", var releaseType and ("mono" or "standard")] => TryFilterLatest("stable", releaseType, releaseNames),
            // Handle latest standard with release type
            ["latest", var releaseType] when ReleaseType.Prefixes.Contains(releaseType, StringComparer.OrdinalIgnoreCase)
                => TryFilterLatest(releaseType, "standard", releaseNames),
            // Handle latest with release type and runtime
            ["latest", var releaseType, var runtime] when ReleaseType.Prefixes.Contains(releaseType, StringComparer.OrdinalIgnoreCase) &&
                                                          runtime is "mono" or "standard"
                => TryFilterLatest(releaseType, runtime, releaseNames),
            // Explicit version, i.e. `4.2-stable(-mono)`
            _ => TryFilterRelease(query, releaseNames)
        };
    }

    public IEnumerable<string> FilterReleasesByQuery(string[] query, string[] releaseNames)
    {
        // No filters
        if (query.Length == 0)
        {
            return releaseNames;
        }

        // Default to no version filter
        var releaseType = query
            .FirstOrDefault(x => ReleaseType.Prefixes
                .Any(prefix => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)), string.Empty);

        // Default to no type filter
        var possibleVersion = query
            .Where(x => !x.Equals(releaseType, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault(string.Empty);

        return releaseNames
            .Where(x => x.StartsWith(possibleVersion, StringComparison.OrdinalIgnoreCase))
            .Where(x => x.Contains(releaseType, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x) // TODO: fix ordering by version suffix
            .ToArray();
    }

    public Release? TryCreateRelease(string versionString)
    {
        var release = Release.TryParse(versionString);
        if (release == null)
        {
            return null;
        }

        var platformString = platformStringProvider.GetPlatformString(release);
        release.OS = hostSystem.SystemInfo.CurrentOS;
        release.PlatformString = platformString;
        return release;
    }


    private Release? TryFilterRelease(string[] query, string[] releaseNames)
    {
        var runtime = query
            .FirstOrDefault(x => x is "mono" or "standard")?.ToLowerInvariant() == "mono"
            ? RuntimeEnvironment.Mono
            : RuntimeEnvironment.Standard;

        // Default to stable when release type isn't provided
        var releaseType = query
                              .FirstOrDefault(x => ReleaseType.Prefixes
                                  .Any(prefix => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                          ?? "";

        // Get the possible version query by filtering out the release type and runtime
        var possibleVersion = query
            .Except([runtime.Name(), releaseType], StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault("");

        if (possibleVersion.Length == 1 && releaseType.Length == 0)
        {
            releaseType = "stable";
        }

        // Try to find the first release
        var matchingRelease = releaseNames
            .OrderDescending()
            .Where(x => x.StartsWith(possibleVersion))
            .FirstOrDefault(x => x.Contains(releaseType));

        if (matchingRelease == null)
        {
            return null;
        }

        return TryCreateRelease($"{matchingRelease}-{runtime.Name()}");
    }

    private Release? TryFilterLatest(string type, string runtime, string[] releaseNames)
    {
        var version = releaseNames
            .OrderByDescending(x => x.Contains(type, StringComparison.InvariantCultureIgnoreCase))
            .FirstOrDefault();

        return TryCreateRelease($"{version}-{runtime}");
    }
}
