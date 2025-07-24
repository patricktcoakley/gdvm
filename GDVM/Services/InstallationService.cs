using GDVM.Environment;
using GDVM.Error;
using GDVM.Extensions;
using GDVM.Godot;
using GDVM.Progress;
using GDVM.Types;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Security;
using System.Security.Cryptography;

namespace GDVM.Services;

/// <summary>
///     Represents the different stages of a Godot installation process
/// </summary>
public enum InstallationStage
{
    /// <summary>Preparing for installation</summary>
    Initializing,
    /// <summary>Downloading the release archive</summary>
    Downloading,
    /// <summary>Verifying the downloaded file's checksum</summary>
    VerifyingChecksum,
    /// <summary>Extracting files from the archive</summary>
    Extracting,
    /// <summary>Setting the installed version as default</summary>
    SettingDefault
}

public interface IInstallationService
{
    Task<Result<InstallationOutcome, InstallationError>> InstallReleaseAsync(Release godotRelease, 
        IProgress<OperationProgress<InstallationStage>> progress, bool setAsDefault = true,
        CancellationToken cancellationToken = default);

    Task<Result<InstallationOutcome, InstallationError>> InstallByQueryAsync(string[] query, 
        IProgress<OperationProgress<InstallationStage>> progress, bool setAsDefault = false,
        CancellationToken cancellationToken = default);

    Task<string[]> FetchReleaseNames(CancellationToken cancellationToken, bool remote = false);
}

