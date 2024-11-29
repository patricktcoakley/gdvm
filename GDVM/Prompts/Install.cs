using GDVM.Godot;
using Spectre.Console;

namespace GDVM.Prompts;

public static class Install
{
    public static async Task<string> ShowVersionSelectionPrompt(
        string[] releaseNames, CancellationToken cancellationToken)
    {
        var versionPrompt = CreateVersionSelectionPrompt(releaseNames);
        var version = await versionPrompt.ShowAsync(AnsiConsole.Console, cancellationToken);

        var runtimePrompt = CreateRuntimePrompt();
        var godotPlatform = await runtimePrompt.ShowAsync(AnsiConsole.Console, cancellationToken) == "Mono"
            ? RuntimeEnvironment.Mono
            : RuntimeEnvironment.Standard;

        return $"{version}-{godotPlatform.Name()}";
    }

    public static SelectionPrompt<string> CreateRuntimePrompt()
    {
        var isMonoPrompt = new SelectionPrompt<string>()
            .Title(Messages.SelectARuntime)
            .AddChoices("Standard", "Mono");

        return isMonoPrompt;
    }

    public static SelectionPrompt<string> CreateVersionSelectionPrompt(string[] releaseNames)
    {
        var versionPrompt = new SelectionPrompt<string>()
            .Title($"{Messages.SelectAVersionTo("install")}")
            .PageSize(20)
            .WrapAround()
            .EnableSearch()
            .MoreChoicesText("[grey](Move up and down to see more versions)[/]")
            .SearchPlaceholderText(Messages.TypeToSearch)
            .AddChoices(releaseNames);

        return versionPrompt;
    }
}
