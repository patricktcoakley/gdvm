using GDVM.Environment;
using GDVM.Error;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using ZLogger;

namespace GDVM.Command;

public sealed class WhichCommand(IHostSystem hostSystem, Messages messages, IAnsiConsole console, ILogger<WhichCommand> logger)
{
    /// <summary>
    ///     Displays the currently selected version of Godot (if any).
    /// </summary>
    public void Which()
    {
        try
        {
            hostSystem.DisplaySymbolicLinks();
        }
        catch (Exception e)
        {
            logger.ZLogError($"Error reading symlink: {e.Message}");
            console.MarkupLine(
                messages.SomethingWentWrong("when trying to read which Godot version is set")
            );

            throw;
        }
    }
}
