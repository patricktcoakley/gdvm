using GDVM.Environment;

namespace GDVM.Error;

public class Messages(IPathService pathService)
{
    public static string UserCancelled(string what) => $"[red]User cancelled {what} operation :prohibited:[/]";

    public string SomethingWentWrong(string when) =>
        $"[red]Something went wrong {when} 💣\nPlease use [hotpink_1]gdvm logs[/] for more information or check {pathService.LogPath}.[/]";
}
