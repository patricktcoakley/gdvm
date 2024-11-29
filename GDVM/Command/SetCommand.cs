using GDVM.Environment;
using GDVM.Error;
using GDVM.Godot;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using ZLogger;

namespace GDVM.Command;

public sealed class SetCommand(IHostSystem hostSystem, IReleaseManager releaseManager, ILogger<SetCommand> logger)
{
    /// <summary>
    ///     Sets the selected version of Godot.
    /// </summary>
    /// <exception cref="FileNotFoundException"></exception>
    public void Set()
    {
        try
        {
            var installed = hostSystem.ListInstallations().ToArray();
            if (installed.Length == 0)
            {
                logger.ZLogWarning($"Tried to set a version when there were none installed.");
                AnsiConsole.MarkupLine("[orange1] No installations found. Please install one or more versions first.[/]");
                return;
            }

            var versionToSet = AnsiConsole.Prompt(Prompts.Set.CreateSetVersionPrompt(installed));

            if (releaseManager.TryCreateRelease(versionToSet) is not { } godotRelease)
            {
                throw new Exception("Invalid Godot version.");
            }

            var symlinkTargetPath = Path.Combine(Paths.RootPath, godotRelease.ReleaseNameWithRuntime);

            symlinkTargetPath = Path.Combine(symlinkTargetPath, godotRelease.ExecName);
            hostSystem.CreateOrOverwriteSymbolicLink(symlinkTargetPath);

            logger.ZLogInformation($"Successfully set version to {versionToSet}.");
            AnsiConsole.MarkupLine($"[green]Successfully set version to {versionToSet}. [/]");
        }

        catch (InvalidSymlinkException e)
        {
            logger.ZLogError($"Symlink created but appears invalid: {e.SymlinkPath}.");
            AnsiConsole.MarkupLine($"[orange1]WARN: Symlink for {e.SymlinkPath} was created but appears to be invalid. Removing it.[/]");

            hostSystem.RemoveSymbolicLinks();

            throw;
        }
        catch (Exception e)
        {
            logger.ZLogError($"Error setting a version: {e.Message}");
            AnsiConsole.MarkupLine(
                Messages.SomethingWentWrong("when trying to set the version")
            );

            throw;
        }
    }
}
