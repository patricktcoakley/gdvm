using DotNet.Testcontainers.Containers;
using Fgvm.Error;
using System.Text;

namespace Fgvm.Tests.EndToEnd;

public static class TestHelpers
{
    private static readonly HashSet<string> InstalledVersionsCache = new(StringComparer.OrdinalIgnoreCase);

    public static void MarkVersionUninstalled(string version) =>
        InstalledVersionsCache.Remove(version);

    extension(TestContainerFixture fixture)
    {
        public async Task<ExecResult> ExecuteCommand(string[] args) =>
            await fixture.Container.ExecAsync([fixture.FgvmPath, .. args]);

        public async Task EnsureVersionInstalled(string version)
        {
            if (InstalledVersionsCache.Contains(version))
            {
                return;
            }

            if (!await fixture.HasVersionInstalled(version))
            {
                var installResult = await fixture.ExecuteCommand(["install", version]);
                await fixture.AssertSuccessfulExecutionAsync(installResult, "install");
            }

            InstalledVersionsCache.Add(version);
        }
    }

    extension(TestContainerFixture fixture)
    {
        public async Task<ExecResult> ExecuteCommandInDirectory(string workingDirectory, string[] args)
        {
            // Build the command with properly escaped arguments
            var escapedArgs = args.Select(arg => $"\"{arg}\"");
            var commandString = $"cd {workingDirectory} && {fixture.FgvmPath} {string.Join(" ", escapedArgs)}";

            return await fixture.Container.ExecAsync(["sh", "-c", commandString]);
        }

        public async Task<ExecResult> ExecuteShellCommand(string command, string[] args) =>
            await fixture.Container.ExecAsync([command, .. args]);

        public async Task<bool> DirectoryExists(string path)
        {
            var result = await fixture.Container.ExecAsync(["test", "-d", path]);
            return result.ExitCode == ExitCodes.Success;
        }

        public async Task<bool> FileExists(string path)
        {
            var result = await fixture.Container.ExecAsync(["test", "-f", path]);
            return result.ExitCode == ExitCodes.Success;
        }

        public async Task<string> ReadFile(string path)
        {
            var result = await fixture.Container.ExecAsync(["cat", path]);
            if (result.ExitCode != ExitCodes.Success)
            {
                throw new InvalidOperationException($"Failed to read file {path}: {result.Stderr}");
            }

            return result.Stdout;
        }

        public async Task<string[]> ListDirectory(string path)
        {
            var result = await fixture.Container.ExecAsync(["ls", "-1", path]);
            if (result.ExitCode != ExitCodes.Success)
            {
                throw new InvalidOperationException($"Failed to list directory {path}: {result.Stderr}");
            }

            return result.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        }
    }

    extension(ExecResult result)
    {
        public void AssertSuccessfulExecution(string? expectedOutput = null, string[]? additionalContext = null)
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

                if (additionalContext != null)
                {
                    foreach (var context in additionalContext)
                    {
                        builder.AppendLine("Additional Context:");
                        builder.AppendLine(context);
                    }
                }

                Assert.Fail(builder.ToString());
            }

            Assert.Equal(ExitCodes.Success, result.ExitCode);
        }

        public void AssertFailedExecution(string? expectedError = null)
        {
            Assert.NotEqual(ExitCodes.Success, result.ExitCode);

            if (expectedError != null)
            {
                Assert.Contains(expectedError, result.Stderr, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    extension(TestContainerFixture fixture)
    {
        public async Task<string> GetLogs()
        {
            var result = await fixture.ExecuteCommand(["logs"]);
            return result.Stdout;
        }

        public async Task AssertSuccessfulExecutionAsync(ExecResult result, string? expectedOutput = null)
        {
            if (result.ExitCode != ExitCodes.Success)
            {
                var logs = await fixture.GetLogs();
                result.AssertSuccessfulExecution(expectedOutput, [logs]);
            }
            else
            {
                result.AssertSuccessfulExecution(expectedOutput);
            }
        }

        public async Task AssertLogContains(string expectedText)
        {
            var logs = await fixture.GetLogs();
            Assert.Contains(expectedText, logs, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<bool> HasVersionInstalled(string version)
        {
            var result = await fixture.ExecuteCommand(["list"]);
            return result.ExitCode == ExitCodes.Success && result.Stdout.Contains(version, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<string> GetCurrentVersion()
        {
            var result = await fixture.ExecuteCommand(["which"]);
            return result.ExitCode == ExitCodes.Success ? result.Stdout.Trim() : string.Empty;
        }
    }
}
