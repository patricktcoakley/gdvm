using ConsoleAppFramework;
using GDVM.Environment;
using GDVM.Error;
using GDVM.Extensions;
using GDVM.Godot;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.IO.Compression;
using System.Security;
using System.Security.Cryptography;
using ZLogger;

namespace GDVM.Command;

public sealed class InstallCommand(IHostSystem hostSystem, IReleaseManager releaseManager, ILogger<InstallCommand> logger)
{
    /// <summary>
    ///     Prompts the user to select a version or takes a query to find the best match, verifies and installs it as the default.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="CryptographicException"></exception>
    /// <exception cref="SecurityException"></exception>
    public async Task Install(CancellationToken cancellationToken = default, [Argument] params string[] query)
    {
        var extractPath = "";
        Release? godotRelease = null;

        try
        {
            var releaseNames = await FetchReleaseNames(cancellationToken);
            if (query.Length == 0)
            {
                var version = await Prompts.Install.ShowVersionSelectionPrompt(releaseNames, cancellationToken);
                godotRelease = releaseManager.TryCreateRelease(version) ?? throw new Exception($"Invalid Godot version {version}.");
            }
            else
            {
                godotRelease = releaseManager.FindReleaseByQuery(query, releaseNames);
            }


            var installPathBase = godotRelease.ReleaseNameWithRuntime;

            // TODO: determine if this should be tunable; 32kb seems reasonable for now
            const int bufferSize = 32768;
            using var response =
                await releaseManager.GetZipFile(godotRelease.ZipFileName, godotRelease, cancellationToken);

            var contentLength = response.Content.Headers.ContentLength ?? 0;

            // This could be swapped for a FileStream but MemoryStream makes sense because
            // it allows for rewind, and it can also be directly passed into a Zip object
            // for direct extraction without needing to deal with temp files.
            // The downside is it lives on the LOH, but it should average at or under ~100MB.
            await using var memStream = new MemoryStream(checked((int)contentLength));

            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var downloadTask = ctx.AddTask($"[green]Downloading {installPathBase}...[/]");
                    downloadTask.MaxValue = contentLength;
                    downloadTask.IsIndeterminate = contentLength == 0;

                    await using var networkStream = await response.Content.ReadAsStreamAsync(cancellationToken);

                    var buffer = new byte[bufferSize];
                    int bytesRead;

                    while ((bytesRead = await networkStream.ReadAsync(buffer, cancellationToken)) > 0)
                    {
                        await memStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                        downloadTask.Increment(bytesRead);
                    }
                });


            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Star)
                .SpinnerStyle(Style.Parse("green bold"))
                .StartAsync("Installing...", async _ =>
                {
                    // Verify checksum if available; Godot versions prior to 3.3 didn't have them
                    if (godotRelease is { Major: >= 3, Minor: >= 3 })
                    {
                        AnsiConsole.MarkupLine("Calculating checksum :input_numbers:...");

                        // Reset for checksum
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
                    else
                    {
                        AnsiConsole.MarkupLine("[orange1]Selected version is older than 3.3; no checksum available to verify.[/]");
                    }

                    AnsiConsole.MarkupLine("Extracting files :file_folder:...");

                    // Reset for file operation
                    memStream.Position = 0;

                    extractPath = Path.Combine(Paths.RootPath, installPathBase);

                    using var archive = new ZipArchive(memStream, ZipArchiveMode.Read);

                    // We want to extract a flattened archive for Mono Linux and Windows and normally for the rest
                    archive.ExtractWithFlatteningSupport(extractPath, true);

                    AnsiConsole.MarkupLine("Creating symlink... :link:");

                    var symlinkTargetPath = Path.Combine(extractPath, godotRelease.ExecName);

                    hostSystem.CreateOrOverwriteSymbolicLink(symlinkTargetPath);
                });

            logger.ZLogInformation($"Successfully installed {godotRelease.ReleaseNameWithRuntime}.");
            AnsiConsole.MarkupLine("Finished! :party_popper:");
        }
        catch (TaskCanceledException)
        {
            logger.ZLogError($"User cancelled installation.");
            AnsiConsole.MarkupLine(Messages.UserCancelled("installation"));

            throw;
        }
        catch (InvalidSymlinkException e)
        {
            logger.ZLogError($"Symlink created but appears invalid: {e.SymlinkPath}.");
            AnsiConsole.MarkupLine(
                $"[orange1] WARN: Symlink for {e.SymlinkPath} was created but appears to be invalid. Removing it. [/]");

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

            logger.ZLogError(e, $"Error downloading and installing Godot {godotRelease?.ReleaseNameWithRuntime}");
            AnsiConsole.MarkupLine(
                Messages.SomethingWentWrong($"when trying to install Godot: {e.Message}")
            );

            throw;
        }
    }

    private async Task<string[]> FetchReleaseNames(CancellationToken cancellationToken)
    {
        string[] releaseNames;
        var usedCache = false;

        // TODO: consider making this tunable or having an override to force it to reach out and update
        var lastWriteTime = File.GetLastWriteTime(Paths.ReleasesPath);
        if (File.Exists(Paths.ReleasesPath) && DateTime.Now.AddDays(-1) <= lastWriteTime)
        {
            logger.ZLogInformation($"Reading from {Paths.ReleasesPath}, last updated {lastWriteTime}");
            releaseNames = (await File.ReadAllLinesAsync(Paths.ReleasesPath, cancellationToken)).ToArray();
            usedCache = true;
        }
        else
        {
            releaseNames = (await releaseManager.ListReleases(cancellationToken)).ToArray();
        }

        if (!usedCache || releaseNames.Length != 0)
        {
            // always update on successful remote read
            await File.WriteAllLinesAsync(Paths.ReleasesPath, releaseNames, cancellationToken);
            return releaseNames;
        }

        logger.ZLogError($"Wasn't able to read {Paths.ReleasesPath}.");
        AnsiConsole.Console.MarkupLine(
            $"[blue]Something went wrong when reading {Paths.ReleasesPath}, trying remote releases. [/]");

        releaseNames = (await releaseManager.ListReleases(cancellationToken)).ToArray();

        if (releaseNames.Length == 0)
        {
            logger.ZLogError($"Wasn't able to read remote releases.");
            AnsiConsole.Console.MarkupLine("[red]Something went wrong when downloading the remote releases. [/]");
            return [];
        }

        // always update on successful remote read
        await File.WriteAllLinesAsync(Paths.ReleasesPath, releaseNames, cancellationToken);

        return releaseNames;
    }

    private static async Task<string> CalculateChecksum(MemoryStream memStream, CancellationToken cancellationToken)
    {
        using var sha512Hash = SHA512.Create();
        var hashBytes = await sha512Hash.ComputeHashAsync(memStream, cancellationToken);
        var checksum = Convert.ToHexStringLower(hashBytes);
        return checksum;
    }

    /// <summary>
    ///     Parses the `SHA512-SUMS.txt` for the selected file to grab the correct SHA512 since they are 2-space separated.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="content"></param>
    /// <returns></returns>
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
