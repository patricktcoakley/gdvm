using Microsoft.Extensions.Logging;

namespace GDVM.Godot;

public interface ITuxFamilyClient
{
    Task<string> GetSha512Async(Release godotRelease, CancellationToken cancellationToken);
    Task<HttpResponseMessage> GetZipFileAsync(string filename, Release godotRelease, CancellationToken cancellationToken);
}

// Basically exists as a backup for checksums of older versions since GitHub doesn't seem to always have them.
// TuxFamily has had inconsistent connectivity and is seeminlgy down, so we just use the internet archive version.
public class TuxFamilyClient(HttpClient httpClient, ILogger<TuxFamilyClient> logger) : ITuxFamilyClient
{
    // Use a stable archive snapshot; the live endpoint is unreliable.
    private const string BaseUrl = "https://web.archive.org/web/20211106101031if_/https://downloads.tuxfamily.org/godotengine";

    // TODO: Replace with Task<Result<string, NetworkError>> GetSha512Async(Release godotRelease, CancellationToken cancellationToken)
    public async Task<string> GetSha512Async(Release godotRelease, CancellationToken cancellationToken)
    {
        var url = BuildUrl("SHA512-SUMS.txt", godotRelease);

        try
        {
            var response = await httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Found SHA512 for {Version} at {Url}", godotRelease.Version, url);
                return await response.Content.ReadAsStringAsync(cancellationToken);
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("{Url} returned {StatusCode}. Body: {Body}", url, response.StatusCode, body);
            throw new HttpRequestException($"TuxFamily SHA512 request failed: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get SHA512 from TuxFamily for {Version}", godotRelease.Version);
            throw;
        }
    }

    // TODO: Replace with Task<Result<HttpResponseMessage, NetworkError>> GetZipFileAsync(string filename, Release godotRelease, CancellationToken cancellationToken)
    public async Task<HttpResponseMessage> GetZipFileAsync(string filename, Release godotRelease, CancellationToken cancellationToken)
    {
        var url = BuildUrl(filename, godotRelease);

        try
        {
            var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Found {File} for {Release} at {Url}", filename, godotRelease.ReleaseNameWithRuntime, url);
                return response;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("{Url} returned {StatusCode}. Body: {Body}", url, response.StatusCode, body);
            throw new HttpRequestException($"TuxFamily zip file request failed: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get zip file from TuxFamily for {ReleaseNameWithRuntime}", godotRelease.ReleaseNameWithRuntime);
            throw;
        }
    }

    private static string BuildUrl(string filename, Release godotRelease)
    {
        var basePath = godotRelease.Type is null || godotRelease.Type == "stable"
            ? $"{BaseUrl}/{godotRelease.Version}"
            : $"{BaseUrl}/{godotRelease.Version}/{godotRelease.Type}";

        return godotRelease.RuntimeEnvironment == RuntimeEnvironment.Mono
            ? $"{basePath}/mono/{filename}"
            : $"{basePath}/{filename}";
    }
}
