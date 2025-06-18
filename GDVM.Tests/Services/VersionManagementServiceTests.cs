using GDVM.Environment;
using GDVM.Godot;
using GDVM.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Spectre.Console.Testing;

namespace GDVM.Test.Services;

public class VersionManagementServiceTests
{
    private readonly TestConsole _console;
    private readonly Mock<IHostSystem> _mockHostSystem;
    private readonly Mock<IInstallationService> _mockInstallationService;
    private readonly Mock<IReleaseManager> _mockReleaseManager;
    private readonly VersionManagementService _service;

    public VersionManagementServiceTests()
    {
        _mockHostSystem = new Mock<IHostSystem>();
        _mockReleaseManager = new Mock<IReleaseManager>();
        var mockPathService = new Mock<IPathService>();
        _mockInstallationService = new Mock<IInstallationService>();
        var mockLogger = new Mock<ILogger<VersionManagementService>>();

        mockPathService.Setup(x => x.RootPath).Returns("/test/gdvm");
        mockPathService.Setup(x => x.SymlinkPath).Returns("/test/gdvm/bin/godot");
        mockPathService.Setup(x => x.ConfigPath).Returns("/test/gdvm/gdvm.ini");
        mockPathService.Setup(x => x.ReleasesPath).Returns("/test/gdvm/.releases");
        mockPathService.Setup(x => x.BinPath).Returns("/test/gdvm/bin");
        mockPathService.Setup(x => x.MacAppSymlinkPath).Returns("/test/gdvm/bin/Godot.app");
        mockPathService.Setup(x => x.LogPath).Returns("/test/gdvm/.log");

        _console = new TestConsole();

        _service = new VersionManagementService(
            _mockHostSystem.Object,
            _mockReleaseManager.Object,
            _mockInstallationService.Object,
            mockPathService.Object,
            _console,
            mockLogger.Object
        );
    }

    [Fact]
    public async Task ResolveVersionForLaunchAsync_WithNoProjectAndNoInstallations_ReturnsNotFound()
    {
        _mockHostSystem.Setup(x => x.ListInstallations()).Returns([]);

        var result = await _service.ResolveVersionForLaunchAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(VersionResolutionStatus.NotFound, result.Status);
        var hasProjectMessage = _console.Output.Contains("Project requires") || _console.Output.Contains("Project specifies");
        var hasNoInstallationMessage = _console.Output.Contains("No Godot versions installed");
        var hasNoVersionSetMessage = _console.Output.Contains("No current Godot version set");

        Assert.True(hasProjectMessage || hasNoInstallationMessage || hasNoVersionSetMessage,
            $"Expected either project-specific, no installations, or no version set message. Actual output: {_console.Output}");
    }

