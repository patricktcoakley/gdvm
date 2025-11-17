using Fgvm.Cli.Error;
using Fgvm.Cli.ViewModels;
using Fgvm.Environment;
using Fgvm.Types;
using Spectre.Console;
using System.Text.Json.Serialization;

namespace Fgvm.Cli.Command;

public sealed class WhichCommand(IHostSystem hostSystem, IAnsiConsole console)
{
    /// <summary>
    ///     Displays the currently selected version of Godot (if any).
    /// </summary>
    public void Which(bool json = false)
    {
        var result = hostSystem.ResolveCurrentSymlinks();
        var view = WhichView.Create(result);

        if (json)
        {
            console.WriteLine(view.ToJson());
            return;
        }

        console.MarkupLine(view.ToDisplay());
    }
}

internal readonly record struct WhichView : IJsonView<WhichView>
{
    private WhichView(bool hasVersion, string? symlinkPath, string? macAppSymlinkPath, string? message)
    {
        HasVersion = hasVersion;
        SymlinkPath = symlinkPath;
        MacAppSymlinkPath = macAppSymlinkPath;
        Message = message;
    }

    [JsonPropertyName("hasVersion")]
    public bool HasVersion { get; init; }

    [JsonPropertyName("symlinkPath")]
    public string? SymlinkPath { get; init; }

    [JsonPropertyName("macAppSymlinkPath")]
    public string? MacAppSymlinkPath { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    public static WhichView Create(Result<SymlinkInfo, SymlinkError> result) => result switch
    {
        Result<SymlinkInfo, SymlinkError>.Success(var info) =>
            new WhichView(true, info.SymlinkPath, info.MacAppSymlinkPath, null),
        Result<SymlinkInfo, SymlinkError>.Failure(SymlinkError.NoVersionSet) =>
            new WhichView(false, null, null, "No Godot version is currently set."),
        Result<SymlinkInfo, SymlinkError>.Failure(SymlinkError.InvalidSymlink(var path, var target)) =>
            new WhichView(false, path, null, $"Invalid symlink: {path} -> {target}"),
        _ => new WhichView(false, null, null, "Unknown symlink error.")
    };

    public string ToDisplay()
    {
        if (HasVersion && SymlinkPath is not null)
        {
            var message = Messages.CurrentVersionSetTo(SymlinkPath);
            if (MacAppSymlinkPath is not null)
            {
                message += Messages.CurrentMacOSAppSetTo(MacAppSymlinkPath);
            }

            return message;
        }

        return Message ?? Messages.UnknownSymlinkError;
    }
}
