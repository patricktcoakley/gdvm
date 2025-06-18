using Microsoft.Extensions.Logging;
using Spectre.Console;
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
///     A download client that coordinates between GitHub and TuxFamily sources.
///     Uses GitHub as primary and TuxFamily as backup for improved reliability.
/// </summary>
public class DownloadClient(IGitHubClient gitHubClient, ITuxFamilyClient tuxFamilyClient, IAnsiConsole console, ILogger<DownloadClient> logger) : IDownloadClient
{
    public async Task<IEnumerable<string>> ListReleases(CancellationToken cancellationToken)
    {
        try
        {
            return await gitHubClient.ListReleasesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.ZLogError($"Failed to list releases from GitHub: {ex.Message}");
            throw;
        }
    }

    public async Task<string> GetSha512(Release godotRelease, CancellationToken cancellationToken)
    {
        // Try GitHub first
        try
        {
            var sha512 = await gitHubClient.GetSha512Async(godotRelease, cancellationToken);
            console.WriteLine($"Found SHA512 for {godotRelease.Version} at GitHub.");
            return sha512;
        }
        catch (Exception ex)
        {
            logger.ZLogError($"GitHub SHA512 failed for {godotRelease.Version}: {ex.Message}");
            console.WriteLine($"{godotRelease.Version} SHA512 was unavailable from GitHub, trying TuxFamily.");
        }

        // Fallback to TuxFamily
        try
        {
            var sha512 = await tuxFamilyClient.GetSha512Async(godotRelease, cancellationToken);
            console.WriteLine($"Found SHA512 for {godotRelease.Version} at TuxFamily.");
            return sha512;
        }
        catch (Exception ex)
        {
            logger.ZLogError($"TuxFamily SHA512 failed for {godotRelease.Version}: {ex.Message}");
            console.WriteLine($"{godotRelease.Version} SHA512 was unavailable from TuxFamily.");
        }

        logger.ZLogError($"SHA512-SUMS.txt was missing from all sources for {godotRelease.Version}.");
        throw new HttpRequestException("Wasn't able to download SHA512-SUMS.txt from any sources.");
    }

    public async Task<HttpResponseMessage> GetZipFile(string filename, Release godotRelease, CancellationToken cancellationToken)
    {
        // Try GitHub first
        try
        {
            var response = await gitHubClient.GetZipFileAsync(filename, godotRelease, cancellationToken);
            console.WriteLine($"Found {godotRelease.ReleaseNameWithRuntime} at GitHub.");
            return response;
        }
        catch (Exception ex)
        {
            logger.ZLogError($"GitHub zip file failed for {godotRelease.ReleaseNameWithRuntime}: {ex.Message}");
            console.WriteLine($"{godotRelease.ReleaseNameWithRuntime} was unavailable from GitHub, trying TuxFamily.");
        }

        // Fallback to TuxFamily
        try
        {
            var response = await tuxFamilyClient.GetZipFileAsync(filename, godotRelease, cancellationToken);
            console.WriteLine($"Found {godotRelease.ReleaseNameWithRuntime} at TuxFamily.");
            return response;
        }
        catch (Exception ex)
        {
            logger.ZLogError($"TuxFamily zip file failed for {godotRelease.ReleaseNameWithRuntime}: {ex.Message}");
            console.WriteLine($"{godotRelease.ReleaseNameWithRuntime} was unavailable from TuxFamily.");
        }

        logger.ZLogError($"{filename} was missing from all sources for {godotRelease.ReleaseNameWithRuntime}.");
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