public class InstallationService(
    IHostSystem hostSystem,
    IReleaseManager releaseManager,
    IPathService pathService,
    ILogger<InstallationService> logger)
    : IInstallationService
{
    /// <summary>
    ///     Tries to install a Godot release.
    /// </summary>
    /// <param name="godotRelease">The release to install</param>
    /// <param name="progress">Progress reporter for installation updates</param>
    /// <param name="setAsDefault">Whether to set this version as the global default</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The installation result if successful, null if the installation failed.</returns>
    public async Task<Result<InstallationOutcome, InstallationError>> InstallReleaseAsync(Release godotRelease, 
        IProgress<OperationProgress<InstallationStage>> progress, bool setAsDefault = true,
        CancellationToken cancellationToken = default)
    {
        var extractPath = "";

        try
        {
            var installPathBase = godotRelease.ReleaseNameWithRuntime;

            progress.Report(new OperationProgress<InstallationStage>(InstallationStage.Initializing, $"Initializing installation of {installPathBase}..."));

            // Check if already installed
            var existingPath = Path.Combine(pathService.RootPath, installPathBase);
            if (Directory.Exists(existingPath))
            {
                logger.LogInformation("Version {InstallPathBase} is already installed", installPathBase);
                return new Result<InstallationOutcome, InstallationError>.Success(
                    new InstallationOutcome.AlreadyInstalled(godotRelease.ReleaseNameWithRuntime));
            }

            // Show progress immediately before HTTP request
            progress.Report(new OperationProgress<InstallationStage>(InstallationStage.Downloading, $"Downloading {installPathBase}..."));

            const int bufferSize = 32768;
            using var response = await releaseManager.GetZipFile(godotRelease.ZipFileName, godotRelease, cancellationToken);

            var contentLength = response.Content.Headers.ContentLength ?? 0;

            await using var memStream = new MemoryStream(checked((int)contentLength));

            // Download the release with dedicated progress context
            cancellationToken.ThrowIfCancellationRequested();

            await using var networkStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            var buffer = new byte[bufferSize];
            int bytesRead;
            var totalDownloaded = 0L;
            var lastProgressUpdate = 0L;
            var startTime = DateTime.UtcNow;

            while ((bytesRead = await networkStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await memStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalDownloaded += bytesRead;

                // Update progress every 1MB or when finished
                if (contentLength > 0 && (totalDownloaded - lastProgressUpdate >= 1024 * 1024 || totalDownloaded == contentLength))
                {
                    var downloadedMB = totalDownloaded / 1024.0 / 1024.0;
                    var totalMB = contentLength / 1024.0 / 1024.0;

                    // Calculate download speed
                    var elapsedSeconds = (DateTime.UtcNow - startTime).TotalSeconds;
                    var speedText = "";
                    if (elapsedSeconds > 0.5)
                    {
                        var speedMBps = downloadedMB / elapsedSeconds;
                        speedText = speedMBps >= 1.0
                            ? $" • {speedMBps:F1} MB/s"
                            : $" • {speedMBps * 1024:F0} KB/s";
                    }

                    progress.Report(new OperationProgress<InstallationStage>(InstallationStage.Downloading,
                        $"Downloading {installPathBase} • {downloadedMB:F1}/{totalMB:F1} MB{speedText}"));

                    lastProgressUpdate = totalDownloaded;
                }
            }

            // Verify checksum if available
            if (godotRelease.Major > 3 || godotRelease is { Major: 3, Minor: >= 3 })
            {
                progress.Report(new OperationProgress<InstallationStage>(InstallationStage.VerifyingChecksum, "Verifying checksum..."));
                memStream.Position = 0;
                var sha512String = await releaseManager.GetSha512(godotRelease, cancellationToken);
                var calculatedChecksum = await CalculateChecksum(memStream, cancellationToken);

                if (TryParseSha512SumsContent(godotRelease.ZipFileName, sha512String) is not { } expectedHash)
                {
                    throw new CryptographicException($"Unable to Parse {sha512String} or find expected hash.");
                }

                if (!calculatedChecksum.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    throw new SecurityException(
                        $"Unexpected results for {godotRelease.ZipFileName} checksum. Expected hash: {expectedHash}, computed hash: {calculatedChecksum}.");
                }
            }

            progress.Report(new OperationProgress<InstallationStage>(InstallationStage.Extracting, "Extracting files..."));
            memStream.Position = 0;
            extractPath = Path.Combine(pathService.RootPath, installPathBase);

            using var archive = new ZipArchive(memStream, ZipArchiveMode.Read);
            archive.ExtractWithFlatteningSupport(extractPath, true);

            if (setAsDefault)
            {
                progress.Report(new OperationProgress<InstallationStage>(InstallationStage.SettingDefault, "Setting as default version..."));
                var symlinkTargetPath = Path.Combine(extractPath, godotRelease.ExecName);
                hostSystem.CreateOrOverwriteSymbolicLink(symlinkTargetPath);
            }

            logger.LogInformation("Successfully installed {ReleaseNameWithRuntime}", godotRelease.ReleaseNameWithRuntime);
            return new Result<InstallationOutcome, InstallationError>.Success(
                new InstallationOutcome.NewInstallation(godotRelease.ReleaseNameWithRuntime));
        }
        catch (TaskCanceledException)
        {
            logger.LogError("User cancelled installation");
            throw;
        }
        catch (InvalidSymlinkException e)
        {
            logger.LogError("Symlink created but appears invalid: {SymlinkPath}", e.SymlinkPath);
            hostSystem.RemoveSymbolicLinks();
            throw;
        }
        catch (Exception e)
        {
            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
                logger.LogError(e, "Removing {ExtractPath} due to error", extractPath);
            }

            logger.LogError(e, "Error downloading and installing Godot {ReleaseNameWithRuntime}", godotRelease.ReleaseNameWithRuntime);
            throw;
        }
    }

    /// <summary>
    ///     Tries to find and install a version matching the query.
    /// </summary>
    /// <param name="query">Version query arguments</param>
    /// <param name="progress">Progress reporter for installation updates</param>
    /// <param name="setAsDefault">Whether to set the installed version as the global default</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The installation result if successful, null if installation failed.</returns>
    public async Task<Result<InstallationOutcome, InstallationError>> InstallByQueryAsync(string[] query, 
        IProgress<OperationProgress<InstallationStage>> progress, bool setAsDefault = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var releaseNames = await FetchReleaseNames(cancellationToken);
            var godotRelease = releaseManager.TryFindReleaseByQuery(query, releaseNames);

            // Retry with remote fetch if not found
            godotRelease ??= releaseManager.TryFindReleaseByQuery(query, await FetchReleaseNames(cancellationToken, true));

            return godotRelease == null
                ? new Result<InstallationOutcome, InstallationError>.Failure(new InstallationError.NotFound())
                : await InstallReleaseAsync(godotRelease, progress, setAsDefault, cancellationToken);
        }
        catch
        {
            return new Result<InstallationOutcome, InstallationError>.Failure(
                new InstallationError.Failed());
        }
    }

    public async Task<string[]> FetchReleaseNames(CancellationToken cancellationToken, bool remote = false)
    {
        var lastWriteTime = File.GetLastWriteTime(pathService.ReleasesPath);
        var isCacheValid = !remote && File.Exists(pathService.ReleasesPath) && DateTime.Now.AddDays(-1) <= lastWriteTime;

        if (isCacheValid)
        {
            logger.LogInformation("Reading from {ReleasesPath}, last updated {LastWriteTime}", pathService.ReleasesPath, lastWriteTime);
            var cachedReleases = await File.ReadAllLinesAsync(pathService.ReleasesPath, cancellationToken);
            if (cachedReleases.Length > 0)
            {
                return cachedReleases;
            }

            logger.LogError("Cached releases file is empty, fetching from remote");
        }

        var releaseNames = (await releaseManager.ListReleases(cancellationToken)).ToArray();

        if (releaseNames.Length == 0)
        {
            logger.LogError("Unable to fetch remote releases");
            return [];
        }

        await File.WriteAllLinesAsync(pathService.ReleasesPath, releaseNames, cancellationToken);
        return releaseNames;
    }

    private static async Task<string> CalculateChecksum(MemoryStream memStream, CancellationToken cancellationToken)
    {
        using var sha512Hash = SHA512.Create();
        var hashBytes = await sha512Hash.ComputeHashAsync(memStream, cancellationToken);
        var checksum = Convert.ToHexStringLower(hashBytes);
        return checksum;
    }

    public static string? TryParseSha512SumsContent(string fileName, string content)
    {
        var lines = content.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parts = line.Split([' '], 2, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                continue;
            }

            var (currentHash, currentFile) = (parts[0].Trim(), parts[1].Trim());

            if (currentFile != fileName)
            {
                continue;
            }

            return currentHash;
        }

        return null;
    }
}
