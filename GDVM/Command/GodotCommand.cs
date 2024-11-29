using ConsoleAppFramework;
using GDVM.Environment;
using GDVM.Error;
using GDVM.Godot;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Diagnostics;
using System.Text;
using ZLogger;

namespace GDVM.Command;

public sealed class GodotCommand(IHostSystem hostSystem, IReleaseManager releaseManager, ILogger<GodotCommand> logger)
{
    /// <summary>
    ///     Launches the currently selected Godot version, or takes the `-i|--interactive` flag to prompt the user to choose.
    /// </summary>
    /// <param name="args">Arguments to pass to the Godot executable.</param>
    /// <param name="interactive">-i, Creates a prompt to select and launch an installed Godot version.</param>
    /// <param name="cancellationToken"></param>
    [Command("godot")]
    public async Task Launch([Argument] string args = "", bool interactive = false,
        CancellationToken cancellationToken = default)
    {
        var error = new StringBuilder();
        var execPath = "";
        var workingDirectory = "";
        var process = new Process();

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Path.Exists(Paths.SymlinkPath))
            {
                logger.ZLogError($"Tried to launch when no version is set.");
                AnsiConsole.MarkupLine("[red]No current Godot version set.[/]");
                return;
            }

            // default to symlink info
            var symlinkInfo = new FileInfo(Paths.SymlinkPath);
            execPath = symlinkInfo.ResolveLinkTarget(true)!.FullName;

            if (symlinkInfo.LinkTarget is { } target)
            {
                var split = target.Split(Path.DirectorySeparatorChar)[..^1];
                workingDirectory = string.Join(Path.DirectorySeparatorChar, split);
            }

            if (interactive)
            {
                var installed = hostSystem.ListInstallations().ToList();
                var selection = AnsiConsole.Prompt(Prompts.Godot.CreateGodotSelectionPrompt(installed));

                if (releaseManager.TryCreateRelease(selection) is not { } godotRelease)
                {
                    throw new Exception("Invalid Godot version.");
                }

                // needs to match the root install path for dependencies (GodotSharp) to work
                workingDirectory = Path.Combine(Paths.RootPath, godotRelease.ReleaseNameWithRuntime);
                execPath = Path.Combine(Paths.RootPath, workingDirectory, godotRelease.ExecName);
            }

            // Should work on all platforms and architectures but unsure; working on Windows X64 and macOS ARM64.

            process.StartInfo = new ProcessStartInfo
            {
                Arguments = args,
                FileName = execPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workingDirectory
            };

            process.EnableRaisingEvents = true;

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is { } data)
                {
                    AnsiConsole.MarkupLine($"[green]{data}[/]");
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is { } data)
                {
                    AnsiConsole.MarkupLine(data.EscapeMarkup());
                    error.Append(data + " ");
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            process.CancelOutputRead();
            process.CancelErrorRead();
        }
        catch (TaskCanceledException)
        {
            logger.ZLogError($"User cancelled running Godot.");
            AnsiConsole.MarkupLine(Messages.UserCancelled("godot"));

            throw;
        }
        catch (Exception e)
        {
            logger.ZLogError(
                $"Error running Godot at path {execPath} and working directory {workingDirectory} with the following error: {e.Message}");

            AnsiConsole.MarkupLine(
                Messages.SomethingWentWrong("when trying to launch Godot.")
            );

            throw;
        }
        finally
        {
            Console.WriteLine(process.ExitCode);
            if (process.ExitCode != 0)
            {
                logger.ZLogError(
                    $"Error launching an instance using path {execPath} and working directory {workingDirectory} with the following error:{error.ToString().EscapeMarkup()}");
            }
        }
    }
}
