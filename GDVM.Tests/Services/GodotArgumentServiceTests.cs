using GDVM.Services;

namespace GDVM.Test.Services;

public class GodotArgumentServiceTests
{
    private readonly GodotArgumentService _service = new();

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void ShouldForceAttachedMode_WithNullOrEmptyArguments_ReturnsFalse(string? argumentString)
    {
        var result = _service.ShouldForceAttachedMode(argumentString);
        Assert.False(result);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("--version")]
    [InlineData("--verbose")]
    [InlineData("-v")]
    [InlineData("--quiet")]
    [InlineData("-q")]
    [InlineData("--no-header")]
    public void ShouldForceAttachedMode_WithGeneralOptions_ReturnsTrue(string argument)
    {
        var result = _service.ShouldForceAttachedMode(argument);
        Assert.True(result);
    }

    [Theory]
    [InlineData("--debug")]
    [InlineData("-d")]
    [InlineData("--print-fps")]
    [InlineData("--debug-stringnames")]
    [InlineData("--gpu-profile")]
    [InlineData("--profiling")]
    [InlineData("--benchmark")]
    [InlineData("--benchmark-file")]
    public void ShouldForceAttachedMode_WithDebugOptions_ReturnsTrue(string argument)
    {
        var result = _service.ShouldForceAttachedMode(argument);
        Assert.True(result);
    }

    [Theory]
    [InlineData("--script")]
    [InlineData("-s")]
    [InlineData("--check-only")]
    [InlineData("--import")]
    [InlineData("--export-release")]
    [InlineData("--export-debug")]
    [InlineData("--export-pack")]
    [InlineData("--export-patch")]
    [InlineData("--convert-3to4")]
    [InlineData("--validate-conversion-3to4")]
    [InlineData("--doctool")]
    [InlineData("--gdscript-docs")]
    [InlineData("--gdextension-docs")]
    [InlineData("--build-solutions")]
    [InlineData("--dump-gdextension-interface")]
    [InlineData("--dump-extension-api")]
    [InlineData("--dump-extension-api-with-docs")]
    [InlineData("--validate-extension-api")]
    [InlineData("--install-android-build-template")]
    public void ShouldForceAttachedMode_WithStandaloneTools_ReturnsTrue(string argument)
    {
        var result = _service.ShouldForceAttachedMode(argument);
        Assert.True(result);
    }

    [Fact]
    public void ShouldForceAttachedMode_WithHeadlessOption_ReturnsTrue()
    {
        var result = _service.ShouldForceAttachedMode("--headless");
        Assert.True(result);
    }

    [Theory]
    [InlineData("--main-pack")]
    [InlineData("--resolution")]
    [InlineData("--windowed")]
    [InlineData("--maximized")]
    [InlineData("--fullscreen")]
    [InlineData("--position")]
    [InlineData("path/to/project")]
    public void ShouldForceAttachedMode_WithNonTerminalRequiringArguments_ReturnsFalse(string argument)
    {
        var result = _service.ShouldForceAttachedMode(argument);
        Assert.False(result);
    }

    [Theory]
    [InlineData("--help --version")]
    [InlineData("--verbose --debug")]
    [InlineData("--script myscript.gd")]
    [InlineData("--HELP")] // Test case insensitive
    [InlineData("--VERSION")]
    [InlineData("project.godot --script test.gd")]
    public void ShouldForceAttachedMode_WithMultipleArgumentsIncludingTerminalRequired_ReturnsTrue(string arguments)
    {
        var result = _service.ShouldForceAttachedMode(arguments);
        Assert.True(result);
    }

    [Theory]
    [InlineData("project.godot --resolution 1920x1080")]
    [InlineData("--windowed --position 100,100")]
    [InlineData("path/to/project --main-pack game.pck")]
    public void ShouldForceAttachedMode_WithMultipleArgumentsNotRequiringTerminal_ReturnsFalse(string arguments)
    {
        var result = _service.ShouldForceAttachedMode(arguments);
        Assert.False(result);
    }

    [Theory]
    [InlineData("--help", true)]
    [InlineData("-h", true)]
    [InlineData("--version", true)]
    [InlineData("--verbose", true)]
    [InlineData("-v", true)]
    [InlineData("--quiet", true)]
    [InlineData("-q", true)]
    [InlineData("--no-header", true)]
    [InlineData("--resolution", false)]
    [InlineData("--windowed", false)]
    [InlineData("", false)]
    public void HasGeneralOptions_WithVariousArguments_ReturnsExpectedResult(string arguments, bool expected)
    {
        var result = _service.HasGeneralOptions(arguments);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("--debug", true)]
    [InlineData("-d", true)]
    [InlineData("--print-fps", true)]
    [InlineData("--debug-stringnames", true)]
    [InlineData("--gpu-profile", true)]
    [InlineData("--profiling", true)]
    [InlineData("--benchmark", true)]
    [InlineData("--benchmark-file", true)]
    [InlineData("--help", false)]
    [InlineData("--windowed", false)]
    [InlineData("", false)]
    public void HasDebugOptions_WithVariousArguments_ReturnsExpectedResult(string arguments, bool expected)
    {
        var result = _service.HasDebugOptions(arguments);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("--script", true)]
    [InlineData("-s", true)]
    [InlineData("--check-only", true)]
    [InlineData("--import", true)]
    [InlineData("--export-release", true)]
    [InlineData("--export-debug", true)]
    [InlineData("--export-pack", true)]
    [InlineData("--export-patch", true)]
    [InlineData("--convert-3to4", true)]
    [InlineData("--validate-conversion-3to4", true)]
    [InlineData("--doctool", true)]
    [InlineData("--gdscript-docs", true)]
    [InlineData("--gdextension-docs", true)]
    [InlineData("--build-solutions", true)]
    [InlineData("--dump-gdextension-interface", true)]
    [InlineData("--dump-extension-api", true)]
    [InlineData("--dump-extension-api-with-docs", true)]
    [InlineData("--validate-extension-api", true)]
    [InlineData("--install-android-build-template", true)]
    [InlineData("--help", false)]
    [InlineData("--windowed", false)]
    [InlineData("", false)]
    public void HasStandaloneTools_WithVariousArguments_ReturnsExpectedResult(string arguments, bool expected)
    {
        var result = _service.HasStandaloneTools(arguments);
        Assert.Equal(expected, result);
    }
}
