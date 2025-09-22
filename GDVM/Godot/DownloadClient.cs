using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace GDVM.Godot;

public interface IDownloadClient
{
    Task<IEnumerable<string>> ListReleases(CancellationToken cancellationToken);
    Task<string> GetSha512(Release godotRelease, CancellationToken cancellationToken);
    Task<HttpResponseMessage> GetZipFile(string filename, Release godotRelease, CancellationToken cancellationToken);
}

/// <summary>
///     A download client that coordinates between GitHub and TuxFamily sources.
///     Uses GitHub as primary and TuxFamily as backup for improved reliability.
/// </summary>
public class DownloadClient(IGitHubClient gitHubClient, ITuxFamilyClient tuxFamilyClient, ILogger<DownloadClient> logger) : IDownloadClient
{
    // TODO: Replace with Task<Result<IEnumerable<string>, NetworkError>> ListReleases(CancellationToken cancellationToken)
    public async Task<IEnumerable<string>> ListReleases(CancellationToken cancellationToken)
    {
        try
        {
            return await gitHubClient.ListReleasesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list releases from GitHub");
            throw;
        }
    }

    // TODO: Replace with Task<Result<string, NetworkError>> GetSha512(Release godotRelease, CancellationToken cancellationToken)
    public async Task<string> GetSha512(Release godotRelease, CancellationToken cancellationToken)
    {
        // Try GitHub first
        try
        {
            var sha512 = await gitHubClient.GetSha512Async(godotRelease, cancellationToken);
            logger.LogInformation("Found SHA512 for {Version} at GitHub", godotRelease.Version);
            return sha512;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GitHub SHA512 failed for {Version}", godotRelease.Version);
        }

        // Fallback to TuxFamily
        try
        {
            var sha512 = await tuxFamilyClient.GetSha512Async(godotRelease, cancellationToken);
            logger.LogInformation("Found SHA512 for {Version} at TuxFamily", godotRelease.Version);
            return sha512;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TuxFamily SHA512 failed for {Version}", godotRelease.Version);
        }

        logger.LogError("SHA512-SUMS.txt was missing from all sources for {Version}", godotRelease.Version);
        throw new HttpRequestException("Wasn't able to download SHA512-SUMS.txt from any sources.");
    }

    // TODO: Replace with Task<Result<HttpResponseMessage, NetworkError>> GetZipFile(string filename, Release godotRelease, CancellationToken cancellationToken)
    public async Task<HttpResponseMessage> GetZipFile(string filename, Release godotRelease, CancellationToken cancellationToken)
    {
        // Try GitHub first
        try
        {
            var response = await gitHubClient.GetZipFileAsync(filename, godotRelease, cancellationToken);
            logger.LogInformation("Found {ReleaseNameWithRuntime} at GitHub", godotRelease.ReleaseNameWithRuntime);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GitHub zip file failed for {ReleaseNameWithRuntime}", godotRelease.ReleaseNameWithRuntime);
        }

        // Fallback to TuxFamily
        try
        {
            var response = await tuxFamilyClient.GetZipFileAsync(filename, godotRelease, cancellationToken);
            logger.LogInformation("Found {ReleaseNameWithRuntime} at TuxFamily", godotRelease.ReleaseNameWithRuntime);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TuxFamily zip file failed for {ReleaseNameWithRuntime}", godotRelease.ReleaseNameWithRuntime);
        }

        logger.LogError("{Filename} was missing from all sources for {ReleaseNameWithRuntime}", filename, godotRelease.ReleaseNameWithRuntime);
        throw new HttpRequestException($"Wasn't able to download {filename} from any sources.");
    }
}

/// <summary>
///     Just here to grab the release name from the list of builds, nothing more.
/// </summary>
public class GitHubReleaseAsset
{
    // trim `godot-` and `.json` to extract just the version and release type
    private const string Prefix = "godot-";
    private const string Suffix = ".json";
    public required string Name { get; set; }
    public string ReleaseName => Name[Prefix.Length..^Suffix.Length];
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(GitHubReleaseAsset))]
[JsonSerializable(typeof(List<GitHubReleaseAsset>))]
internal partial class GithubReleaseAssetContext : JsonSerializerContext;
