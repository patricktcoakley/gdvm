using Fgvm.Cli.Prompts;
using Spectre.Console.Testing;

namespace Fgvm.Tests.Prompts;

public class InstallTests
{
    private static readonly string[] TestReleases =
    [
        "4.4-dev5",
        "4.4-dev4",
        "4.4-dev3",
        "4.4-dev2",
        "4.4-dev1",
        "4.3-stable",
        "4.3-rc3",
        "4.3-rc2"
    ];


    public InstallTests()
    {
        _testConsole = new TestConsole
        {
            Profile = { Capabilities = { Interactive = true } }
        };

        _testConsole.EmitAnsiSequences();
    }

    private TestConsole _testConsole { get; }

    [Fact]
    public void VersionSelectionPrompt_ShouldSelectCorrectVersion_WhenNavigatingWithArrows()
    {
        foreach (var index in new[] { 0, TestReleases.Length / 2, TestReleases.Length - 1 })
        {
            for (var i = 0; i < index; i++)
            {
                _testConsole.Input.PushKey(ConsoleKey.DownArrow);
            }

            _testConsole.Input.PushKey(ConsoleKey.Enter);

            var prompt = Install.CreateVersionSelectionPrompt(TestReleases);
            var selection = prompt.Show(_testConsole);

            Assert.Equal(TestReleases[index], selection);
        }
    }

    [Fact]
    public void VersionSelectionPrompt_ShouldSelectFirstVersion_WhenPressingEnterImmediately()
    {
        _testConsole.Input.PushKey(ConsoleKey.Enter);

        var prompt = Install.CreateVersionSelectionPrompt(TestReleases);
        var selection = prompt.Show(_testConsole);

        Assert.Equal(TestReleases[0], selection);
    }

    [Fact]
    public void VersionSelectionPrompt_ShouldWrapAround_WhenNavigatingPastLastItem()
    {
        for (var i = 0; i < TestReleases.Length; i++)
        {
            _testConsole.Input.PushKey(ConsoleKey.DownArrow);
        }

        _testConsole.Input.PushKey(ConsoleKey.Enter);

        var prompt = Install.CreateVersionSelectionPrompt(TestReleases);
        var selection = prompt.Show(_testConsole);

        Assert.Equal(TestReleases[0], selection);
    }

    [Fact]
    public void VersionSelectionPrompt_ShouldHandleUpArrowNavigation()
    {
        _testConsole.Input.PushKey(ConsoleKey.UpArrow);
        _testConsole.Input.PushKey(ConsoleKey.Enter);

        var prompt = Install.CreateVersionSelectionPrompt(TestReleases);
        var selection = prompt.Show(_testConsole);

        Assert.Equal(TestReleases[^1], selection);
    }

    [Fact]
    public void RuntimePrompt_ShouldReturnMono_WhenUserSelectsMono()
    {
        _testConsole.Input.PushKey(ConsoleKey.DownArrow);
        _testConsole.Input.PushKey(ConsoleKey.Enter);

        var prompt = Install.CreateRuntimePrompt();
        var selection = prompt.Show(_testConsole);

        Assert.Equal("Mono", selection);
    }

    [Fact]
    public void RuntimePrompt_ShouldReturnStandard_WhenUserSelectsStandard()
    {
        _testConsole.Input.PushKey(ConsoleKey.Enter);

        var prompt = Install.CreateRuntimePrompt();
        var selection = prompt.Show(_testConsole);

        Assert.Equal("Standard", selection);
    }
}
