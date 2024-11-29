using Spectre.Console;

namespace GDVM.Prompts;

public static class Set
{
    public static SelectionPrompt<string> CreateSetVersionPrompt(string[] installed) =>
        new SelectionPrompt<string>()
            .Title("[green]Select a version to set: [/]?")
            .PageSize(10)
            .MoreChoicesText("[grey](Move up and down to see more versions)[/]")
            .AddChoices(installed);
}
