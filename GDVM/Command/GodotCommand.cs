using ConsoleAppFramework;
using GDVM.Error;
using GDVM.Godot;
using GDVM.Services;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Diagnostics;
using System.Text;
using ZLogger;

namespace GDVM.Command;

public sealed class GodotCommand(IVersionManagementService versionManagementService, IGodotArgumentService argumentService, IProjectManager projectManager, Messages messages, IAnsiConsole console, ILogger<GodotCommand> logger)
{
    /// <summary>
    ///     Launches the currently selected Godot version. If a project-specific version is detected but not installed, it prompts the user to automatically install it.
    ///     By default, Godot runs in detached mode (independent of the terminal).
    /// </summary>
    /// <param name="interactive">-i, Creates a prompt to select and launch an installed Godot version.</param>
    /// <param name="attached">-a, Launches Godot in attached mode, keeping it connected to the terminal for output.</param>
    /// <param name="args">Arguments to pass to the Godot executable. Multiple arguments must be passed as a single quoted string (e.g., "--version --verbose").</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [Command("godot")]
    public async Task Launch(bool interactive = false, bool attached = false, string[]? args = null, CancellationToken cancellationToken = default)
    {
        var error = new StringBuilder();
        var process = new Process();
        VersionResolutionResult versionResult = default;

        // Register cancellation callback early to handle cancellation during version resolution
        await using var cancellationRegistration = cancellationToken.Register(() =>
        {
            try
            {
                if (process.HasExited == false)
                {
                    process.Kill();
                }
            }
            catch (Exception ex)
            {
                logger.ZLogWarning($"Failed to kill process during cancellation: {ex.Message}");
            }
        });

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Validate arg string
            if (args is { Length: > 1 })
            {
                console.MarkupLine("[red]Error: Multiple arguments detected. Please pass all Godot arguments as a single quoted string.[/]");
                console.MarkupLine($"[yellow]Example: gdvm godot --args \"{string.Join(" ", args)}\"[/]");
                console.MarkupLine("[dim]This ensures proper argument parsing and avoids shell interpretation issues.[/]");
                return;
            }

            // Use the version management service to resolve the appropriate version (explicit .gdvm-version only)
            versionResult = await versionManagementService.ResolveVersionForLaunchExplicitAsync(interactive, cancellationToken);

            if (!versionResult.IsSuccess)
            {
                // Error messages are already handled in the service
                return;
            }

            var execPath = versionResult.ExecutablePath;
            var workingDirectory = versionResult.WorkingDirectory;

            // Check if this is a help or version command that should output directly to console
            var argumentString = args != null ? string.Join(" ", args) : "";

            // Auto-detect project file and add it to arguments if we're in a project directory
            if (string.IsNullOrEmpty(argumentString))
            {
                var projectFilePath = projectManager.FindProjectFilePath();
                if (projectFilePath is not null)
                {
                    // Godot expects the directory path, not the file path
                    var projectDirectory = Path.GetDirectoryName(projectFilePath);
                    argumentString = $"--editor --path \"{projectDirectory}\"";
                    console.MarkupLine($"[dim]Auto-detected project file: {Path.GetFileName(projectFilePath)}[/]");
                }
            }

            // Force attached mode for certain arguments that need terminal output
            var forceAttached = argumentService.ShouldForceAttachedMode(argumentString);
            var useAttachedMode = attached || forceAttached;

            if (!useAttachedMode)
            {
                // In detached mode (default), completely disconnect from terminal
                process.StartInfo = new ProcessStartInfo
                {
                    Arguments = argumentString,
                    FileName = execPath,
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = workingDirectory
                };

                process.Start();

                // Close the streams to fully disconnect from terminal
                process.StandardInput.Close();

                console.MarkupLine($"[green]Launched Godot {versionResult.VersionName} in detached mode (PID: {process.Id}).[/]");
                return;
            }

            // For attached mode, redirect and handle output through Spectre.Console
            process.StartInfo = new ProcessStartInfo
            {
                Arguments = argumentString,
                FileName = execPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workingDirectory
            };

            if (forceAttached && !attached)
            {
                console.MarkupLine($"[yellow]Note: Running Godot {versionResult.VersionName} in attached mode due to arguments requiring terminal output.[/]");
            }

            process.EnableRaisingEvents = true;

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is { } data)
                {
                    console.MarkupLine($"[green]{data.EscapeMarkup()}[/]");
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not { } data)
                {
                    return;
                }

                console.MarkupLine(data.EscapeMarkup());
                error.Append(data + " ");
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(CancellationToken.None);
            process.CancelOutputRead();
            process.CancelErrorRead();

            // Check if cancellation was requested after process completed
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (TaskCanceledException)
        {
            logger.ZLogError($"User cancelled running Godot.");
            console.MarkupLine(Messages.UserCancelled("godot"));

            throw;
        }
        catch (Exception e)
        {
            logger.ZLogError(
                $"Error running Godot at path {(versionResult.IsSuccess ? versionResult.ExecutablePath : "unknown")} and working directory {(versionResult.IsSuccess ? versionResult.WorkingDirectory : "unknown")} with the following error: {e.Message}");
            console.MarkupLine(
                messages.SomethingWentWrong("when trying to launch Godot.")
            );

            throw;
        }
        finally
        {
            if (attached && process.ExitCode != 0)
            {
                logger.ZLogError(
                    $"Error launching an instance using path {(versionResult.IsSuccess ? versionResult.ExecutablePath : "unknown")} and working directory {(versionResult.IsSuccess ? versionResult.WorkingDirectory : "unknown")} with the following error:{error.ToString().EscapeMarkup()}");
                console.MarkupLine(messages.SomethingWentWrong("when running Godot."));
            }
        }
    }
}
