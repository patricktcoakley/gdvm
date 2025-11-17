using Fgvm.Cli.Error;
using Fgvm.Cli.ViewModels;
using Fgvm.Environment;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using ZLogger;

namespace Fgvm.Cli.Command;

public sealed class LogsCommand(
    IPathService pathService,
    IAnsiConsole console,
    ILogger<LogsCommand> logger
)
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
    private static readonly string[] LogLevels = ["DEFAULT", "DEBUG", "INFORMATION", "WARNING", "ERROR", "CRITICAL"];

    /// <summary>
    ///     Outputs the contents of the log file, with optional filters `-l|--level` for level and `-m|--message` for messages.
    /// </summary>
    /// <param name="json">Output logs to JSON.</param>
    /// <param name="level">-l, Level to filter by.</param>
    /// <param name="message">-m, Message text to filter by.</param>
    /// <param name="cancellationToken"></param>
    public async Task Logs(bool json = false, string level = "", string message = "", CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Path.Exists(pathService.LogPath))
            {
                throw new FileNotFoundException(Messages.LogPathNotFound(pathService.LogPath));
            }

            if (!string.IsNullOrEmpty(level) && !LogLevels.Any(x => x.StartsWith(level, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentOutOfRangeException(Messages.LogLevelOutOfRange(level));
            }


            await using var stream = new FileStream(
                pathService.LogPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);

            using var reader = new StreamReader(stream, Encoding.UTF8);

            var entries = new List<LogEntryView>();
            var malformed = new List<string>();

            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                var parsed = TryParseLogLine(line, logger);
                if (parsed is { } entry)
                {
                    if (MatchesFilter(entry, level, message))
                    {
                        entries.Add(entry);
                    }
                }
                else
                {
                    malformed.Add(line);
                }
            }

            console.WriteLine(json ? entries.ToJson() : entries.ToSlog(malformed));
        }
        catch (TaskCanceledException)
        {
            logger.ZLogError($"User cancelled reading the logs.");
            console.MarkupLine(Messages.UserCancelled("reading logs"));

            throw;
        }
        catch (Exception e)
        {
            logger.ZLogError($"Error reading the logs: {e.Message}");
            console.MarkupLine(
                Messages.SomethingWentWrong("when trying to read the logs", pathService)
            );

            throw;
        }
    }

    private static LogEntryView? TryParseLogLine(string line, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        var parts = line.Split('|', 4);
        if (parts.Length < 4)
        {
            logger.ZLogWarning($"Malformed log entry (expected 4 segments): {line}");
            return null;
        }

        if (!DateTime.TryParseExact(parts[0], DateTimeFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var timestamp))
        {
            logger.ZLogWarning($"Could not parse timestamp '{parts[0]}' in log entry: {line}");
            return null;
        }

        var levelPart = parts[1].Trim();
        var messagePart = parts[2].Trim();
        var categoryPart = parts[3].Trim();

        // TODO: eventually remove this but keep for backward compatibility
        // Handle the old log category format
        if (categoryPart.Length >= 2 && categoryPart.StartsWith('(') && categoryPart.EndsWith(')'))
        {
            categoryPart = categoryPart[1..^1].Trim();
        }

        return new LogEntryView(timestamp, levelPart, messagePart, categoryPart);
    }

    private static bool MatchesFilter(LogEntryView entry, string levelFilter, string messageFilter)
    {
        if (!string.IsNullOrEmpty(levelFilter) &&
            !entry.Level.Contains(levelFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(messageFilter) &&
            !entry.Message.Contains(messageFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
}

public readonly record struct LogEntryView(
    [property: JsonPropertyName("timestamp")]
    DateTime Timestamp,
    [property: JsonPropertyName("level")] string Level,
    [property: JsonPropertyName("message")]
    string Message,
    [property: JsonPropertyName("category")]
    string Category) : IJsonView<LogEntryView>
{
    public override string ToString() =>
        $"Timestamp: {Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)}, LogLevel: {Level}, Message: {Message}, Category: {Category}";
}

public static class LogEntryViewExtensions
{
    public static string ToJson(this IReadOnlyList<LogEntryView> entries) =>
        JsonViewExtensions.ToJson(entries);

    public static string ToSlog(this IReadOnlyList<LogEntryView> entries, IReadOnlyList<string> malformed)
    {
        var builder = new StringBuilder();

        if (entries.Count > 0)
        {
            foreach (var entry in entries)
            {
                builder.AppendLine(entry.ToString());
            }
        }

        if (malformed.Count > 0)
        {
            builder.AppendLine($"Skipped {malformed.Count} malformed log entries.");
        }

        if (entries.Count == 0 && malformed.Count == 0)
        {
            builder.AppendLine("No log entries found.");
        }

        return builder.ToString().TrimEnd();
    }
}
