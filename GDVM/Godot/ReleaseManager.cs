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
    string? FindCompatibleVersion(string projectVersion, bool isDotNet, IEnumerable<string> installedVersions);

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

        return
        [
            .. releaseNames
                .Where(x => x.StartsWith(possibleVersion, StringComparison.OrdinalIgnoreCase))
                .Where(x => x.Contains(releaseType, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x)
        ];
    }

    public Release? TryCreateRelease(string versionString)
    {
        var release = Release.TryParse(versionString);
        if (release == null)
        {
            return null;
        }

        var platformString = platformStringProvider.GetPlatformString(release);
        return release with
        {
            OS = hostSystem.SystemInfo.CurrentOS,
            PlatformString = platformString
        };
    }

    public string? FindCompatibleVersion(string projectVersion, bool isDotNet, IEnumerable<string> installedVersions)
    {
        var versions = installedVersions.ToList();

        if (versions.Count == 0)
        {
            return null;
        }

        var preferredRuntime = isDotNet ? "mono" : "standard";

        // First, try exact match
        var exactMatch = versions.FirstOrDefault(v => v == projectVersion);
        if (exactMatch != null)
        {
            return exactMatch;
        }

        // Parse all compatible releases and find the best match
        var compatibleReleases = versions
            .Select(TryCreateRelease)
            .Where(release => release != null)
            .Cast<Release>()
            .Where(release =>
            {
                // Check if this release matches our criteria
                var versionString = $"{release.Major}.{release.Minor}";
                bool isVersionMatch;

                if (projectVersion.Contains('.'))
                {
                    // Project version is like "4.3" - match exact major.minor
                    isVersionMatch = versionString == projectVersion;
                }
                else
                {
                    // Project version is like "4" - match major only
                    isVersionMatch = release.Major.ToString() == projectVersion;
                }

                return isVersionMatch &&
                       release.RuntimeEnvironment.ToString().Equals(preferredRuntime, StringComparison.CurrentCultureIgnoreCase);
            })
            .ToArray();

        if (compatibleReleases.Length == 0)
        {
            return null;
        }

        // Use OrderByDescending with the built-in Release.CompareTo for preference ordering
        // Now that ReleaseType.CompareTo prefers higher RC numbers, this gives us:
        // 1. Higher version numbers first (4.3 > 4.2)
        // 2. More stable releases first (stable > rc2 > rc1 > dev)
        var bestRelease = compatibleReleases
            .OrderByDescending(r => r)
            .First();

        return bestRelease.ReleaseNameWithRuntime;
    }


    private Release? TryFilterRelease(string[] query, string[] releaseNames)
    {
        var invalidArgs = ArgumentValidator.GetInvalidArguments(query);
        if (invalidArgs.Count > 0)
        {
            throw new ArgumentException(
                $"Invalid arguments: {string.Join(", ", invalidArgs)}. Valid arguments are version numbers (e.g. `4`, `4.2`), release types ( {string.Join(", ", ReleaseType.Prefixes.Select(p => $"`{p}`"))}), and runtime environments (`mono`, `standard`).");
        }

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

        // Try to find the first release that matches the criteria
        return releaseNames
            .Where(x => x.StartsWith(possibleVersion))
            .Where(x => string.IsNullOrEmpty(releaseType) || x.Contains(releaseType))
            .Select(releaseName => TryCreateRelease($"{releaseName}-{runtime.Name()}"))
            .OfType<Release>()
            .OrderByDescending(release => release)
            .FirstOrDefault();
    }


    private Release? TryFilterLatest(string type, string runtime, string[] releaseNames)
    {
        var version = releaseNames
            .OrderByDescending(x => x.Contains(type, StringComparison.InvariantCultureIgnoreCase))
            .FirstOrDefault();

        return TryCreateRelease($"{version}-{runtime}");
    }
}
