using GDVM.Environment;
using GDVM.Error;
using GDVM.Extensions;
using GDVM.Godot;
using GDVM.Progress;
using GDVM.Types;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.IO.Compression;
using System.Security;
using System.Security.Cryptography;
using ZLogger;
using InstallationProgress = GDVM.Progress.OperationProgress<GDVM.Progress.InstallationStage>;

namespace GDVM.Services;

public interface IInstallationService
{
    Task<Result<InstallationOutcome, InstallationError>> InstallReleaseAsync(Release godotRelease, bool setAsDefault = true,
        IProgress<InstallationProgress>? progress = null, CancellationToken cancellationToken = default);

    Task<Result<InstallationOutcome, InstallationError>> InstallByQueryAsync(string[] query, bool setAsDefault = false, IProgress<InstallationProgress>? progress = null,
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
    /// <param name="setAsDefault">Whether to set this version as the global default</param>
    /// <param name="progress">Optional progress reporter for installation progress</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The installation result if successful, null if the installation failed.</returns>
    public async Task<Result<InstallationOutcome, InstallationError>> InstallReleaseAsync(Release godotRelease, bool setAsDefault = true,
        IProgress<InstallationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var extractPath = "";

        try
        {
            var installPathBase = godotRelease.ReleaseNameWithRuntime;

            // Check if already installed
            var existingPath = Path.Combine(pathService.RootPath, installPathBase);
            if (Directory.Exists(existingPath))
            {
                logger.ZLogInformation($"Version {installPathBase} is already installed.");
                return new Result<InstallationOutcome, InstallationError>.Success(
                    new InstallationOutcome.AlreadyInstalled(godotRelease.ReleaseNameWithRuntime));
            }

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

            // Use AnsiConsole.Progress directly for download to avoid multi-line issues
            await AnsiConsole.Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn())
                .StartAsync(async ctx =>
                {
                    var downloadTask = ctx.AddTask($"Downloading {installPathBase}...");
                    downloadTask.MaxValue = contentLength;
                    downloadTask.IsIndeterminate = contentLength == 0;

                    while ((bytesRead = await networkStream.ReadAsync(buffer, cancellationToken)) > 0)
                    {
                        await memStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                        totalDownloaded += bytesRead;

                        if (contentLength > 0)
                        {
                            var downloadedMB = totalDownloaded / 1024.0 / 1024.0;
                            var totalMB = contentLength / 1024.0 / 1024.0;
                            downloadTask.Description = $"Downloading {installPathBase}... ({downloadedMB:F1}/{totalMB:F1} MB)";
                            downloadTask.Value = totalDownloaded;
                        }
                    }
                });

            // Verify checksum if available
            if (godotRelease.Major > 3 || godotRelease is { Major: 3, Minor: >= 3 })
            {
                progress?.Report(new InstallationProgress("Verifying checksum...", InstallationStage.VerifyingChecksum));
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

            progress?.Report(new InstallationProgress("Extracting files...", InstallationStage.Extracting));
            memStream.Position = 0;
            extractPath = Path.Combine(pathService.RootPath, installPathBase);

            using var archive = new ZipArchive(memStream, ZipArchiveMode.Read);
            archive.ExtractWithFlatteningSupport(extractPath, true);

            if (setAsDefault)
            {
                progress?.Report(new InstallationProgress("Setting as default version...", InstallationStage.SettingDefault));
                var symlinkTargetPath = Path.Combine(extractPath, godotRelease.ExecName);
                hostSystem.CreateOrOverwriteSymbolicLink(symlinkTargetPath);
            }

            logger.ZLogInformation($"Successfully installed {godotRelease.ReleaseNameWithRuntime}.");
            return new Result<InstallationOutcome, InstallationError>.Success(
                new InstallationOutcome.NewInstallation(godotRelease.ReleaseNameWithRuntime));
        }
        catch (TaskCanceledException)
        {
            logger.ZLogError($"User cancelled installation.");
            throw;
        }
        catch (InvalidSymlinkException e)
        {
            logger.ZLogError($"Symlink created but appears invalid: {e.SymlinkPath}.");
            hostSystem.RemoveSymbolicLinks();
            throw;
        }
        catch (Exception e)
        {
            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
                logger.ZLogError(e, $"Removing {extractPath} due to error.");
            }

            logger.ZLogError(e, $"Error downloading and installing Godot {godotRelease.ReleaseNameWithRuntime}");
            throw;
        }
    }

    /// <summary>
    ///     Tries to find and install a version matching the query.
    /// </summary>
    /// <param name="query">Version query arguments</param>
    /// <param name="setAsDefault">Whether to set the installed version as the global default</param>
    /// <param name="progress">Optional progress reporter for installation progress</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The installation result if successful, null if installation failed.</returns>
    public async Task<Result<InstallationOutcome, InstallationError>> InstallByQueryAsync(string[] query, bool setAsDefault = false,
        IProgress<InstallationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var releaseNames = await FetchReleaseNames(cancellationToken);
            var godotRelease = releaseManager.TryFindReleaseByQuery(query, releaseNames);

            // Retry with remote fetch if not found
            if (godotRelease == null)
            {
                releaseNames = await FetchReleaseNames(cancellationToken, true);
                godotRelease = releaseManager.TryFindReleaseByQuery(query, releaseNames);
            }

            if (godotRelease == null)
            {
                return new Result<InstallationOutcome, InstallationError>.Failure(
                    new InstallationError.NotFound());
            }

            var success = await InstallReleaseAsync(godotRelease, setAsDefault, progress, cancellationToken);
            return success;
        }
        catch
        {
            return new Result<InstallationOutcome, InstallationError>.Failure(
                new InstallationError.Failed());
        }
    }

    public async Task<string[]> FetchReleaseNames(CancellationToken cancellationToken, bool remote = false)
    {
        string[] releaseNames;
        var usedCache = false;

        var lastWriteTime = File.GetLastWriteTime(pathService.ReleasesPath);
        if (!remote && File.Exists(pathService.ReleasesPath) && DateTime.Now.AddDays(-1) <= lastWriteTime)
        {
            logger.ZLogInformation($"Reading from {pathService.ReleasesPath}, last updated {lastWriteTime}");
            releaseNames = (await File.ReadAllLinesAsync(pathService.ReleasesPath, cancellationToken)).ToArray();
            usedCache = true;
        }
        else
        {
            releaseNames = (await releaseManager.ListReleases(cancellationToken)).ToArray();
        }

        if (!usedCache || releaseNames.Length != 0)
        {
            await File.WriteAllLinesAsync(pathService.ReleasesPath, releaseNames, cancellationToken);
            return releaseNames;
        }

        logger.ZLogError($"Wasn't able to read {pathService.ReleasesPath}.");

        releaseNames = (await releaseManager.ListReleases(cancellationToken)).ToArray();

        if (releaseNames.Length == 0)
        {
            logger.ZLogError($"Wasn't able to read remote releases.");
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
