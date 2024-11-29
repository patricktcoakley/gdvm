namespace GDVM.Prompts;

public static class Messages
{
    public static string TypeToSearch => "[aqua]Type to search...[/]";
    public static string SelectARuntime => "[green]Standard or Mono (.NET Support)?[/]\n[hotpink_1](Press CTRL+C to cancel)[/]";
    public static string SelectAVersionTo(string what) => $"[green]Select a version to {what}[/]\n[hotpink_1](Press CTRL+C to cancel)[/]";
    public static string SelectVersionsTo(string what) => $"[green]Select the versions to {what}[/]\n[hotpink_1](Press CTRL+C to cancel)[/]";
}
