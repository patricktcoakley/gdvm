using Spectre.Console;

namespace GDVM.Prompts;

public static class Godot
{
    public static SelectionPrompt<string> CreateGodotSelectionPrompt(List<string> installed) =>
        new SelectionPrompt<string>()
            .Title($"{Messages.SelectAVersionTo("launch")}")
            .PageSize(10)
            .WrapAround()
            .EnableSearch()
            .MoreChoicesText("[grey](Move up and down to see more versions)[/]")
            .AddChoices(installed);
}
