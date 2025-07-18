using GDVM.Environment;

namespace GDVM.Error;

/// <summary>
///     CLI-specific user-facing messages with markup formatting
/// </summary>
public static class Messages
{
    // Prompt messages
    public static string TypeToSearch => "[aqua]Type to search...[/]";

    public static string SelectARuntime => "[green]Standard or Mono (.NET Support)?[/]\n[hotpink_1](Press CTRL+C to cancel)[/]";

    /// <summary>
    ///     Message for when user cancels an operation
    /// </summary>
    public static string UserCancelled(string what) =>
        $"[red]User cancelled {what} operation :prohibited:[/]";

    /// <summary>
    ///     Generic error message for when something goes wrong
    /// </summary>
    public static string SomethingWentWrong(string when, string logPath) =>
        $"[red]Something went wrong {when} 💣[/]\n[red]Please use [hotpink_1]gdvm logs[/] for more information or check {logPath}.[/]";

    /// <summary>
    ///     Helper method that takes IPathService to get log path
    /// </summary>
    public static string SomethingWentWrong(string when, IPathService pathService) =>
        SomethingWentWrong(when, pathService.LogPath);

    /// <summary>
    ///     Simplified version without specific log path
    /// </summary>
    public static string SomethingWentWrong(string when) =>
        $"[red]Something went wrong {when} 💣[/]\n[red]Please use [hotpink_1]gdvm logs[/] for more information.[/]";

    public static string SelectAVersionTo(string what) =>
        $"[green]Select a version to {what}[/]\n[hotpink_1](Press CTRL+C to cancel)[/]";

    public static string SelectVersionsTo(string what) =>
        $"[green]Select the versions to {what}[/]\n[hotpink_1](Press CTRL+C to cancel)[/]";
}
