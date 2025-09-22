using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;

namespace GDVM.Godot;

public interface IGitHubClient
{
    Task<IEnumerable<string>> ListReleasesAsync(CancellationToken cancellationToken);
    Task<string> GetSha512Async(Release godotRelease, CancellationToken cancellationToken);
    Task<HttpResponseMessage> GetZipFileAsync(string filename, Release godotRelease, CancellationToken cancellationToken);
}

public class GitHubClient : IGitHubClient
{
    private const string BaseUrl = "https://github.com/godotengine/godot-builds/releases/download";
    private const string ApiUrl = "https://api.github.com/repos/godotengine/godot-builds/contents/releases";
    private readonly IConfiguration _configuration;

    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubClient> _logger;

    public GitHubClient(HttpClient httpClient, IConfiguration configuration, ILogger<GitHubClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        // Configure the HttpClient for GitHub API
        ConfigureHttpClient();
    }

    private string? GitHubToken => _configuration["github:token"];

    // TODO: Replace with Task<Result<IEnumerable<string>, NetworkError>> ListReleasesAsync(CancellationToken cancellationToken)
    public async Task<IEnumerable<string>> ListReleasesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(ApiUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("{ApiUrl} returned {StatusCode}", ApiUrl, response.StatusCode);
                throw new HttpRequestException($"{ApiUrl} returned {response.StatusCode}.");
            }

            var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
            var releases = JsonSerializer.Deserialize(jsonString, GithubReleaseAssetContext.Default.ListGitHubReleaseAsset) ?? [];
            releases.Reverse();
            return releases.Select<GitHubReleaseAsset, string>(r => r.ReleaseName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list releases from GitHub");
            throw;
        }
    }

    // TODO: Replace with Task<Result<string, NetworkError>> GetSha512Async(Release godotRelease, CancellationToken cancellationToken)
    public async Task<string> GetSha512Async(Release godotRelease, CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}/{godotRelease.ReleaseName}/SHA512-SUMS.txt";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync(cancellationToken);
            }

            _logger.LogError("{Url} returned {StatusCode}", url, response.StatusCode);
            throw new HttpRequestException($"GitHub SHA512 request failed: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SHA512 from GitHub for {Version}", godotRelease.Version);
            throw;
        }
    }

    // TODO: Replace with Task<Result<HttpResponseMessage, NetworkError>> GetZipFileAsync(string filename, Release godotRelease, CancellationToken cancellationToken)
    public async Task<HttpResponseMessage> GetZipFileAsync(string filename, Release godotRelease, CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}/{godotRelease.ReleaseName}/{filename}";

        try
        {
            var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            _logger.LogError("{Url} returned {StatusCode}", url, response.StatusCode);
            throw new HttpRequestException($"GitHub zip file request failed: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get zip file from GitHub for {ReleaseNameWithRuntime}", godotRelease.ReleaseNameWithRuntime);
            throw;
        }
    }

    private void ConfigureHttpClient()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.Clear();
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("gdvm", null));

        if (GitHubToken != null)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GitHubToken);
        }
    }
}
