using DotNet.Testcontainers.Containers;
using GDVM.Error;

namespace GDVM.Tests.EndToEnd;

public static class TestHelpers
{
    public static string GetGdvmPath(string rid)
    {
        return $"/workspace/GDVM.CLI/bin/Debug/net9.0/{rid}/publish/gdvm";
    }

    public static async Task<ExecResult> ExecuteCommand(this TestContainerFixture fixture, params string[] args)
    {
        var gdvmPath = GetGdvmPath(fixture.Rid);
        var command = new List<string> { gdvmPath };
        command.AddRange(args);

        return await fixture.Container.ExecAsync(command.ToArray());
    }

    public static async Task<ExecResult> ExecuteCommandInDirectory(this TestContainerFixture fixture, string workingDirectory, params string[] args)
    {
        var gdvmPath = GetGdvmPath(fixture.Rid);

        // Build the command with properly escaped arguments
        var escapedArgs = args.Select(arg => $"\"{arg}\"");
        var commandString = $"cd {workingDirectory} && {gdvmPath} {string.Join(" ", escapedArgs)}";

        return await fixture.Container.ExecAsync(["sh", "-c", commandString]);
    }

    public static async Task<ExecResult> ExecuteShellCommand(this TestContainerFixture fixture, string command, params string[] args)
    {
        var fullCommand = new List<string> { command };
        fullCommand.AddRange(args);

        return await fixture.Container.ExecAsync(fullCommand.ToArray());
    }

    public static async Task<bool> DirectoryExists(this TestContainerFixture fixture, string path)
    {
        var result = await fixture.Container.ExecAsync(["test", "-d", path]);
        return result.ExitCode == ExitCodes.Success;
    }

    public static async Task<bool> FileExists(this TestContainerFixture fixture, string path)
    {
        var result = await fixture.Container.ExecAsync(["test", "-f", path]);
        return result.ExitCode == ExitCodes.Success;
    }

    public static async Task<string> ReadFile(this TestContainerFixture fixture, string path)
    {
        var result = await fixture.Container.ExecAsync(["cat", path]);
        if (result.ExitCode != ExitCodes.Success)
        {
            throw new InvalidOperationException($"Failed to read file {path}: {result.Stderr}");
        }

        return result.Stdout;
    }

    public static async Task<string[]> ListDirectory(this TestContainerFixture fixture, string path)
    {
        var result = await fixture.Container.ExecAsync(["ls", "-1", path]);
        if (result.ExitCode != ExitCodes.Success)
        {
            throw new InvalidOperationException($"Failed to list directory {path}: {result.Stderr}");
        }

        return result.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    public static void AssertSuccessfulExecution(this ExecResult result, string? expectedOutput = null)
    {
        Assert.Equal(ExitCodes.Success, result.ExitCode);

        if (expectedOutput != null)
        {
            Assert.Contains(expectedOutput, result.Stdout, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static void AssertFailedExecution(this ExecResult result, string? expectedError = null)
    {
        Assert.NotEqual(ExitCodes.Success, result.ExitCode);

        if (expectedError != null)
        {
            Assert.Contains(expectedError, result.Stderr, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static async Task<string> GetLogs(this TestContainerFixture fixture)
    {
        var result = await fixture.ExecuteCommand("logs");
        return result.Stdout;
    }

    public static async Task AssertLogContains(this TestContainerFixture fixture, string expectedText)
    {
        var logs = await fixture.GetLogs();
        Assert.Contains(expectedText, logs, StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<bool> HasVersionInstalled(this TestContainerFixture fixture, string version)
    {
        var result = await fixture.ExecuteCommand("list");
        return result.ExitCode == ExitCodes.Success && result.Stdout.Contains(version, StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<string> GetCurrentVersion(this TestContainerFixture fixture)
    {
        var result = await fixture.ExecuteCommand("which");
        return result.ExitCode == ExitCodes.Success ? result.Stdout.Trim() : string.Empty;
    }
}
