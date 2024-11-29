using GDVM.Environment;

namespace GDVM.Error;

// Just a place to keep all the reusable error messages
public static class Messages
{
    public static string UserCancelled(string what) => $"[red]User cancelled {what} operation :prohibited:[/]";

    public static string SomethingWentWrong(string when) =>
        $"[red]Something went wrong {when} :bomb:\nPlease use `[hotpink_1]gdvm logs[/]` for more information or check {Paths.LogPath}.[/]";
}
