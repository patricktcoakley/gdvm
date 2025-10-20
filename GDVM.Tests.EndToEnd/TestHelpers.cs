using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotNet.Testcontainers.Containers;
using GDVM.Error;

namespace GDVM.Tests.EndToEnd;

public static class TestHelpers
{
    private static readonly HashSet<string> InstalledVersionsCache = new(StringComparer.OrdinalIgnoreCase);

    public static async Task<ExecResult> ExecuteCommand(this TestContainerFixture fixture, params string[] args)
    {
        var command = new List<string> { fixture.GdvmPath };
        command.AddRange(args);

        return await fixture.Container.ExecAsync(command.ToArray());
    }

    public static async Task EnsureVersionInstalled(this TestContainerFixture fixture, string version)
    {
        if (InstalledVersionsCache.Contains(version))
        {
            return;
        }

        if (!await fixture.HasVersionInstalled(version))
        {
            var installResult = await fixture.ExecuteCommand("install", version);
            await fixture.AssertSuccessfulExecutionAsync(installResult, "install");
        }

        InstalledVersionsCache.Add(version);
    }

    public static void MarkVersionUninstalled(string version) =>
        InstalledVersionsCache.Remove(version);

    public static async Task<ExecResult> ExecuteCommandInDirectory(this TestContainerFixture fixture, string workingDirectory, params string[] args)
    {
        // Build the command with properly escaped arguments
        var escapedArgs = args.Select(arg => $"\"{arg}\"");
        var commandString = $"cd {workingDirectory} && {fixture.GdvmPath} {string.Join(" ", escapedArgs)}";

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

    public static void AssertSuccessfulExecution(this ExecResult result, string? expectedOutput = null, params string[] additionalContext)
    {
        if (result.ExitCode != ExitCodes.Success)
        {
            var builder = new StringBuilder();
            if (!string.IsNullOrEmpty(expectedOutput))
            {
                builder.AppendLine($"Context: {expectedOutput}");
            }

            builder.AppendLine($"Exit Code: {result.ExitCode}");
            builder.AppendLine("STDOUT:");
            builder.AppendLine(result.Stdout);
            builder.AppendLine("STDERR:");
            builder.AppendLine(result.Stderr);

            foreach (var context in additionalContext)
            {
                builder.AppendLine("Additional Context:");
                builder.AppendLine(context);
            }

            Assert.Fail(builder.ToString());
        }

        Assert.Equal(ExitCodes.Success, result.ExitCode);
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

    public static async Task AssertSuccessfulExecutionAsync(this TestContainerFixture fixture, ExecResult result, string? expectedOutput = null)
    {
        if (result.ExitCode != ExitCodes.Success)
        {
            var logs = await fixture.GetLogs();
            result.AssertSuccessfulExecution(expectedOutput, logs);
        }
        else
        {
            result.AssertSuccessfulExecution(expectedOutput);
        }
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
