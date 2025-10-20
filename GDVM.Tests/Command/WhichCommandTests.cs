using GDVM.Command;
using GDVM.Environment;
using GDVM.Types;
using GDVM.ViewModels;
using Moq;
using Spectre.Console.Testing;
using System.Text.Json;

namespace GDVM.Test.Command;

public sealed class WhichCommandTests
{
    [Fact]
    public void WhichCommand_WritesJson_WhenVersionIsSet()
    {
        var hostSystem = new Mock<IHostSystem>();
        var info = new SymlinkInfo("/Users/test/gdvm/4.5/bin/godot", "/Users/test/gdvm/4.5/Godot.app");
        hostSystem.Setup(x => x.ResolveCurrentSymlinks()).Returns(new Result<SymlinkInfo, SymlinkError>.Success(info));

        var console = new TestConsole();
        var command = new WhichCommand(hostSystem.Object, console);

        command.Which(true);

        var json = console.Output.Trim();
        var view = JsonSerializer.Deserialize<WhichView>(json, JsonView.Options);
        Assert.True(view.HasVersion);
        Assert.Equal(info.SymlinkPath, view.SymlinkPath);
    }

    [Fact]
    public void WhichCommand_WritesJson_WhenNoVersionSet()
    {
        var hostSystem = new Mock<IHostSystem>();
        hostSystem.Setup(x => x.ResolveCurrentSymlinks())
            .Returns(new Result<SymlinkInfo, SymlinkError>.Failure(new SymlinkError.NoVersionSet()));

        var console = new TestConsole();
        var command = new WhichCommand(hostSystem.Object, console);

        command.Which(true);

        var json = console.Output.Trim();
        var view = JsonSerializer.Deserialize<WhichView>(json, JsonView.Options);
        Assert.False(view.HasVersion);
        Assert.Equal("No Godot version is currently set.", view.Message);
    }
}
