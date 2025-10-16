using GDVM.Godot;
using GDVM.Test.Godot.ReleaseManager;

namespace GDVM.Test.Godot;

public sealed class ProjectManagerTests : IDisposable
{
    private readonly ProjectManager _projectManager;
    private readonly string _tempDirectory;

    public ProjectManagerTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"gdvm-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
        var releaseManager = new ReleaseManagerBuilder().Build();
        _projectManager = new ProjectManager(releaseManager);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public void FindProjectVersion_WithGdvmVersionFile_ReturnsVersionFromFile()
    {
        const string versionContent = "4.3-stable";
        var versionFilePath = Path.Combine(_tempDirectory, ".gdvm-version");
        File.WriteAllText(versionFilePath, versionContent);

        var result = _projectManager.FindProjectVersion(_tempDirectory);

        Assert.Equal("4.3-stable-standard", result);
    }

    [Fact]
    public void FindProjectVersion_WithGdvmVersionFileContainingWhitespace_ReturnsTrimedVersion()
    {
        const string versionContent = "  4.3-stable  \n";
        var versionFilePath = Path.Combine(_tempDirectory, ".gdvm-version");
        File.WriteAllText(versionFilePath, versionContent);

        var result = _projectManager.FindProjectVersion(_tempDirectory);

        Assert.Equal("4.3-stable-standard", result);
    }

    [Fact]
    public void FindProjectVersion_WithEmptyGdvmVersionFile_FallsBackToProjectGodot()
    {
        var versionFilePath = Path.Combine(_tempDirectory, ".gdvm-version");
        File.WriteAllText(versionFilePath, "   \n");

        var projectFilePath = Path.Combine(_tempDirectory, "project.godot");
        const string projectContent = """
                                      [application]
                                      config/name="Test Project"
                                      config/features=PackedStringArray("4.3", "Forward Plus")
                                      """;

        File.WriteAllText(projectFilePath, projectContent);

        var result = _projectManager.FindProjectVersion(_tempDirectory);

        Assert.Equal("4.3-stable-standard", result);
    }

    [Fact]
    public void FindProjectVersion_WithProjectGodotFile_ReturnsVersionFromFeatures()
    {
        var projectFilePath = Path.Combine(_tempDirectory, "project.godot");
        const string projectContent = """
                                      [application]
                                      config/name="Test Project"
                                      config/features=PackedStringArray("4.3", "Forward Plus")
                                      """;

        File.WriteAllText(projectFilePath, projectContent);

        var result = _projectManager.FindProjectVersion(_tempDirectory);

        Assert.Equal("4.3-stable-standard", result);
    }

    [Fact]
    public void FindProjectVersion_WithProjectGodotFileContainingMultipleVersions_ReturnsFirstValidVersion()
    {
        var projectFilePath = Path.Combine(_tempDirectory, "project.godot");
        const string projectContent = """
                                      [application]
                                      config/name="Test Project"
                                      config/features=PackedStringArray("4.3", "4.2", "Forward Plus")
                                      """;

        File.WriteAllText(projectFilePath, projectContent);

        var result = _projectManager.FindProjectVersion(_tempDirectory);

        Assert.Equal("4.3-stable-standard", result);
    }

    [Fact]
    public void FindProjectVersion_WithNoFiles_ReturnsNull()
    {
        var result = _projectManager.FindProjectVersion(_tempDirectory);

        Assert.Null(result);
    }

    [Fact]
    public void FindProjectVersion_WithInvalidProjectGodotFile_ReturnsNull()
    {
        var projectFilePath = Path.Combine(_tempDirectory, "project.godot");
        const string projectContent = """
                                      [application]
                                      config/name="Test Project"
                                      config/features=PackedStringArray("Forward Plus")
                                      """;

        File.WriteAllText(projectFilePath, projectContent);

        var result = _projectManager.FindProjectVersion(_tempDirectory);

        Assert.Null(result);
    }

    [Fact]
    public void FindProjectInfo_WithGdvmVersionFile_ReturnsReleaseWithoutDotNet()
    {
        const string versionContent = "4.3-stable";
        var versionFilePath = Path.Combine(_tempDirectory, ".gdvm-version");
        File.WriteAllText(versionFilePath, versionContent);

        var result = _projectManager.FindProjectInfo(_tempDirectory);

        Assert.NotNull(result);
        Assert.Equal("4.3-stable-standard", result.ReleaseNameWithRuntime);
        Assert.False(result.IsDotNet);
    }

    [Fact]
    public void FindProjectInfo_WithGdvmVersionFileContainingMono_ReturnsReleaseWithDotNet()
    {
        const string versionContent = "4.3-stable-mono";
        var versionFilePath = Path.Combine(_tempDirectory, ".gdvm-version");
        File.WriteAllText(versionFilePath, versionContent);

        var result = _projectManager.FindProjectInfo(_tempDirectory);

        Assert.NotNull(result);
        Assert.Equal("4.3-stable-mono", result.ReleaseNameWithRuntime);
        Assert.True(result.IsDotNet);
    }

    [Fact]
    public void FindProjectInfo_WithProjectGodotDotNetProject_ReturnsProjectInfoWithDotNet()
    {
        var projectFilePath = Path.Combine(_tempDirectory, "project.godot");
        const string projectContent = """
                                      [application]
                                      config/name="Test Project"
                                      config/features=PackedStringArray("4.3", "C#")

                                      [dotnet]
                                      project/assembly_name="TestProject"
                                      """;

        File.WriteAllText(projectFilePath, projectContent);

        var result = _projectManager.FindProjectInfo(_tempDirectory);

        Assert.NotNull(result);
        Assert.Equal("4.3-stable-mono", result.ReleaseNameWithRuntime);
        Assert.True(result.IsDotNet);
    }

