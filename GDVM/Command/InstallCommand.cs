using ConsoleAppFramework;
using GDVM.Environment;
using GDVM.Error;
using GDVM.Godot;
using GDVM.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Security;
using System.Security.Cryptography;
using ZLogger;

namespace GDVM.Command;

// These are implemented as sub-commands because ConsoleAppFramework doesn't seem to support using flags
// with params, so the options are to either use a named field for args, which isn't user-friendly, or just
// create a dummy sub-command. This means the help menu is not going to capture both under install.
// see: https://github.com/Cysharp/ConsoleAppFramework/issues/179

public sealed class InstallCommand(
    IHostSystem hostSystem,
    IReleaseManager releaseManager,
    IInstallationService installationService,
    Messages messages,
    IAnsiConsole console,
    ILogger<InstallCommand> logger)
{
    /// <summary>
    ///     Prompts the user to select a version or takes a query to find the best match, verifies and installs it.
    /// </summary>
    /// <param name="query">Version query arguments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="CryptographicException"></exception>
    /// <exception cref="SecurityException"></exception>
    public async Task Install(CancellationToken cancellationToken = default, [Argument] params string[] query) => await InstallCore(query, false, cancellationToken);

    /// <summary>
    ///     Prompts the user to select a version or takes a query to find the best match, verifies and installs it as the default.
    /// </summary>
    /// <param name="query">Version query arguments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="CryptographicException"></exception>
    /// <exception cref="SecurityException"></exception>
    [Command("install default")]
    public async Task InstallDefault(CancellationToken cancellationToken = default, [Argument] params string[] query) =>
        await InstallCore(query, true, cancellationToken);

    /// <summary>
    ///     Core installation logic shared between Install and InstallDefault methods.
    /// </summary>
    /// <param name="query">Version query arguments</param>
    /// <param name="setAsDefault">Whether to set the installed version as default</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="CryptographicException"></exception>
    /// <exception cref="SecurityException"></exception>
    private async Task InstallCore(string[] query, bool setAsDefault, CancellationToken cancellationToken)
    {
        try
        {
            InstallationResult installationResult;

            if (query.Length == 0)
            {
                var releaseNames = await installationService.FetchReleaseNames(cancellationToken);
                var version = await Prompts.Install.ShowVersionSelectionPrompt(releaseNames, console, cancellationToken);
                var godotRelease = releaseManager.TryCreateRelease(version) ?? throw new Exception($"Unable to get release with selection `{version}`.");

                installationResult = await installationService.InstallReleaseAsync(godotRelease, setAsDefault, cancellationToken);
            }
            else
            {
                installationResult = await installationService.InstallByQueryAsync(query, setAsDefault, cancellationToken);
            }

            var msg = installationResult switch
            {
                { Status: InstallationStatus.AlreadyInstalled } => $"[yellow]{installationResult.ReleaseNameWithRuntime}[/] is already installed.",
                { Status: InstallationStatus.NewInstallation } => $"[green]Finished installing [blue]{installationResult.ReleaseNameWithRuntime}[/].[/]",
                _ => throw new Exception($"Unable to find Godot release with query `{string.Join(", ", query)}`")
            };

            console.MarkupLine(msg);
        }
        catch (TaskCanceledException)
        {
            logger.ZLogError($"User cancelled installation.");
            console.MarkupLine(Messages.UserCancelled("installation"));
            throw;
        }
        catch (InvalidSymlinkException e)
        {
            logger.ZLogError($"Symlink created but appears invalid: {e.SymlinkPath}.");
            console.MarkupLine(
                $"[orange1] WARN: Symlink for {e.SymlinkPath} was created but appears to be invalid. Removing it. [/]");

            hostSystem.RemoveSymbolicLinks();
            throw;
        }
        catch (Exception e)
        {
            logger.ZLogError(e, $"Error downloading and installing Godot.");
            console.MarkupLine(
                messages.SomethingWentWrong($"when trying to install Godot: {e.Message}")
            );

            throw;
        }
    }
}
