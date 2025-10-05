using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace GDVM.Tests.EndToEnd;

public class TestContainerFixture : IAsyncLifetime
{
    private IContainer? _container;
    private string? _rid;

    public IContainer Container => _container ?? throw new InvalidOperationException("Container not initialized");
    public string Rid => _rid ?? throw new InvalidOperationException("RID not initialized");

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
            .WithImage("mcr.microsoft.com/dotnet/sdk:9.0")
            .WithWorkingDirectory("/workspace")
            .WithBindMount(solutionDir, "/workspace")
            .WithCommand("tail", "-f", "/dev/null") // Keep container running
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("dotnet", "--version"))
            .Build();

        await _container.StartAsync();

        // Detect container architecture to determine RID
        var archResult = await _container.ExecAsync(["uname", "-m"]);
        if (archResult.ExitCode != 0)
        {
            throw new InvalidOperationException("Failed to detect container architecture");
        }

        var arch = archResult.Stdout.Trim();
        _rid = arch switch
        {
            "x86_64" => "linux-x64",
            "aarch64" => "linux-arm64",
            _ => throw new InvalidOperationException($"Unsupported architecture: {arch}")
        };

        // Publish with Debug config to avoid AOT (which requires native build tools)
        var publishResult = await _container.ExecAsync(["dotnet", "publish", "/workspace/GDVM.CLI/GDVM.CLI.csproj", "-c", "Debug"]);
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
