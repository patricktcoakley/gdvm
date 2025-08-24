using GDVM.Environment;
using GDVM.Error;
using GDVM.Types;
using Spectre.Console;

namespace GDVM.Command;

public sealed class WhichCommand(IHostSystem hostSystem, IAnsiConsole console)
{
    /// <summary>
    ///     Displays the currently selected version of Godot (if any).
    /// </summary>
    public void Which()
    {
        var result = hostSystem.ResolveCurrentSymlinks();

        var message = result switch
        {
            Result<SymlinkInfo, SymlinkError>.Success(var info) =>
                FormatSymlinkInfo(info),
            Result<SymlinkInfo, SymlinkError>.Failure(SymlinkError.NoVersionSet) =>
                Messages.NoVersionSet,
            Result<SymlinkInfo, SymlinkError>.Failure(SymlinkError.InvalidSymlink(var path, var target)) =>
                Messages.InvalidSymlink(path, target),
            _ => Messages.UnknownSymlinkError
        };

        console.MarkupLine(message);
    }

    private static string FormatSymlinkInfo(SymlinkInfo info)
    {
        var message = Messages.CurrentVersionSetTo(info.SymlinkPath);

        if (info.MacAppSymlinkPath is not null)
        {
            message += Messages.CurrentMacOSAppSetTo(info.MacAppSymlinkPath);
        }

        return message;
    }
}
