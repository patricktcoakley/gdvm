using GDVM.Command;
using GDVM.Environment;
using Microsoft.Extensions.Logging.Abstractions;
using Spectre.Console.Testing;
using System.Text.Json;
using System.Threading;

namespace GDVM.Test.Command;

public sealed class LogsCommandTests : IDisposable
{
    private readonly string _tempRoot;

    public LogsCommandTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "gdvm-logs-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public async Task LogsCommand_WritesHumanReadableOutput()
    {
        var timestamp = DateTime.UtcNow;
        var logLines = new[]
        {
            $"{timestamp:yyyy-MM-dd HH:mm:ss.fff}|Information|Operation completed|GDVM.Test.Component",
            "malformed entry"
        };

        var (command, console) = CreateCommand(logLines);

        await command.Logs(json: false, cancellationToken: CancellationToken.None);

        var output = console.Output;
        Assert.Contains("Timestamp:", output);
        Assert.Contains("LogLevel: Information", output);
        Assert.Contains("GDVM.Test.Component", output);
        Assert.Contains("Skipped 1 malformed log entries.", output);
    }

    [Fact]
    public async Task LogsCommand_WritesJsonOutput()
    {
        var first = DateTime.UtcNow;
        var second = first.AddSeconds(1);
        var logLines = new[]
        {
            $"{first:yyyy-MM-dd HH:mm:ss.fff}|Warning|First message|GDVM.Json.Category",
            $"{second:yyyy-MM-dd HH:mm:ss.fff}|Information|Second message with | pipe|GDVM.Json.Other"
        };

        var (command, console) = CreateCommand(logLines);

        await command.Logs(json: true, cancellationToken: CancellationToken.None);

        var json = console.Output.Trim();
        Assert.False(string.IsNullOrWhiteSpace(json));

        var entries = JsonSerializer.Deserialize<List<JsonLogEntry>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.Level == "Warning" && e.Category == "GDVM.Json.Category");
    }

    [Fact]
    public async Task LogsCommand_AppliesFilters()
    {
        var logLines = new[]
        {
            $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}|Information|Download completed|GDVM.Filter.Component",
            $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}|Warning|Cache miss|GDVM.Filter.Component"
        };

        var (command, console) = CreateCommand(logLines);

        await command.Logs(level: "warn", cancellationToken: CancellationToken.None);

        var output = console.Output;
        Assert.Contains("Cache miss", output);
        Assert.DoesNotContain("Download completed", output);
    }

    private (LogsCommand Command, TestConsole Console) CreateCommand(IEnumerable<string> lines)
    {
        var logPath = Path.Combine(_tempRoot, ".log");
        File.WriteAllLines(logPath, lines);

        var pathService = new TestPathService(logPath);
        var console = new TestConsole();
        var logger = NullLogger<LogsCommand>.Instance;

        var command = new LogsCommand(pathService, console, logger);
        return (command, console);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }
        catch
        {
            // best effort cleanup
        }
    }

    private sealed class TestPathService : IPathService
    {
        public TestPathService(string logPath)
        {
            LogPath = logPath;
            RootPath = Path.GetDirectoryName(logPath)!;
            ConfigPath = Path.Combine(RootPath, "gdvm.ini");
            ReleasesPath = Path.Combine(RootPath, ".releases");
            BinPath = Path.Combine(RootPath, "bin");
            SymlinkPath = Path.Combine(BinPath, "godot");
            MacAppSymlinkPath = Path.Combine(BinPath, "Godot.app");
        }

        public string RootPath { get; }
        public string ConfigPath { get; }
        public string ReleasesPath { get; }
        public string BinPath { get; }
        public string SymlinkPath { get; }
        public string MacAppSymlinkPath { get; }
        public string LogPath { get; }
    }

    private sealed record JsonLogEntry
    {
        public DateTime Timestamp { get; init; }
        public string Level { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
    }
}