    [Fact]
    public void FindProjectInfo_WithProjectGodotStandardProject_ReturnsProjectInfoWithoutDotNet()
    {
        var projectFilePath = Path.Combine(_tempDirectory, "project.godot");
        const string projectContent = """
                                      [application]
                                      config/name="Test Project"
                                      config/features=PackedStringArray("4.3", "Forward Plus")
                                      """;

        File.WriteAllText(projectFilePath, projectContent);

        var result = _projectManager.FindProjectInfo(_tempDirectory);

        Assert.NotNull(result);
        Assert.Equal("4.3-stable-standard", result.ReleaseNameWithRuntime);
        Assert.False(result.IsDotNet);
    }

    [Theory]
    [InlineData("4.3", "4.3-stable-standard")]
    [InlineData("4.3.1", "4.3.1-stable-standard")]
    [InlineData("4.2", "4.2-stable-standard")]
    [InlineData("4.10.5", "4.10.5-stable-standard")]
    public void FindProjectInfo_WithValidVersionFormats_ReturnsCorrectVersion(string version, string expectedRelease)
    {
        var projectFilePath = Path.Combine(_tempDirectory, "project.godot");
        var projectContent = $"""
                              [application]
                              config/name="Test Project"
                              config/features=PackedStringArray("{version}", "Forward Plus")
                              """;

        File.WriteAllText(projectFilePath, projectContent);

        var result = _projectManager.FindProjectInfo(_tempDirectory);

        Assert.NotNull(result);
        Assert.Equal(expectedRelease, result.ReleaseNameWithRuntime);
    }

    [Theory]
    [InlineData("config/features=PackedStringArray(\"4.3\", \"Forward Plus\")")]
    [InlineData("config/features=PackedStringArray( \"4.3\" , \"Forward Plus\" )")]
    [InlineData("config/features=PackedStringArray(\"Forward Plus\", \"4.3\")")]
    public void FindProjectInfo_WithDifferentFeatureFormatting_ParsesCorrectly(string featuresLine)
    {
        var projectFilePath = Path.Combine(_tempDirectory, "project.godot");
        var projectContent = $"""
                              [application]
                              config/name="Test Project"
                              {featuresLine}
                              """;

        File.WriteAllText(projectFilePath, projectContent);

        var result = _projectManager.FindProjectInfo(_tempDirectory);

        Assert.NotNull(result);
        Assert.Equal("4.3-stable-standard", result.ReleaseNameWithRuntime);
    }

    [Fact]
    public void FindProjectInfo_WithMalformedProjectGodotFile_ReturnsNull()
    {
        var projectFilePath = Path.Combine(_tempDirectory, "project.godot");
        const string projectContent = """
                                      [application]
                                      config/name="Test Project"
                                      config/features=PackedStringArray("Forward Plus"
                                      """;

        File.WriteAllText(projectFilePath, projectContent);

        var result = _projectManager.FindProjectInfo(_tempDirectory);

        Assert.Null(result);
    }

    [Fact]
    public void CreateVersionFile_CreatesFileWithCorrectContent()
    {
        const string version = "4.3-stable";

        _projectManager.CreateVersionFile(version, _tempDirectory);

        var filePath = Path.Combine(_tempDirectory, ".gdvm-version");
        Assert.True(File.Exists(filePath));
        var content = File.ReadAllText(filePath);
        Assert.Equal($"4.3-stable{System.Environment.NewLine}", content);
    }

    [Fact]
    public void CreateVersionFile_WithNullDirectory_UsesCurrentDirectory()
    {
        var originalDirectory = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(_tempDirectory);
            const string version = "4.3-stable";

            _projectManager.CreateVersionFile(version);

            var filePath = Path.Combine(_tempDirectory, ".gdvm-version");
            Assert.True(File.Exists(filePath));
            var content = File.ReadAllText(filePath);
            Assert.Equal($"4.3-stable{System.Environment.NewLine}", content);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    [Fact]
    public void FindProjectVersion_WithNullDirectory_UsesCurrentDirectory()
    {
        var originalDirectory = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(_tempDirectory);
            var versionFilePath = Path.Combine(_tempDirectory, ".gdvm-version");
            File.WriteAllText(versionFilePath, "4.3-stable");

            var result = _projectManager.FindProjectVersion();

            Assert.Equal("4.3-stable-standard", result);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    [Fact]
    public void FindProjectInfo_PrioritizesGdvmVersionFileOverProjectGodot()
    {
        var versionFilePath = Path.Combine(_tempDirectory, ".gdvm-version");
        File.WriteAllText(versionFilePath, "4.4-dev5");

        var projectFilePath = Path.Combine(_tempDirectory, "project.godot");
        const string projectContent = """
                                      [application]
                                      config/name="Test Project"
                                      config/features=PackedStringArray("4.3", "Forward Plus")
                                      """;

        File.WriteAllText(projectFilePath, projectContent);

        var result = _projectManager.FindProjectInfo(_tempDirectory);

        Assert.NotNull(result);
        Assert.Equal("4.4-dev5-standard", result.ReleaseNameWithRuntime);
    }
}
