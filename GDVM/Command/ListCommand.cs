using GDVM.Environment;
using GDVM.Error;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using ZLogger;

namespace GDVM.Command;

public sealed class ListCommand(IHostSystem hostSystem, ILogger<ListCommand> logger)
{
    /// <summary>
    ///     Lists the local Godot installations.
    /// </summary>
    public void List()
    {
        try
        {
            var installed = hostSystem.ListInstallations();
            var symlinkFullPath = new FileInfo(Paths.SymlinkPath).LinkTarget ?? "NOT_SET";

            var versions = installed.Select(x => symlinkFullPath.Contains(x) ? $":eight_pointed_star:  {x}" : $"   {x}");

            var panel = new Panel(string.Join("\n", versions))
            {
                Header = new PanelHeader("List Of Installed Versions"),
                Width = 40,
                Border = BoxBorder.Rounded
            };

            AnsiConsole.Write(panel);
        }
        catch (Exception e)
        {
            logger.ZLogError($"Error listing installations: {e.Message}");
            AnsiConsole.MarkupLine(
                Messages.SomethingWentWrong("when trying to list installations")
            );

            throw;
        }
    }
}
