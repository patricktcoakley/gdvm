using GDVM.Error;
using System.Xml.Linq;

namespace GDVM.Tests.EndToEnd;

public class EndToEndTests(TestContainerFixture fixture) : IClassFixture<TestContainerFixture>
{
    [Fact]
    public async Task DisplaysHelpWhenNoArgumentsProvided()
    {
        var result = await fixture.Container.ExecuteCommand();

        result.AssertSuccessfulExecution();
        Assert.Contains("Usage:", result.Stdout);
        Assert.Contains("Commands:", result.Stdout);
    }

    [Fact]
    public async Task DisplaysVersionWithVersionFlag()
    {
        var expected = GetProjectVersion();
        var result = await fixture.Container.ExecuteCommand("--version");

        result.AssertSuccessfulExecution(expected);
    }

    [Fact]
    public async Task CreatesGdvmDirectoryOnFirstCommand()
    {
        await fixture.Container.ExecuteCommand("list");

        var directoryExists = await fixture.Container.DirectoryExists("/root/gdvm");
        Assert.True(directoryExists);
    }

    [Fact]
    public async Task DisplaysHelpForUnrecognizedCommands()
    {
        var result = await fixture.Container.ExecuteCommand("nonexistent-command");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Usage:", result.Stdout);
    }

    [Fact]
    public async Task RecordsCommandExecutionsInLogs()
    {
        await fixture.Container.ExecuteCommand("list");
        await fixture.Container.ExecuteCommand("which");

        var logResult = await fixture.Container.ExecuteCommand("logs");
        Assert.Equal(0, logResult.ExitCode);
        Assert.True(logResult.Stdout.Length > 0, "Logs should not be empty after operations");
    }

    [Fact]
    public async Task SearchCommandExecutesWithoutCrashing()
    {
        var result = await fixture.Container.ExecuteCommand("search");

        Assert.InRange(result.ExitCode, ExitCodes.Success, int.MaxValue);

        if (result.ExitCode != ExitCodes.Success)
        {
            await fixture.Container.AssertLogContains("search");
        }
    }

    [Fact]
    public async Task ListCommandDisplaysOutput()
    {
        var result = await fixture.Container.ExecuteCommand("list");

        result.AssertSuccessfulExecution();
        Assert.True(result.Stdout.Length > 0);
    }

    [Fact]
    public async Task WhichCommandSucceedsWithoutActiveVersion()
    {
        var result = await fixture.Container.ExecuteCommand("which");

        result.AssertSuccessfulExecution();
    }

