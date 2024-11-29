using Spectre.Console;

namespace GDVM.Prompts;

public static class Remove
{
    public static async Task<IEnumerable<string>> ShowVersionRemovalPrompt(
        string[] installed, CancellationToken cancellationToken)
    {
        var versionPrompt = CreateRemoveVersionPrompt(installed);
        var versions = await versionPrompt.ShowAsync(AnsiConsole.Console, cancellationToken);
        return versions;
    }

    private static MultiSelectionPrompt<string> CreateRemoveVersionPrompt(string[] installed) =>
        new MultiSelectionPrompt<string>()
            .Title($"{Messages.SelectVersionsTo("remove")}")
            .Required()
            .PageSize(10)
            .MoreChoicesText("[grey](Move up and down to reveal more versions)[/]")
            .Mode(SelectionMode.Independent)
            .InstructionsText(
                "[grey](Press [blue]<space>[/] to toggle an installation, " +
                "[green]<enter>[/] to accept)[/]")
            .AddChoices(installed);
}
