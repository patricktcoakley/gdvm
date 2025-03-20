using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using ZLogger;

namespace GDVM.Godot;

public interface IDownloadClient
{
    Task<IEnumerable<string>> ListReleases(CancellationToken cancellationToken);
    Task<string> GetSha512(Release godotRelease, CancellationToken cancellationToken);
    Task<HttpResponseMessage> GetZipFile(string filename, Release godotRelease, CancellationToken cancellationToken);
}

/// <summary>
///     A very simple client to grab the list of releases and download various files of a particular release.
///     Currently, it uses GitHub as the primary and TuxFamily as the backup, as TuxFamily is generally slower
///     but often has SHA512-SUMS.txt for releases GitHub doesn't.
/// </summary>
public class DownloadClient : IDownloadClient
{
    private const string _githubReleaseContentsUrl = "https://api.github.com/repos/godotengine/godot-builds/contents/releases";

    private const string _githubReleasesUrl = "https://github.com/godotengine/godot-builds/releases/download";

    // Tux Family has a tendency to go down; its main purpose is to procure the checksums for old releases so we don't need the latest
    private const string _tuxFamilyUrl = "https://web.archive.org/web/20240927142429/https://downloads.tuxfamily.org/godotengine";

    private static readonly HttpClient _httpClient = new();


    private readonly IConfiguration _configuration;
    private readonly ILogger<DownloadClient> _logger;

    /// <summary>
    ///     A very simple client to grab the list of releases and download various files of a particular release.
    ///     Currently, it uses GitHub as the primary and TuxFamily as the backup, as TuxFamily is generally slower
    ///     but often has SHA512-SUMS.txt for releases GitHub doesn't.
    /// </summary>
    public DownloadClient(IConfiguration configuration, ILogger<DownloadClient> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private string? _githubToken => _configuration["github:token"];

    // for now coupled to GitHub
    public async Task<IEnumerable<string>> ListReleases(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _githubReleaseContentsUrl);

        SetGitHubAuthorizationHeader(request);

        var resp = await _httpClient.SendAsync(request, cancellationToken);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.ZLogError($"{_githubReleaseContentsUrl} returned {resp.StatusCode}.");
            throw new HttpRequestException($"{_githubReleaseContentsUrl} returned {resp.StatusCode}.");
        }

        var jsonString = await resp.Content.ReadAsStringAsync(cancellationToken);
        var releases = JsonSerializer.Deserialize(jsonString, GithubReleaseAssetContext.Default.ListGitHubReleaseAsset) ?? [];
        releases.Reverse();
        return releases.Select<GitHubReleaseAsset, string>(r => r.ReleaseName);
    }

    public async Task<string> GetSha512(Release godotRelease, CancellationToken cancellationToken)
    {
        string[] urls =
        [
            GitHubUrlPattern("SHA512-SUMS.txt", godotRelease),
            TuxFamilyUrlPattern("SHA512-SUMS.txt", godotRelease)
        ];

        foreach (var url in urls)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            // TODO: Refactor this to a typed client
            if (url.StartsWith(_githubReleasesUrl))
            {
                SetGitHubAuthorizationHeader(request);
            }

            var resp = await _httpClient.SendAsync(request, cancellationToken);
            if (resp.IsSuccessStatusCode)
            {
                AnsiConsole.WriteLine($"Found SHA512 for {godotRelease.Version} at {url}.");
                return await resp.Content.ReadAsStringAsync(cancellationToken);
            }

            _logger.ZLogError($"{url} returned {resp.StatusCode}.");
            AnsiConsole.WriteLine($"{godotRelease.Version} at {url} was unavailable, trying another source.");
        }


        _logger.ZLogError($"SHA512-SUMS.txt was missing from all sources.");
        throw new HttpRequestException("Wasn't able to download SHA512-SUMS.txt from any sources.");
    }

    public async Task<HttpResponseMessage> GetZipFile(string filename, Release godotRelease, CancellationToken cancellationToken)
    {
        string[] urls =
        [
            GitHubUrlPattern(filename, godotRelease),
            TuxFamilyUrlPattern(filename, godotRelease)
        ];

        foreach (var url in urls)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            // TODO: Refactor this to a typed client
            if (url.StartsWith(_githubReleasesUrl))
            {
                SetGitHubAuthorizationHeader(request);
            }

            var resp = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (resp.IsSuccessStatusCode)
            {
                AnsiConsole.WriteLine($"Found {godotRelease.ReleaseNameWithRuntime} at {url}.");
                return resp;
            }

            _logger.ZLogError($"{url} returned {resp.StatusCode}.");
            AnsiConsole.WriteLine($"{godotRelease.ReleaseNameWithRuntime} at {url} was unavailable, trying another source.");
        }

        _logger.ZLogError($"{filename} was missing from all sources..");
        throw new HttpRequestException($"Wasn't able to download {filename} from any sources.");
    }

    // TODO: Refactor this to a typed client
    // Lazy way to set the headers
    private void SetGitHubAuthorizationHeader(HttpRequestMessage request)
    {
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("gdvm", null));
        if (_githubToken != null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _githubToken);
        }
    }

    private static string GitHubUrlPattern(string filename, Release godotRelease) => $"{_githubReleasesUrl}/{godotRelease.ReleaseName}/{filename}";

    private static string TuxFamilyUrlPattern(string filename, Release godotRelease)
    {
        // stable is the root path on Tux Family
        var baseUrl = godotRelease.Type is null || godotRelease.Type == "stable"
            ? $"{_tuxFamilyUrl}/{godotRelease.Version}"
            : $"{_tuxFamilyUrl}/{godotRelease.Version}/{godotRelease.Type}";

        return godotRelease.RuntimeEnvironment == RuntimeEnvironment.Mono ? $"{baseUrl}/mono/{filename}" : $"{baseUrl}/{filename}";
    }
}

/// <summary>
///     Just here to grab the release name from the list of builds, nothing more.
/// </summary>
public class GitHubReleaseAsset
{
    public required string Name { get; set; }
    // trim `godot-` and `.json` to extract just the version and release type
    public string ReleaseName => Name[6..^5];
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(GitHubReleaseAsset))]
[JsonSerializable(typeof(List<GitHubReleaseAsset>))]
internal partial class GithubReleaseAssetContext : JsonSerializerContext;
