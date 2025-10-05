using DotNet.Testcontainers.Containers;

namespace GDVM.Tests.EndToEnd;

public static class TestHelpers
{
    private static string? _cachedGdvmPath;

    public static async Task<string> GetGdvmPath(IContainer container)
    {
        if (_cachedGdvmPath != null)
            return _cachedGdvmPath;

        // Find the gdvm binary - it will be in Debug build output with platform-specific RID
        var findResult = await container.ExecAsync(["find", "/workspace/GDVM.CLI/bin/Debug/net9.0", "-name", "gdvm", "-type", "f"]);
        if (findResult.ExitCode != 0 || string.IsNullOrWhiteSpace(findResult.Stdout))
        {
            throw new InvalidOperationException("Could not find gdvm binary in container");
        }

        _cachedGdvmPath = findResult.Stdout.Trim().Split('\n')[0];
        return _cachedGdvmPath;

    }

    public static async Task<ExecResult> ExecuteCommand(this IContainer container, params string[] args)
    {
        var gdvmPath = await GetGdvmPath(container);
        var command = new List<string> { gdvmPath };
        command.AddRange(args);

        return await container.ExecAsync(command.ToArray());
    }

    public static async Task<ExecResult> ExecuteShellCommand(this IContainer container, string command, params string[] args)
    {
        var fullCommand = new List<string> { command };
        fullCommand.AddRange(args);

        return await container.ExecAsync(fullCommand.ToArray());
    }

    public static async Task<bool> DirectoryExists(this IContainer container, string path)
    {
        var result = await container.ExecAsync(["test", "-d", path]);
        return result.ExitCode == 0;
    }

    public static async Task<bool> FileExists(this IContainer container, string path)
    {
        var result = await container.ExecAsync(["test", "-f", path]);
        return result.ExitCode == 0;
    }

    public static async Task<string> ReadFile(this IContainer container, string path)
    {
        var result = await container.ExecAsync(["cat", path]);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to read file {path}: {result.Stderr}");
        }

        return result.Stdout;
    }

    public static async Task<string[]> ListDirectory(this IContainer container, string path)
    {
        var result = await container.ExecAsync(["ls", "-1", path]);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to list directory {path}: {result.Stderr}");
        }

        return result.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    public static void AssertSuccessfulExecution(this ExecResult result, string? expectedOutput = null)
    {
        Assert.Equal(0, result.ExitCode);

        if (expectedOutput != null)
        {
            Assert.Contains(expectedOutput, result.Stdout, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static void AssertFailedExecution(this ExecResult result, string? expectedError = null)
    {
        Assert.NotEqual(0, result.ExitCode);

        if (expectedError != null)
        {
            Assert.Contains(expectedError, result.Stderr, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static async Task<string> GetLogs(this IContainer container)
    {
        var result = await container.ExecuteCommand("logs");
        return result.Stdout;
    }

    public static async Task<bool> LogContains(this IContainer container, string expectedText)
    {
        var logs = await container.GetLogs();
        return logs.Contains(expectedText, StringComparison.OrdinalIgnoreCase);
    }

    public static async Task AssertLogContains(this IContainer container, string expectedText)
    {
        var logs = await container.GetLogs();
        Assert.Contains(expectedText, logs, StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<bool> HasVersionInstalled(this IContainer container, string version)
    {
        var result = await container.ExecuteCommand("list");
        return result.ExitCode == 0 && result.Stdout.Contains(version, StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<string> GetCurrentVersion(this IContainer container)
    {
        var result = await container.ExecuteCommand("which");
        return result.ExitCode == 0 ? result.Stdout.Trim() : string.Empty;
    }
}
