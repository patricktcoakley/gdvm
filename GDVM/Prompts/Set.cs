using Spectre.Console;

namespace GDVM.Prompts;

public static class Set
{
    public static async Task<string> ShowSetVersionPrompt(
        string[] installed, IAnsiConsole console, CancellationToken cancellationToken)
    {
        var prompt = CreateSetVersionPrompt(installed);
        return await prompt.ShowAsync(console, cancellationToken);
    }

    private static SelectionPrompt<string> CreateSetVersionPrompt(string[] installed) =>
        new SelectionPrompt<string>()
            .Title("[green]Select a version to set: [/]?")
            .PageSize(10)
            .WrapAround()
            .EnableSearch()
            .MoreChoicesText("[grey](Move up and down to see more versions)[/]")
            .AddChoices(installed);
}