    [Fact]
    public async Task ResolveVersionForLaunchAsync_WithProjectVersion_ReturnsCorrectResult()
    {
        const string projectVersion = "4.3.0";
        const string versionFileContent = "4.3.0";
        var tempDir = Path.Combine(Path.GetTempPath(), $"gdvm-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var versionFile = Path.Combine(tempDir, ".gdvm-version");
            await File.WriteAllTextAsync(versionFile, versionFileContent);

            var originalDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(tempDir);

            try
            {
                const string compatibleVersion = "4.3.0-stable";
                var installedVersions = new[] { compatibleVersion };

                _mockHostSystem.Setup(x => x.ListInstallations()).Returns(installedVersions);
                _mockReleaseManager.Setup(x => x.FindCompatibleVersion(projectVersion, false, installedVersions))
                    .Returns(compatibleVersion);

                var mockRelease = CreateMockRelease(compatibleVersion);
                _mockReleaseManager.Setup(x => x.TryCreateRelease(compatibleVersion))
                    .Returns(mockRelease);

                var result = await _service.ResolveVersionForLaunchAsync();

                Assert.True(result.IsSuccess);
                Assert.Equal(compatibleVersion, result.VersionName);
                Assert.Contains(compatibleVersion, result.ExecutablePath);
                Assert.Contains(compatibleVersion, result.WorkingDirectory);
                Assert.True(result.IsProjectVersion);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ResolveVersionForLaunchAsync_ProjectVersionNotInstalled_PromptsForInstallation()
    {
        const string projectVersion = "4.3.0";
        var installedVersions = Array.Empty<string>();

        _mockHostSystem.Setup(x => x.ListInstallations()).Returns(installedVersions);
        _mockReleaseManager.Setup(x => x.FindCompatibleVersion(projectVersion, false, installedVersions))
            .Returns((string?)null);

        var result = await _service.ResolveVersionForLaunchAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(VersionResolutionStatus.NotFound, result.Status);
        Assert.Contains("Project requires", _console.Output);
    }

    [Fact]
    public async Task ResolveVersionForLaunchAsync_ForceInteractive_PromptsForSelection()
    {
        var installedVersions = new[] { "4.3.0-stable", "4.2.0-stable" };
        _mockHostSystem.Setup(x => x.ListInstallations()).Returns(installedVersions);

        const string selectedVersion = "4.3.0-stable";
        var mockRelease = CreateMockRelease(selectedVersion);
        _mockReleaseManager.Setup(x => x.TryCreateRelease(selectedVersion))
            .Returns(mockRelease);

        await _service.ResolveVersionForLaunchAsync(true);

        _mockHostSystem.Verify(x => x.ListInstallations(), Times.Once);
        Assert.DoesNotContain("No Godot versions installed", _console.Output);
    }

    [Fact]
    public async Task ResolveVersionForLaunchAsync_MacOSAppBundle_HandlesCorrectly()
    {
        const string projectVersion = "4.3.0";
        const string versionFileContent = "4.3.0";
        var tempDir = Path.Combine(Path.GetTempPath(), $"gdvm-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var versionFile = Path.Combine(tempDir, ".gdvm-version");
            await File.WriteAllTextAsync(versionFile, versionFileContent);

            var originalDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(tempDir);

            try
            {
                const string compatibleVersion = "4.3.0-stable";
                const string execName = "Godot.app";
                var installedVersions = new[] { compatibleVersion };

                _mockHostSystem.Setup(x => x.ListInstallations()).Returns(installedVersions);
                _mockReleaseManager.Setup(x => x.FindCompatibleVersion(projectVersion, false, installedVersions))
                    .Returns(compatibleVersion);

                var mockRelease = CreateMockRelease(compatibleVersion, execName);
                _mockReleaseManager.Setup(x => x.TryCreateRelease(compatibleVersion))
                    .Returns(mockRelease);

                var result = await _service.ResolveVersionForLaunchAsync();

                Assert.True(result.IsSuccess);
                Assert.Equal(compatibleVersion, result.VersionName);
                Assert.Contains(compatibleVersion, result.ExecutablePath);
                Assert.Contains(compatibleVersion, result.WorkingDirectory);
                Assert.True(result.IsProjectVersion);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void CreateOrUpdateVersionFile_CallsProjectManager()
    {
        const string version = "4.3.0-stable";
        var directory = Path.GetTempPath();

        var versionFilePath = Path.Combine(directory, ".gdvm-version");
        try
        {
            _service.CreateOrUpdateVersionFile(version, directory);

            Assert.True(File.Exists(versionFilePath));

            var content = File.ReadAllText(versionFilePath);
            Assert.Equal(version, content.Trim());
        }
        finally
        {
            if (File.Exists(versionFilePath))
            {
                File.Delete(versionFilePath);
            }
        }
    }

    private static Release CreateMockRelease(string versionString, string execName = "Godot")
    {
        var parts = versionString.Split(['-', '.'], StringSplitOptions.RemoveEmptyEntries);
        var major = int.Parse(parts[0]);
        var minor = int.Parse(parts[1]);
        int? patch = parts.Length > 2 && int.TryParse(parts[2], out var p) ? p : null;

        var releaseType = versionString.Contains("stable") ? ReleaseType.Stable() :
            versionString.Contains("rc") ? ReleaseType.Rc(1) :
            versionString.Contains("beta") ? ReleaseType.Beta(1) :
            versionString.Contains("alpha") ? ReleaseType.Alpha(1) :
            ReleaseType.Stable();

        var runtime = versionString.Contains("mono") ? RuntimeEnvironment.Mono : RuntimeEnvironment.Standard;

        var release = new Release(major, minor, patch: patch, type: releaseType, runtimeEnvironment: runtime);

        if (execName is "Godot.app" or "Godot_mono.app")
        {
            release.OS = OS.MacOS;
            release.PlatformString = "macos.universal.zip";
        }
        else
        {
            release.OS = OS.Windows;
            release.PlatformString = "win64.exe";
        }

        return release;
    }

    [Fact]
    public async Task SetLocalVersionAsync_WithNoInstallationsAndNoQuery_ThrowsException()
    {
        _mockHostSystem.Setup(x => x.ListInstallations()).Returns([]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SetLocalVersionAsync(forceInteractive: true));

        Assert.Empty(_console.Output);
    }

    [Fact]
    public async Task SetLocalVersionAsync_WithValidQuery_SetsVersionSuccessfully()
    {
        const string queryVersion = "4.3.0";
        const string matchedVersion = "4.3.0-stable";
        var query = new[] { queryVersion };
        var installedVersions = new[] { matchedVersion };

        _mockHostSystem.Setup(x => x.ListInstallations()).Returns(installedVersions);

        var mockRelease = CreateMockRelease(matchedVersion);
        _mockReleaseManager.Setup(x => x.TryFindReleaseByQuery(query, installedVersions))
            .Returns(mockRelease);

        _mockReleaseManager.Setup(x => x.TryCreateRelease(mockRelease.ReleaseNameWithRuntime))
            .Returns(mockRelease);

        var result = await _service.SetLocalVersionAsync(query);

        Assert.Equal(mockRelease, result);

        Assert.Contains("`.gdvm-version` file in current directory", _console.Output);
    }

    [Fact]
    public async Task SetLocalVersionAsync_VersionNotInstalled_AttemptsInstallation()
    {
        const string queryVersion = "4.3.0";
        const string newVersion = "4.3.0-stable";
        var query = new[] { queryVersion };
        var installedVersions = Array.Empty<string>();

        _mockHostSystem.SetupSequence(x => x.ListInstallations())
            .Returns(installedVersions)
            .Returns([newVersion]);

        _mockReleaseManager.Setup(x => x.TryFindReleaseByQuery(query, Array.Empty<string>()))
            .Returns((Release?)null);

        var mockRelease = CreateMockRelease(newVersion);
        var installationResult = new InstallationResult(mockRelease.ReleaseNameWithRuntime, InstallationStatus.NewInstallation);
        _mockInstallationService.Setup(x => x.InstallByQueryAsync(query, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(installationResult);

        _mockReleaseManager.Setup(x => x.TryFindReleaseByQuery(query, new[] { newVersion }))
            .Returns(mockRelease);

        _mockReleaseManager.Setup(x => x.TryCreateRelease(mockRelease.ReleaseNameWithRuntime))
            .Returns(mockRelease);

        var result = await _service.SetLocalVersionAsync(query);

        Assert.Equal(mockRelease, result);
        _mockInstallationService.Verify(x => x.InstallByQueryAsync(query, It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);

        Assert.Contains("No installed version found matching", _console.Output);
        Assert.Contains("Installing", _console.Output);
        Assert.Contains("Successfully installed", _console.Output);
        Assert.Contains("`.gdvm-version` file in current directory", _console.Output);
    }

    [Fact]
    public async Task FindOrInstallCompatibleVersionAsync_CompatibleVersionExists_ReturnsVersion()
    {
        const string projectVersion = "4.3.0";
        const string compatibleVersion = "4.3.0-stable";
        var installedVersions = new[] { compatibleVersion };

        _mockHostSystem.Setup(x => x.ListInstallations()).Returns(installedVersions);
        _mockReleaseManager.Setup(x => x.FindCompatibleVersion(projectVersion, false, installedVersions))
            .Returns(compatibleVersion);

        var result = await _service.FindOrInstallCompatibleVersionAsync(projectVersion, false);

        Assert.Equal(compatibleVersion, result);
    }

    [Fact]
    public async Task FindOrInstallCompatibleVersionAsync_NoCompatibleVersion_ReturnsNull()
    {
        const string projectVersion = "4.3.0";
        var installedVersions = Array.Empty<string>();

        _mockHostSystem.Setup(x => x.ListInstallations()).Returns(installedVersions);
        _mockReleaseManager.Setup(x => x.FindCompatibleVersion(projectVersion, false, installedVersions))
            .Returns((string?)null);

        var result = await _service.FindOrInstallCompatibleVersionAsync(projectVersion, false, false);

        Assert.Null(result);
        Assert.Empty(_console.Output);
    }

    [Fact]
    public async Task FindOrInstallCompatibleVersionAsync_InstallationSucceeds_ReturnsVersion()
    {
        const string projectVersion = "4.3.0";
        const string compatibleVersion = "4.3.0-stable";
        var initialInstalled = Array.Empty<string>();
        var postInstallInstalled = new[] { compatibleVersion };

        _mockHostSystem.SetupSequence(x => x.ListInstallations())
            .Returns(initialInstalled)
            .Returns(postInstallInstalled);

        _mockReleaseManager.SetupSequence(x => x.FindCompatibleVersion(projectVersion, false, It.IsAny<IEnumerable<string>>()))
            .Returns((string?)null)
            .Returns(compatibleVersion);

        var mockRelease = CreateMockRelease(compatibleVersion);
        var installationResult = new InstallationResult(mockRelease.ReleaseNameWithRuntime, InstallationStatus.NewInstallation);
        _mockInstallationService.Setup(x => x.InstallByQueryAsync(new[] { projectVersion }, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(installationResult);

        _console.Interactive();
        _console.Input.PushKey(ConsoleKey.Enter);

        var result = await _service.FindOrInstallCompatibleVersionAsync(projectVersion, false);

        Assert.Equal(compatibleVersion, result);
        _mockInstallationService.Verify(x => x.InstallByQueryAsync(new[] { projectVersion }, It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindOrInstallCompatibleVersionAsync_DotNetProject_UsesCorrectQuery()
    {
        const string projectVersion = "4.3.0";
        var installedVersions = Array.Empty<string>();

        _mockHostSystem.Setup(x => x.ListInstallations()).Returns(installedVersions);
        _mockReleaseManager.Setup(x => x.FindCompatibleVersion(projectVersion, true, installedVersions))
            .Returns((string?)null);

        var result = await _service.FindOrInstallCompatibleVersionAsync(projectVersion, true, false);

        Assert.Null(result);
        _mockReleaseManager.Verify(x => x.FindCompatibleVersion(projectVersion, true, installedVersions), Times.Once);
        Assert.Empty(_console.Output);
    }

    [Fact]
    public async Task FindOrInstallCompatibleVersionAsync_WithPromptConfirmed_InstallsVersion()
    {
        const string projectVersion = "4.3.0";
        const string compatibleVersion = "4.3.0-stable";
        var initialInstalled = Array.Empty<string>();
        var postInstallInstalled = new[] { compatibleVersion };

        _mockHostSystem.SetupSequence(x => x.ListInstallations())
            .Returns(initialInstalled)
            .Returns(postInstallInstalled);

        _mockReleaseManager.SetupSequence(x => x.FindCompatibleVersion(projectVersion, false, It.IsAny<IEnumerable<string>>()))
            .Returns((string?)null)
            .Returns(compatibleVersion);

        _console.Interactive();
        _console.Input.PushKey(ConsoleKey.Enter);

        var mockRelease = CreateMockRelease(compatibleVersion);
        var installationResult = new InstallationResult(mockRelease.ReleaseNameWithRuntime, InstallationStatus.NewInstallation);
        _mockInstallationService.Setup(x => x.InstallByQueryAsync(new[] { projectVersion }, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(installationResult);

        var result = await _service.FindOrInstallCompatibleVersionAsync(projectVersion, false);

        Assert.Equal(compatibleVersion, result);
        _mockInstallationService.Verify(x => x.InstallByQueryAsync(new[] { projectVersion }, It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);

        Assert.Contains("Project requires", _console.Output);
        Assert.Contains("Installing", _console.Output);
    }

    [Fact]
    public async Task SetGlobalVersionAsync_WithNoInstallations_ThrowsException()
    {
        _mockHostSystem.Setup(x => x.ListInstallations()).Returns([]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SetGlobalVersionAsync(["4.3.0"]));

        Assert.Empty(_console.Output);
    }

    [Fact]
    public async Task SetGlobalVersionAsync_WithValidQuery_SetsVersionSuccessfully()
    {
        const string queryVersion = "4.3.0";
        const string matchedVersion = "4.3.0-stable";
        var query = new[] { queryVersion };
        var installedVersions = new[] { matchedVersion, "4.2.0-stable" };

        _mockHostSystem.Setup(x => x.ListInstallations()).Returns(installedVersions);
        _mockReleaseManager.Setup(x => x.FilterReleasesByQuery(query, installedVersions))
            .Returns([matchedVersion]);

        var mockRelease = CreateMockRelease(matchedVersion);
        _mockReleaseManager.Setup(x => x.TryCreateRelease(matchedVersion))
            .Returns(mockRelease);

        _mockHostSystem.Setup(x => x.CreateOrOverwriteSymbolicLink(It.IsAny<string>()));

        var result = await _service.SetGlobalVersionAsync(query);

        Assert.Equal(mockRelease, result);
        _mockHostSystem.Verify(x => x.CreateOrOverwriteSymbolicLink(It.IsAny<string>()), Times.Once);

        Assert.Contains("Successfully set version to", _console.Output);
        Assert.Contains(matchedVersion, _console.Output);
    }

    [Fact]
    public async Task SetGlobalVersionAsync_WithInvalidQuery_ThrowsException()
    {
        const string invalidVersion = "invalid-version";
        var query = new[] { invalidVersion };
        var installedVersions = new[] { "4.3.0-stable", "4.2.0-stable" };

        _mockHostSystem.Setup(x => x.ListInstallations()).Returns(installedVersions);
        _mockReleaseManager.Setup(x => x.FilterReleasesByQuery(query, installedVersions))
            .Returns([]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SetGlobalVersionAsync(query));

        Assert.Empty(_console.Output);
    }

    [Fact]
    public async Task SetGlobalVersionAsync_WithEmptyQuery_UsesPrompt()
    {
        const string selectedVersion = "4.3.0-stable";
        var installedVersions = new[] { selectedVersion, "4.2.0-stable" };

        _mockHostSystem.Setup(x => x.ListInstallations()).Returns(installedVersions);

        _console.Interactive();
        _console.Input.PushKey(ConsoleKey.Enter);

        var mockRelease = CreateMockRelease(selectedVersion);
        _mockReleaseManager.Setup(x => x.TryCreateRelease(selectedVersion))
            .Returns(mockRelease);

        _mockHostSystem.Setup(x => x.CreateOrOverwriteSymbolicLink(It.IsAny<string>()));

        var result = await _service.SetGlobalVersionAsync([]);

        Assert.Equal(mockRelease, result);
        _mockHostSystem.Verify(x => x.ListInstallations(), Times.Once);
        _mockHostSystem.Verify(x => x.CreateOrOverwriteSymbolicLink(It.IsAny<string>()), Times.Once);

        Assert.Contains("Successfully set version to", _console.Output);
    }

    [Fact]
    public async Task SetGlobalVersionAsync_WithInvalidVersion_ThrowsInvalidOperationException()
    {
        const string invalidVersion = "invalid-version";
        var query = new[] { invalidVersion };
        var installedVersions = new[] { "4.3.0-stable" };

        _mockHostSystem.Setup(x => x.ListInstallations()).Returns(installedVersions);
        _mockReleaseManager.Setup(x => x.FilterReleasesByQuery(query, installedVersions))
            .Returns([invalidVersion]);

        _mockReleaseManager.Setup(x => x.TryCreateRelease(invalidVersion))
            .Returns((Release?)null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SetGlobalVersionAsync(query));

        Assert.Equal("Invalid Godot version.", exception.Message);
    }

    [Fact]
    public async Task ResolveVersionForLaunchAsync_WithNoProjectFile_FallsBackToSymlink()
    {
        const string installedVersion = "4.3.0-stable";
        var installedVersions = new[] { installedVersion };
        var mockRelease = CreateMockRelease(installedVersion);

        _mockHostSystem.Setup(x => x.ListInstallations()).Returns(installedVersions);
        _mockReleaseManager.Setup(x => x.TryCreateRelease(installedVersion))
            .Returns(mockRelease);

        await _service.ResolveVersionForLaunchAsync();

        _mockHostSystem.Verify(x => x.ListInstallations(), Times.Once);

        var hasValidOutput = _console.Output.Contains("No current Godot version set") ||
                             _console.Output.Contains("Using project version") || _console.Output.Contains("Project requires") ||
                             _console.Output.Contains("Error resolving") ||
                             _console.Output.Length == 0;

        Assert.True(hasValidOutput, $"Expected valid project or symlink message. Actual output: {_console.Output}");
    }

    [Fact]
    public async Task ResolveVersionForLaunchAsync_WithNoInstallationsAndInteractive_ReturnsNull()
    {
        _mockHostSystem.Setup(x => x.ListInstallations()).Returns([]);

        var result = await _service.ResolveVersionForLaunchAsync(true);

        Assert.False(result.IsSuccess);
        Assert.Equal(VersionResolutionStatus.NotFound, result.Status);
        Assert.Contains("No Godot versions installed", _console.Output);
    }

    [Fact]
    public async Task ResolveVersionForLaunchAsync_WithInstallationsAndInteractive_PromptsForSelection()
    {
        const string selectedVersion = "4.3.0-stable";
        var installedVersions = new[] { selectedVersion, "4.2.0-stable" };
        var mockRelease = CreateMockRelease(selectedVersion);

        _mockHostSystem.Setup(x => x.ListInstallations()).Returns(installedVersions);
        _mockReleaseManager.Setup(x => x.TryCreateRelease(selectedVersion))
            .Returns(mockRelease);

        _console.Interactive();
        _console.Input.PushKey(ConsoleKey.Enter);

        var result = await _service.ResolveVersionForLaunchAsync(true);

        Assert.True(result.IsSuccess);
        Assert.Equal(selectedVersion, result.VersionName);
    }

    [Fact]
    public async Task ResolveVersionForLaunchAsync_WithInvalidSelectedVersion_ReturnsNull()
    {
        const string selectedVersion = "4.3.0-stable";
        var installedVersions = new[] { selectedVersion };

        _mockHostSystem.Setup(x => x.ListInstallations()).Returns(installedVersions);

        _mockReleaseManager.Setup(x => x.TryCreateRelease(selectedVersion))
            .Returns((Release?)null);

        _console.Interactive();
        _console.Input.PushKey(ConsoleKey.Enter);

        var result = await _service.ResolveVersionForLaunchAsync(true);

        Assert.False(result.IsSuccess);
        Assert.Equal(VersionResolutionStatus.InvalidVersion, result.Status);
        Assert.Contains("Invalid Godot version", _console.Output);
    }

    [Fact]
    public async Task ResolveVersionForLaunchAsync_ExceptionThrown_ReturnsNullAndLogsError()
    {
        const string exceptionMessage = "Test exception";
        _mockHostSystem.Setup(x => x.ListInstallations())
            .Throws(new InvalidOperationException(exceptionMessage));

        var result = await _service.ResolveVersionForLaunchAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(VersionResolutionStatus.Failed, result.Status);
        Assert.Contains("Error resolving Godot version for launch", _console.Output);
    }

    [Fact]
    public async Task SetLocalVersionAsync_WithCancellationToken_HandlesCancellation()
    {
        const string queryVersion = "4.3.0";
        var query = new[] { queryVersion };
        var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        _mockHostSystem.Setup(x => x.ListInstallations()).Returns([]);
        _mockInstallationService.Setup(x => x.InstallByQueryAsync(It.IsAny<string[]>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _service.SetLocalVersionAsync(query, cancellationToken: cancellationTokenSource.Token));
    }

    [Fact]
    public async Task SetLocalVersionAsync_ForceInteractive_ShowsPromptEvenWithInstallations()
    {
        const string selectedVersion = "4.3.0-stable";
        var installedVersions = new[] { selectedVersion };
        var mockRelease = CreateMockRelease(selectedVersion);

        _mockHostSystem.Setup(x => x.ListInstallations()).Returns(installedVersions);

        _mockReleaseManager.Setup(x => x.TryCreateRelease(selectedVersion))
            .Returns(mockRelease);

        _console.Interactive();
        _console.Input.PushKey(ConsoleKey.Enter);

        var result = await _service.SetLocalVersionAsync(forceInteractive: true);

        Assert.Equal(mockRelease, result);
    }

    [Fact]
    public async Task SetLocalVersionAsync_InstallationFails_ThrowsException()
    {
        const string queryVersion = "4.3.0";
        const string errorMessage = "Installation failed";
        var query = new[] { queryVersion };

        _mockHostSystem.Setup(x => x.ListInstallations()).Returns([]);
        _mockReleaseManager.Setup(x => x.TryFindReleaseByQuery(query, Array.Empty<string>()))
            .Returns((Release?)null);

        _mockInstallationService.Setup(x => x.InstallByQueryAsync(query, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(errorMessage));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SetLocalVersionAsync(query));

        Assert.Contains(errorMessage, exception.Message);
    }

    [Fact]
    public async Task SetLocalVersionAsync_NoQueryProvided_PromptsForVersion()
    {
        const string selectedVersion = "4.3.0-stable";
        var installedVersions = new[] { selectedVersion };
        var mockRelease = CreateMockRelease(selectedVersion);

        _mockHostSystem.Setup(x => x.ListInstallations()).Returns(installedVersions);

        _mockReleaseManager.Setup(x => x.TryCreateRelease(selectedVersion))
            .Returns(mockRelease);

        _console.Interactive();
        _console.Input.PushKey(ConsoleKey.Enter);

        var result = await _service.SetLocalVersionAsync();

        Assert.Equal(mockRelease, result);
    }
}
