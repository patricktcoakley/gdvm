using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace GDVM.Tests.EndToEnd;

public class TestContainerFixture : IAsyncLifetime
{
    private IContainer? _container;
    public IContainer Container => _container ?? throw new InvalidOperationException("Container not initialized");

    public async Task InitializeAsync()
    {
        // Find the solution root directory by looking for the .sln file
        var currentDir = Directory.GetCurrentDirectory();
        var solutionDir = currentDir;
        while (!string.IsNullOrEmpty(solutionDir) && !File.Exists(Path.Combine(solutionDir, "GDVM.sln")))
        {
            solutionDir = Directory.GetParent(solutionDir)?.FullName;
        }

        if (string.IsNullOrEmpty(solutionDir))
        {
            throw new InvalidOperationException("Could not find GDVM.sln to determine solution root");
        }

        _container = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/devcontainers/dotnet:1-9.0-bookworm")
            .WithWorkingDirectory("/workspace")
            .WithBindMount(solutionDir, "/workspace")
            .WithCommand("tail", "-f", "/dev/null") // Keep container running
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("dotnet", "--version"))
            .Build();

        await _container.StartAsync();

        // Publish the GDVM CLI for Linux natively in the container with AOT (as intended for Release builds)
        var publishResult = await _container.ExecAsync(["dotnet", "publish", "/workspace/GDVM.CLI/GDVM.CLI.csproj", "-c", "Release"]);
        if (publishResult.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Failed to publish GDVM. Exit code: {publishResult.ExitCode}\nStdout: {publishResult.Stdout}\nStderr: {publishResult.Stderr}");
        }
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }
}
