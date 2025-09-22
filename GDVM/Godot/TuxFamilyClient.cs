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
    private const string BaseUrl = "https://web.archive.org/web/20240927142429/https://downloads.tuxfamily.org/godotengine";

    // TODO: Replace with Task<Result<string, NetworkError>> GetSha512Async(Release godotRelease, CancellationToken cancellationToken)
    public async Task<string> GetSha512Async(Release godotRelease, CancellationToken cancellationToken)
    {
        var url = BuildUrl("SHA512-SUMS.txt", godotRelease);

        try
        {
            var response = await httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync(cancellationToken);
            }

            logger.LogError("{Url} returned {StatusCode}", url, response.StatusCode);
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
                return response;
            }

            logger.LogError("{Url} returned {StatusCode}", url, response.StatusCode);
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
        // stable is the root path on Tux Family
        var baseUrl = godotRelease.Type is null || godotRelease.Type == "stable"
            ? $"{BaseUrl}/{godotRelease.Version}"
            : $"{BaseUrl}/{godotRelease.Version}/{godotRelease.Type}";

        return godotRelease.RuntimeEnvironment == RuntimeEnvironment.Mono
            ? $"{baseUrl}/mono/{filename}"
            : $"{baseUrl}/{filename}";
    }
}
