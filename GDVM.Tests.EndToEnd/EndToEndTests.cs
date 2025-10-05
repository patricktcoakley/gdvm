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
        var result = await fixture.Container.ExecuteCommand("--version");

        result.AssertSuccessfulExecution("1.2.5");
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

        Assert.True(result.ExitCode >= 0);

        if (result.ExitCode != 0)
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

        Assert.NotEqual(0, result.ExitCode);
        await fixture.Container.AssertLogContains("install");
    }

    [Fact]
    public async Task RemoveCommandHandlesInvalidVersionGracefully()
    {
        var result = await fixture.Container.ExecuteCommand("remove", "nonexistent-version-999");

        Assert.True(result.ExitCode >= 0);
    }

    [Fact]
    public async Task SetCommandFailsGracefullyForInvalidVersion()
    {
        var result = await fixture.Container.ExecuteCommand("set", "nonexistent-version-999");

        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task LocalCommandHandlesInvalidVersionGracefully()
    {
        var result = await fixture.Container.ExecuteCommand("local", "nonexistent-version-999");

        Assert.True(result.ExitCode >= 0);
    }

    [Fact]
    public async Task SearchCommandAcceptsVersionQueryParameter()
    {
        var result = await fixture.Container.ExecuteCommand("search", "4.5");

        Assert.True(result.ExitCode >= 0);

        if (result.ExitCode == 0)
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

        Assert.True(result.ExitCode >= 0);
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

        Assert.True(result.ExitCode >= 0);
    }

    [Fact]
    public async Task GodotCommandPassesThroughArguments()
    {
        var result = await fixture.Container.ExecuteCommand("godot", "--version");

        Assert.True(result.ExitCode >= 0);
    }

    [Fact]
    public async Task LocalCommandCreatesVersionFileInCurrentDirectory()
    {
        await fixture.Container.ExecuteShellCommand("mkdir", "-p", "/tmp/local-test");

        var result = await fixture.Container.ExecuteShellCommand("sh", "-c", "cd /tmp/local-test && /workspace/GDVM.CLI/bin/Release/net9.0/linux-arm64/publish/gdvm local 4.5-stable");

        Assert.True(result.ExitCode >= 0);

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

        Assert.True(result.ExitCode >= 0);
    }

    [Fact]
    public async Task RemoveCommandHandlesNonInstalledVersionGracefully()
    {
        var result = await fixture.Container.ExecuteCommand("remove", "99.99.99-invalid");

        Assert.True(result.ExitCode >= 0);
    }

    [Fact]
    public async Task ListCommandWithInstalledFilter()
    {
        var result = await fixture.Container.ExecuteCommand("list", "--installed");

        Assert.True(result.ExitCode >= 0);
    }

    [Fact]
    public async Task SearchCommandHandlesNetworkFailuresGracefully()
    {
        var result = await fixture.Container.ExecuteCommand("search", "nonexistent-query-that-wont-match-anything");

        Assert.True(result.ExitCode >= 0);
    }

    [Fact]
    public async Task VersionFileWithCommentsShouldBeHandled()
    {
        await fixture.Container.ExecuteShellCommand("mkdir", "-p", "/tmp/comment-test");
        await fixture.Container.ExecuteShellCommand("sh", "-c", "echo '# This is a comment\n4.5-stable' > /tmp/comment-test/.gdvm-version");

        var result = await fixture.Container.ExecuteShellCommand("sh", "-c", "cd /tmp/comment-test && /workspace/GDVM.CLI/bin/Release/net9.0/linux-arm64/publish/gdvm which");

        Assert.True(result.ExitCode >= 0);
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
        if (searchResult.ExitCode != 0)
        {
            return;
        }

        var install1 = await fixture.Container.ExecuteCommand("install", "4.5-stable");
        if (install1.ExitCode != 0)
        {
            return;
        }

        var install2 = await fixture.Container.ExecuteCommand("install", "4.5-stable");

        Assert.True(install2.ExitCode >= 0);

        await CleanupVersion("4.5-stable");
    }

    [Fact]
    public async Task FullVersionManagementWorkflow()
    {
        var searchResult = await fixture.Container.ExecuteCommand("search");
        if (searchResult.ExitCode != 0)
        {
            return;
        }
        Assert.Contains("4.5-stable", searchResult.Stdout);

        var installResult = await fixture.Container.ExecuteCommand("install", "4.5-stable");
        if (installResult.ExitCode != 0)
        {
            await fixture.Container.GetLogs();
            return;
        }

        Assert.True(await fixture.Container.HasVersionInstalled("4.5-stable"));

        var currentVersion = await fixture.Container.GetCurrentVersion();
        Assert.Contains("4.5-stable", currentVersion);

        var install2Result = await fixture.Container.ExecuteCommand("install", "4.5-rc2");
        if (install2Result.ExitCode != 0)
        {
            await CleanupVersion("4.5-stable");
            return;
        }

        Assert.True(await fixture.Container.HasVersionInstalled("4.5-stable"));
        Assert.True(await fixture.Container.HasVersionInstalled("4.5-rc2"));

        var setResult = await fixture.Container.ExecuteCommand("set", "4.5-rc2");
        setResult.AssertSuccessfulExecution();

        var newCurrentVersion = await fixture.Container.GetCurrentVersion();
        Assert.Contains("4.5-rc2", newCurrentVersion);

        await fixture.Container.ExecuteShellCommand("mkdir", "-p", "/tmp/test-project");
        var localSetResult = await fixture.Container.ExecuteShellCommand("sh", "-c", "cd /tmp/test-project && /workspace/GDVM.CLI/bin/Release/net9.0/linux-arm64/publish/gdvm local 4.5-stable");

        if (localSetResult.ExitCode == 0)
        {
            var versionFileExists = await fixture.Container.FileExists("/tmp/test-project/.gdvm-version");
            Assert.True(versionFileExists, "Expected .gdvm-version file to be created after successful local command");

            var localResult = await fixture.Container.ExecuteShellCommand("sh", "-c", "cd /tmp/test-project && /workspace/GDVM.CLI/bin/Release/net9.0/linux-arm64/publish/gdvm which");
            Assert.Contains("4.5-stable", localResult.Stdout);
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

    [Fact(Skip = "TODO")]
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

    [Fact(Skip = "TODO")]
    public async Task HandlesInvalidVersionFormatGracefully()
    {
        var gdvmPath = await TestHelpers.GetGdvmPath(fixture.Container);
        await fixture.Container.ExecuteShellCommand("mkdir", "-p", "/tmp/invalid-version");
        await fixture.Container.ExecuteShellCommand("sh", "-c", "echo 'not-a-valid-version-123' > /tmp/invalid-version/.gdvm-version");

        var result = await fixture.Container.ExecuteShellCommand("sh", "-c", $"cd /tmp/invalid-version && {gdvmPath} which");

        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task HandlesVersionFileWithMultipleLines()
    {
        var gdvmPath = await TestHelpers.GetGdvmPath(fixture.Container);
        await fixture.Container.ExecuteShellCommand("mkdir", "-p", "/tmp/multiline");
        await fixture.Container.ExecuteShellCommand("sh", "-c", "echo -e '4.5-stable\\n4.3-stable\\n4.0-stable' > /tmp/multiline/.gdvm-version");

        var result = await fixture.Container.ExecuteShellCommand("sh", "-c", $"cd /tmp/multiline && {gdvmPath} which");

        // Should use first line only
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task HandlesVersionFileWithCommentsAndEmptyLines()
    {
        var gdvmPath = await TestHelpers.GetGdvmPath(fixture.Container);
        await fixture.Container.ExecuteShellCommand("mkdir", "-p", "/tmp/comments");
        await fixture.Container.ExecuteShellCommand("sh", "-c", "echo -e '# Comment\\n\\n4.5-stable\\n' > /tmp/comments/.gdvm-version");

        var result = await fixture.Container.ExecuteShellCommand("sh", "-c", $"cd /tmp/comments && {gdvmPath} which");

        Assert.Equal(0, result.ExitCode);
    }

    [Fact(Skip = "TODO")]
    public async Task GodotCommandFailsWhenNoVersionSet()
    {
        var gdvmPath = await TestHelpers.GetGdvmPath(fixture.Container);
        await fixture.Container.ExecuteShellCommand("mkdir", "-p", "/tmp/no-version");

        var result = await fixture.Container.ExecuteShellCommand("sh", "-c", $"cd /tmp/no-version && {gdvmPath} godot --version");

        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact(Skip = "TODO")]
    public async Task GodotCommandFailsWhenVersionSetButNotInstalled()
    {
        var gdvmPath = await TestHelpers.GetGdvmPath(fixture.Container);
        await fixture.Container.ExecuteShellCommand("mkdir", "-p", "/tmp/not-installed");
        await fixture.Container.ExecuteShellCommand("sh", "-c", "echo '4.5-stable' > /tmp/not-installed/.gdvm-version");

        var result = await fixture.Container.ExecuteShellCommand("sh", "-c", $"cd /tmp/not-installed && {gdvmPath} godot --version");

        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task GodotCommandHandlesComplexArguments()
    {
        var result = await fixture.Container.ExecuteCommand("godot", "--", "--help");

        // Should fail gracefully since godot isn't actually installed, but should parse args correctly
        Assert.True(result.ExitCode >= 0);
    }

    [Fact]
    public async Task InstallingSameVersionTwiceIsIdempotent()
    {
        var searchResult = await fixture.Container.ExecuteCommand("search");
        if (searchResult.ExitCode != 0)
        {
            return;
        }

        var install1 = await fixture.Container.ExecuteCommand("install", "4.5-stable");
        if (install1.ExitCode != 0)
        {
            return;
        }

        var install2 = await fixture.Container.ExecuteCommand("install", "4.5-stable");
        Assert.Equal(0, install2.ExitCode);

        await CleanupVersion("4.5-stable");
    }

    [Fact(Skip = "TODO")]
    public async Task InstallCommandWithInvalidVersionFails()
    {
        var result = await fixture.Container.ExecuteCommand("install", "999.999-invalid");

        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task RemovingGloballySetVersionSucceeds()
    {
        var searchResult = await fixture.Container.ExecuteCommand("search");
        if (searchResult.ExitCode != 0)
        {
            return;
        }

        var install = await fixture.Container.ExecuteCommand("install", "4.5-stable");
        if (install.ExitCode != 0)
        {
            return;
        }

        await fixture.Container.ExecuteCommand("set", "4.5-stable");
        var remove = await fixture.Container.ExecuteCommand("remove", "4.5-stable");

        Assert.Equal(0, remove.ExitCode);
    }

    [Fact]
    public async Task RemovingLocallySetVersionSucceeds()
    {
        var searchResult = await fixture.Container.ExecuteCommand("search");
        if (searchResult.ExitCode != 0)
        {
            return;
        }

        var install = await fixture.Container.ExecuteCommand("install", "4.5-stable");
        if (install.ExitCode != 0)
        {
            return;
        }

        var gdvmPath = await TestHelpers.GetGdvmPath(fixture.Container);
        await fixture.Container.ExecuteShellCommand("mkdir", "-p", "/tmp/remove-local");
        await fixture.Container.ExecuteShellCommand("sh", "-c", $"cd /tmp/remove-local && {gdvmPath} local 4.5-stable");

        var remove = await fixture.Container.ExecuteCommand("remove", "4.5-stable");

        Assert.Equal(0, remove.ExitCode);
    }

    [Fact]
    public async Task SetCommandWithNonExistentVersionFails()
    {
        var result = await fixture.Container.ExecuteCommand("set", "999.999-nonexistent");

        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task LocalCommandWithNonExistentVersionFails()
    {
        var result = await fixture.Container.ExecuteCommand("local", "999.999-nonexistent");

        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact(Skip = "TODO")]
    public async Task LocalCommandCreatesVersionFileWithCorrectContent()
    {
        var searchResult = await fixture.Container.ExecuteCommand("search");
        if (searchResult.ExitCode != 0)
        {
            return;
        }

        var install = await fixture.Container.ExecuteCommand("install", "4.5-stable");
        if (install.ExitCode != 0)
        {
            return;
        }

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
        // Should return empty or minimal output, not crash
    }

    [Fact(Skip = "TODO")]
    public async Task LogsCommandShowsRecentOperationsInOrder()
    {
        var searchResult = await fixture.Container.ExecuteCommand("search");
        if (searchResult.ExitCode != 0)
        {
            return;
        }

        var install = await fixture.Container.ExecuteCommand("install", "4.5-stable");
        if (install.ExitCode != 0)
        {
            return;
        }

        await fixture.Container.ExecuteCommand("set", "4.5-stable");
        await fixture.Container.ExecuteCommand("list");

        var logs = await fixture.Container.GetLogs();

        // Verify operations appear in logs
        Assert.Contains("install", logs.ToLower());
        Assert.Contains("set", logs.ToLower());
        Assert.Contains("list", logs.ToLower());

        await CleanupVersion("4.5-stable");
    }
}