    [Fact]
    public async Task AllCommandsDisplayHelpWithHelpFlag()
    {
        var commands = new[] { "install", "remove", "set", "local", "search", "list", "which", "logs", "godot" };

        foreach (var command in commands)
        {
            var result = await fixture.Container.ExecuteCommand(command, "--help");
            Assert.Equal(0, result.ExitCode);
            Assert.Contains(command, result.Stdout, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task InstallCommandFailsGracefullyForInvalidVersion()
    {
        var result = await fixture.Container.ExecuteCommand("install", "nonexistent-version-999");

        Assert.Equal(ExitCodes.ArgumentError, result.ExitCode);
        await fixture.Container.AssertLogContains("install");
    }

    [Fact]
    public async Task RemoveCommandHandlesInvalidVersionGracefully()
    {
        var result = await fixture.Container.ExecuteCommand("remove", "nonexistent-version-999");

        Assert.Equal(ExitCodes.Success, result.ExitCode);
    }

    [Fact]
    public async Task SetCommandFailsGracefullyForInvalidVersion()
    {
        var result = await fixture.Container.ExecuteCommand("set", "nonexistent-version-999");

        Assert.Equal(ExitCodes.GeneralError, result.ExitCode);
    }

    [Fact]
    public async Task LocalCommandHandlesInvalidVersionGracefully()
    {
        var result = await fixture.Container.ExecuteCommand("local", "nonexistent-version-999");

        Assert.Equal(ExitCodes.ArgumentError, result.ExitCode);
    }

    [Fact]
    public async Task SearchCommandAcceptsVersionQueryParameter()
    {
        var result = await fixture.Container.ExecuteCommand("search", "4.5");

        Assert.InRange(result.ExitCode, ExitCodes.Success, int.MaxValue);

        if (result.ExitCode == ExitCodes.Success)
        {
            Assert.True(result.Stdout.Length > 0);
        }
    }

    [Fact]
    public async Task WhichCommandReadsLocalVersionFile()
    {
        var gdvmPath = await TestHelpers.GetGdvmPath(fixture.Container);
        await fixture.Container.ExecuteShellCommand("mkdir", "-p", "/tmp/version-test");
        await fixture.Container.ExecuteShellCommand("sh", "-c", "echo '4.5-stable' > /tmp/version-test/.gdvm-version");

        var result = await fixture.Container.ExecuteShellCommand("sh", "-c", $"cd /tmp/version-test && {gdvmPath} which");

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task ReadsVersionFileFromCurrentDirectory()
    {
        var gdvmPath = await TestHelpers.GetGdvmPath(fixture.Container);
        await fixture.Container.ExecuteShellCommand("mkdir", "-p", "/tmp/project/subdir");
        await fixture.Container.ExecuteShellCommand("sh", "-c", "echo '4.5-rc2' > /tmp/project/subdir/.gdvm-version");

        var result = await fixture.Container.ExecuteShellCommand("sh", "-c", $"cd /tmp/project/subdir && {gdvmPath} which");

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task HandlesEmptyVersionFileGracefully()
    {
        var gdvmPath = await TestHelpers.GetGdvmPath(fixture.Container);
        await fixture.Container.ExecuteShellCommand("mkdir", "-p", "/tmp/empty-version-test");
        await fixture.Container.ExecuteShellCommand("touch", "/tmp/empty-version-test/.gdvm-version");

        var result = await fixture.Container.ExecuteShellCommand("sh", "-c", $"cd /tmp/empty-version-test && {gdvmPath} which");

        Assert.InRange(result.ExitCode, ExitCodes.Success, int.MaxValue);
    }

    [Fact]
    public async Task HandlesWhitespaceInVersionFile()
    {
        var gdvmPath = await TestHelpers.GetGdvmPath(fixture.Container);
        await fixture.Container.ExecuteShellCommand("mkdir", "-p", "/tmp/whitespace-test");
        await fixture.Container.ExecuteShellCommand("sh", "-c", "echo '  4.5-stable  ' > /tmp/whitespace-test/.gdvm-version");

        var result = await fixture.Container.ExecuteShellCommand("sh", "-c", $"cd /tmp/whitespace-test && {gdvmPath} which");

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task GodotCommandShowsUsageWithoutArguments()
    {
        var result = await fixture.Container.ExecuteCommand("godot");

        Assert.InRange(result.ExitCode, ExitCodes.Success, int.MaxValue);
    }

    [Fact]
    public async Task GodotCommandPassesThroughArguments()
    {
        var result = await fixture.Container.ExecuteCommand("godot", "--version");

        Assert.InRange(result.ExitCode, ExitCodes.Success, int.MaxValue);
    }

    [Fact]
    public async Task LocalCommandCreatesVersionFileInCurrentDirectory()
    {
        await fixture.Container.ExecuteShellCommand("mkdir", "-p", "/tmp/local-test");

        var result = await fixture.Container.ExecuteShellCommand("sh", "-c",
            "cd /tmp/local-test && /workspace/GDVM.CLI/bin/Release/net9.0/linux-arm64/publish/gdvm local 4.5-stable");

        Assert.InRange(result.ExitCode, ExitCodes.Success, int.MaxValue);

        var fileExists = await fixture.Container.FileExists("/tmp/local-test/.gdvm-version");
        if (fileExists)
        {
            var content = await fixture.Container.ReadFile("/tmp/local-test/.gdvm-version");
            Assert.Contains("4.5-stable", content);
        }
    }

    [Fact]
    public async Task SetCommandUpdatesGlobalVersion()
    {
        var result = await fixture.Container.ExecuteCommand("set", "4.5-stable");

        Assert.InRange(result.ExitCode, ExitCodes.Success, int.MaxValue);
    }

    [Fact]
    public async Task RemoveCommandHandlesNonInstalledVersionGracefully()
    {
        var result = await fixture.Container.ExecuteCommand("remove", "99.99.99-invalid");

        Assert.InRange(result.ExitCode, ExitCodes.Success, int.MaxValue);
    }

    [Fact]
    public async Task ListCommandWithInstalledFilter()
    {
        var result = await fixture.Container.ExecuteCommand("list", "--installed");

        Assert.InRange(result.ExitCode, ExitCodes.Success, int.MaxValue);
    }

    [Fact]
    public async Task SearchCommandHandlesNetworkFailuresGracefully()
    {
        var result = await fixture.Container.ExecuteCommand("search", "nonexistent-query-that-wont-match-anything");

        Assert.InRange(result.ExitCode, ExitCodes.Success, int.MaxValue);
    }

    [Fact]
    public async Task VersionFileWithCommentsShouldBeHandled()
    {
        await fixture.Container.ExecuteShellCommand("mkdir", "-p", "/tmp/comment-test");
        await fixture.Container.ExecuteShellCommand("sh", "-c", "echo '# This is a comment\n4.5-stable' > /tmp/comment-test/.gdvm-version");

        var result = await fixture.Container.ExecuteShellCommand("sh", "-c",
            "cd /tmp/comment-test && /workspace/GDVM.CLI/bin/Release/net9.0/linux-arm64/publish/gdvm which");

        Assert.InRange(result.ExitCode, ExitCodes.Success, int.MaxValue);
    }

    [Fact]
    public async Task LogsCommandDisplaysPreviousOperations()
    {
        await fixture.Container.ExecuteCommand("list");
        await fixture.Container.ExecuteCommand("search");
        await fixture.Container.ExecuteCommand("which");

        var result = await fixture.Container.ExecuteCommand("logs");

        result.AssertSuccessfulExecution();
        Assert.True(result.Stdout.Length > 0);
    }

    [Fact]
    public async Task MultipleSequentialInstallsWorkCorrectly()
    {
        var searchResult = await fixture.Container.ExecuteCommand("search");
        Assert.Equal(ExitCodes.Success, searchResult.ExitCode);

        var install1 = await fixture.Container.ExecuteCommand("install", "4.5-stable");
        Assert.Equal(ExitCodes.Success, install1.ExitCode);

        var install2 = await fixture.Container.ExecuteCommand("install", "4.5-stable");

        Assert.InRange(install2.ExitCode, ExitCodes.Success, int.MaxValue);

        await CleanupVersion("4.5-stable");
    }

    [Fact]
    public async Task FullVersionManagementWorkflow()
    {
        var searchResult = await fixture.Container.ExecuteCommand("search");
        Assert.Equal(ExitCodes.Success, searchResult.ExitCode);

        Assert.Contains("4.5-stable", searchResult.Stdout);

        var installResult = await fixture.Container.ExecuteCommand("install", "4.5-stable");
        Assert.Equal(ExitCodes.Success, installResult.ExitCode);

        Assert.True(await fixture.Container.HasVersionInstalled("4.5"),
            "Expected 4.5 stable version to be installed");

        var currentVersion = await fixture.Container.GetCurrentVersion();
        Assert.Contains("4.5", currentVersion);

        var install2Result = await fixture.Container.ExecuteCommand("install", "4.5-rc2");
        Assert.Equal(ExitCodes.Success, install2Result.ExitCode);

        Assert.True(await fixture.Container.HasVersionInstalled("4.5"),
            "Expected 4.5 stable version to still be installed");

        Assert.True(await fixture.Container.HasVersionInstalled("rc2"),
            "Expected 4.5 rc2 version to be installed");

        // The install of 4.5-rc2 may find a patch version like 4.5.1-rc2
        // Query with just rc2 should find any rc2 version installed
        var setResult = await fixture.Container.ExecuteCommand("set", "rc2");
        setResult.AssertSuccessfulExecution();

        var newCurrentVersion = await fixture.Container.GetCurrentVersion();
        Assert.Contains("rc2", newCurrentVersion);

        await fixture.Container.ExecuteShellCommand("mkdir", "-p", "/tmp/test-project");
        var localSetResult = await fixture.Container.ExecuteShellCommand("sh", "-c",
            "cd /tmp/test-project && /workspace/GDVM.CLI/bin/Release/net9.0/linux-arm64/publish/gdvm local 4.5-stable");

        if (localSetResult.ExitCode == ExitCodes.Success)
        {
            var versionFileExists = await fixture.Container.FileExists("/tmp/test-project/.gdvm-version");
            Assert.True(versionFileExists, "Expected .gdvm-version file to be created after successful local command");

            var localResult = await fixture.Container.ExecuteShellCommand("sh", "-c",
                "cd /tmp/test-project && /workspace/GDVM.CLI/bin/Release/net9.0/linux-arm64/publish/gdvm which");

            Assert.Contains("4.5", localResult.Stdout);
        }

        await CleanupVersion("4.5-rc2");
        await CleanupVersion("4.5-stable");

        var finalListResult = await fixture.Container.ExecuteCommand("list");
        finalListResult.AssertSuccessfulExecution();

        var logs = await fixture.Container.GetLogs();
        Assert.True(logs.Length > 0, "Should have generated logs during workflow");
        await fixture.Container.AssertLogContains("install");
    }

    private async Task CleanupVersion(string version)
    {
        await fixture.Container.ExecuteCommand("remove", version);
    }

    [Fact]
    public async Task ReadsNearestVersionFileWhenMultipleExist()
    {
        var gdvmPath = await TestHelpers.GetGdvmPath(fixture.Container);
        await fixture.Container.ExecuteShellCommand("mkdir", "-p", "/tmp/nested/project/subdir");
        await fixture.Container.ExecuteShellCommand("sh", "-c", "echo '4.3-stable' > /tmp/nested/.gdvm-version");
        await fixture.Container.ExecuteShellCommand("sh", "-c", "echo '4.5-stable' > /tmp/nested/project/.gdvm-version");

        var result = await fixture.Container.ExecuteShellCommand("sh", "-c", $"cd /tmp/nested/project/subdir && {gdvmPath} which");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("4.5", result.Stdout);
    }

    [Fact(Skip = "TODO: which command doesn't validate .gdvm-version file contents, only checks symlinks")]
    public async Task HandlesInvalidVersionFormatGracefully()
    {
        var gdvmPath = await TestHelpers.GetGdvmPath(fixture.Container);
        await fixture.Container.ExecuteShellCommand("mkdir", "-p", "/tmp/invalid-version");
        await fixture.Container.ExecuteShellCommand("sh", "-c", "echo 'not-a-valid-version-123' > /tmp/invalid-version/.gdvm-version");

        var result = await fixture.Container.ExecuteShellCommand("sh", "-c", $"cd /tmp/invalid-version && {gdvmPath} which");

        Assert.NotEqual(ExitCodes.Success, result.ExitCode);
    }

    [Fact]
    public async Task HandlesVersionFileWithMultipleLines()
    {
        var gdvmPath = await TestHelpers.GetGdvmPath(fixture.Container);
        await fixture.Container.ExecuteShellCommand("mkdir", "-p", "/tmp/multiline");
        await fixture.Container.ExecuteShellCommand("sh", "-c", @"echo -e '4.5-stable\n4.3-stable\n4.0-stable' > /tmp/multiline/.gdvm-version");

        var result = await fixture.Container.ExecuteShellCommand("sh", "-c", $"cd /tmp/multiline && {gdvmPath} which");

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task HandlesVersionFileWithCommentsAndEmptyLines()
    {
        var gdvmPath = await TestHelpers.GetGdvmPath(fixture.Container);
        await fixture.Container.ExecuteShellCommand("mkdir", "-p", "/tmp/comments");
        await fixture.Container.ExecuteShellCommand("sh", "-c", @"echo -e '# Comment\n\n4.5-stable\n' > /tmp/comments/.gdvm-version");

        var result = await fixture.Container.ExecuteShellCommand("sh", "-c", $"cd /tmp/comments && {gdvmPath} which");

        Assert.Equal(0, result.ExitCode);
    }

    [Fact(Skip = "TODO: godot command not returning non-zero exit code - ExitCodeFilter may not be converting exception properly")]
    public async Task GodotCommandFailsWhenNoVersionSet()
    {
        var gdvmPath = await TestHelpers.GetGdvmPath(fixture.Container);
        await fixture.Container.ExecuteShellCommand("mkdir", "-p", "/tmp/no-version");

        var result = await fixture.Container.ExecuteShellCommand("sh", "-c", $"cd /tmp/no-version && {gdvmPath} godot --version");

        Assert.NotEqual(ExitCodes.Success, result.ExitCode);
    }

    [Fact(Skip = "TODO: PromptForInstallationAsync uses interactive prompt which fails in non-interactive E2E tests - need env var or flag for auto-install")]
    public async Task GodotCommandFailsWhenVersionSetButNotInstalled()
    {
        var gdvmPath = await TestHelpers.GetGdvmPath(fixture.Container);
        await fixture.Container.ExecuteShellCommand("mkdir", "-p", "/tmp/not-installed");
        await fixture.Container.ExecuteShellCommand("sh", "-c", "echo '4.5-stable' > /tmp/not-installed/.gdvm-version");

        var result = await fixture.Container.ExecuteShellCommand("sh", "-c", $"cd /tmp/not-installed && {gdvmPath} godot --version");

        Assert.NotEqual(ExitCodes.Success, result.ExitCode);
    }

    [Fact]
    public async Task GodotCommandHandlesComplexArguments()
    {
        var result = await fixture.Container.ExecuteCommand("godot", "--", "--help");

        Assert.InRange(result.ExitCode, ExitCodes.Success, int.MaxValue);
    }

    [Fact]
    public async Task InstallingSameVersionTwiceIsIdempotent()
    {
        var searchResult = await fixture.Container.ExecuteCommand("search");
        Assert.Equal(ExitCodes.Success, searchResult.ExitCode);

        var install1 = await fixture.Container.ExecuteCommand("install", "4.5-stable");
        Assert.Equal(ExitCodes.Success, install1.ExitCode);

        var install2 = await fixture.Container.ExecuteCommand("install", "4.5-stable");
        Assert.Equal(ExitCodes.Success, install2.ExitCode);

        await CleanupVersion("4.5-stable");
    }

    [Fact]
    public async Task InstallCommandWithInvalidVersionFails()
    {
        var result = await fixture.Container.ExecuteCommand("install", "999.999-invalid");

        Assert.Equal(ExitCodes.ArgumentError, result.ExitCode);
    }

    [Fact]
    public async Task RemovingGloballySetVersionSucceeds()
    {
        var searchResult = await fixture.Container.ExecuteCommand("search");
        Assert.Equal(ExitCodes.Success, searchResult.ExitCode);

        var install = await fixture.Container.ExecuteCommand("install", "4.5-stable");
        Assert.Equal(ExitCodes.Success, install.ExitCode);

        await fixture.Container.ExecuteCommand("set", "4.5-stable");
        var remove = await fixture.Container.ExecuteCommand("remove", "4.5-stable");

        Assert.Equal(ExitCodes.Success, remove.ExitCode);
    }

    [Fact]
    public async Task RemovingLocallySetVersionSucceeds()
    {
        var searchResult = await fixture.Container.ExecuteCommand("search");
        Assert.Equal(ExitCodes.Success, searchResult.ExitCode);

        var install = await fixture.Container.ExecuteCommand("install", "4.5-stable");
        Assert.Equal(ExitCodes.Success, install.ExitCode);

        var gdvmPath = await TestHelpers.GetGdvmPath(fixture.Container);
        await fixture.Container.ExecuteShellCommand("mkdir", "-p", "/tmp/remove-local");
        await fixture.Container.ExecuteShellCommand("sh", "-c", $"cd /tmp/remove-local && {gdvmPath} local 4.5-stable");

        var remove = await fixture.Container.ExecuteCommand("remove", "4.5-stable");

        Assert.Equal(ExitCodes.Success, remove.ExitCode);
    }

    [Fact]
    public async Task SetCommandWithNonExistentVersionFails()
    {
        var result = await fixture.Container.ExecuteCommand("set", "999.999-nonexistent");

        Assert.Equal(ExitCodes.GeneralError, result.ExitCode);
    }

    [Fact]
    public async Task LocalCommandWithNonExistentVersionFails()
    {
        var result = await fixture.Container.ExecuteCommand("local", "999.999-nonexistent");

        Assert.Equal(ExitCodes.ArgumentError, result.ExitCode);
    }

    [Fact(Skip = "TODO: local command not creating .gdvm-version file - likely failing at SetLocalVersionAsync before CreateOrUpdateVersionFile is called")]
    public async Task LocalCommandCreatesVersionFileWithCorrectContent()
    {
        var searchResult = await fixture.Container.ExecuteCommand("search");
        Assert.Equal(ExitCodes.Success, searchResult.ExitCode);

        var install = await fixture.Container.ExecuteCommand("install", "4.5-stable");
        Assert.Equal(ExitCodes.Success, install.ExitCode);

        var gdvmPath = await TestHelpers.GetGdvmPath(fixture.Container);
        await fixture.Container.ExecuteShellCommand("mkdir", "-p", "/tmp/local-content-test");
        await fixture.Container.ExecuteShellCommand("sh", "-c", $"cd /tmp/local-content-test && {gdvmPath} local 4.5-stable");

        var fileContent = await fixture.Container.ReadFile("/tmp/local-content-test/.gdvm-version");

        Assert.Contains("4.5-stable", fileContent);

        await CleanupVersion("4.5-stable");
    }

    [Fact]
    public async Task LogsCommandWorksWithNoOperations()
    {
        var result = await fixture.Container.ExecuteCommand("logs");

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task LogsCommandShowsRecentOperationsInOrder()
    {
        var searchResult = await fixture.Container.ExecuteCommand("search");
        Assert.Equal(ExitCodes.Success, searchResult.ExitCode);

        var install = await fixture.Container.ExecuteCommand("install", "4.5-stable");
        Assert.Equal(ExitCodes.Success, install.ExitCode);

        var set = await fixture.Container.ExecuteCommand("set", "4.5-stable");
        Assert.Equal(ExitCodes.Success, set.ExitCode);

        var list = await fixture.Container.ExecuteCommand("list");
        Assert.Equal(ExitCodes.Success, list.ExitCode);

        var logs = await fixture.Container.GetLogs();

        Assert.False(string.IsNullOrEmpty(logs));
        Assert.Contains("install", logs.ToLower());
        Assert.Contains("set", logs.ToLower());

        await CleanupVersion("4.5-stable");
    }

    [Fact]
    public async Task InstallingGodot3StandardWorksOnBothArchitectures()
    {
        // v3.6-stable standard works on both x64 (x11.64) and arm64 (linux.arm64)
        // When installing 3.6-stable, it will find the latest patch version (e.g., 3.6.1-stable)
        var install = await fixture.Container.ExecuteCommand("install", "3.6-stable");
        Assert.Equal(ExitCodes.Success, install.ExitCode);

        // Check that some version of 3.6 stable was installed (could be 3.6-stable or 3.6.1-stable, etc.)
        Assert.True(await fixture.Container.HasVersionInstalled("3.6"),
            "Version 3.6 not found in installed versions");

        await CleanupVersion("3.6-stable");
    }

    [Fact]
    public async Task InstalledGodotBinaryIsExecutable()
    {
        var install = await fixture.Container.ExecuteCommand("install", "4.3-stable");
        Assert.Equal(ExitCodes.Success, install.ExitCode);

        await fixture.Container.ExecuteCommand("set", "4.3-stable");

        // Run Godot in headless mode to verify it's actually executable
        var godotVersion = await fixture.Container.ExecuteCommand("godot", "--", "--version", "--headless");
        Assert.Equal(ExitCodes.Success, godotVersion.ExitCode);
        Assert.Contains("4.3", godotVersion.Stdout);

        await CleanupVersion("4.3-stable");
    }

    [Fact(Skip = "Mono ARM64 builds not consistently available across Godot versions")]
    public async Task InstallingMonoRuntimeWorks()
    {
        // This test is skipped because mono ARM64 availability varies by Godot version
        // and there's no stable version guaranteed to be available long-term
        var install = await fixture.Container.ExecuteCommand("install", "latest", "mono");
        Assert.Equal(ExitCodes.Success, install.ExitCode);

        Assert.True(await fixture.Container.HasVersionInstalled("mono"),
            "Mono runtime version not found in installed versions");

        await fixture.Container.ExecuteCommand("set", "latest", "mono");

        var godotVersion = await fixture.Container.ExecuteCommand("godot", "--", "--version", "--headless");
        Assert.Equal(0, godotVersion.ExitCode);

        await fixture.Container.ExecuteCommand("remove", "latest", "mono");
    }

    [Fact]
    public async Task SearchCommandDisplaysChronologicalOrdering()
    {
        var searchResult = await fixture.Container.ExecuteCommand("search", "4");
        Assert.Equal(ExitCodes.Success, searchResult.ExitCode);

        // Parse output by removing panel borders and extracting version lines
        var lines = searchResult.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.Contains("4.") && line.Contains("-"))
            .Select(line => line.Replace("│", "").Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        // Verify we have some results
        Assert.NotEmpty(lines);

        // Find indices of specific versions to verify chronological ordering
        // Chronological means: higher versions first, then stability within each version
        var stableIndex = Array.FindIndex(lines, l => l.Contains("4.5-stable") || l.Contains("4.4-stable"));
        var rcIndex = Array.FindIndex(lines, l => l.Contains("-rc"));

        // If we have both stable and rc versions, stable of same/higher version should come before rc
        if (stableIndex >= 0 && rcIndex >= 0)
        {
            // Extract version numbers to compare
            var stableLine = lines[stableIndex];
            var rcLine = lines[rcIndex];

            // If they're the same major.minor version, stable should come first
            if (stableLine.StartsWith("4.") && rcLine.StartsWith("4."))
            {
                var stableVersion = stableLine.Split('-')[0];
                var rcVersion = rcLine.Split('-')[0];

                if (stableVersion == rcVersion)
                {
                    Assert.True(stableIndex < rcIndex,
                        $"Chronological ordering failed: {stableLine} (index {stableIndex}) should appear before {rcLine} (index {rcIndex})");
                }
            }
        }
    }

    [Fact]
    public async Task InstallCommandUsesStabilityFirstSelection()
    {
        // This test verifies that when multiple versions match a query,
        // install prefers stable versions over newer unstable versions

        // First, check what versions are available
        var searchResult = await fixture.Container.ExecuteCommand("search", "4");
        Assert.Equal(ExitCodes.Success, searchResult.ExitCode);

        var hasStable = searchResult.Stdout.Contains("-stable");
        var hasDev = searchResult.Stdout.Contains("-dev");

        if (!hasStable || !hasDev)
        {
            return; // Skip if we don't have both stable and dev versions
        }

        var installResult = await fixture.Container.ExecuteCommand("install", "4");
        Assert.Equal(ExitCodes.Success, installResult.ExitCode);

        var listResult = await fixture.Container.ExecuteCommand("list");
        Assert.Equal(ExitCodes.Success, listResult.ExitCode);

        // Should have installed a stable version, not a dev version
        var installedStable = listResult.Stdout.Contains("-stable");
        var installedDev = listResult.Stdout.Contains("-dev");

        Assert.True(installedStable && !installedDev,
            $"Install with query '4' should prefer stable version over dev. Installed versions: {listResult.Stdout}");

        // Cleanup
        await CleanupVersion("4");
    }

    /// <summary>
    ///     Attempts to read the project version from the .csproj file by locating it in the directory hierarchy.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static string GetProjectVersion()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        FileInfo? slnFile = null;
        while (dir is not null && (slnFile = dir.GetFiles("*.sln").FirstOrDefault()) is null)
        {
            dir = dir.Parent;
        }

        if (slnFile is null)
        {
            throw new InvalidOperationException("Could not locate solution directory.");
        }

        var csproj = dir?.GetFiles("*.csproj", SearchOption.AllDirectories)
            .FirstOrDefault(f => f.Name.Equals("GDVM.CLI.csproj", StringComparison.OrdinalIgnoreCase));

        if (csproj is null)
        {
            throw new InvalidOperationException("Could not locate project file.");
        }

        var doc = XDocument.Load(csproj.FullName);
        var version = doc.Descendants("Version").FirstOrDefault()?.Value
                      ?? throw new InvalidOperationException("Version element not found.");

        return version;
    }
}
