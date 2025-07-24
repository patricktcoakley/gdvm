using GDVM.Environment;
using GDVM.Error;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Globalization;
using System.Text;
using ZLogger;

namespace GDVM.Command;

public sealed class LogsCommand(IPathService pathService, IAnsiConsole console, ILogger<LogsCommand> logger)
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
    private static readonly string[] LogLevels = ["DEFAULT", "DEBUG", "INFORMATION", "WARNING", "ERROR", "CRITICAL"];


    /// <summary>
    ///     Outputs the contents of the log file, with optional filters `-l|--level` for level and `-m|--message` for messages.
    /// </summary>
    /// <param name="level">-l, Level to filter by.</param>
    /// <param name="message">-m, Message text to filter by.</param>
    /// <param name="cancellationToken"></param>
    public async Task Logs(string level = "", string message = "", CancellationToken cancellationToken = default)
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


            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                var parts = line.Split('|');
                if (parts.Length < 4)
                {
                    continue;
                }

                if (!DateTime.TryParseExact(parts[0], DateTimeFormat,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var timestamp))
                {
                    logger.ZLogWarning($"Could not parse timestamp: {parts[0]}");
                    continue;
                }

                var logEntry = new LogEntry
                {
                    Timestamp = timestamp,
                    LogLevel = parts[1],
                    Message = parts[2],
                    Category = parts[3]
                };

                if (string.IsNullOrEmpty(level) && string.IsNullOrEmpty(message) ||
                    logEntry.LogLevel.Contains(level, StringComparison.OrdinalIgnoreCase) &&
                    logEntry.Message.Contains(message, StringComparison.OrdinalIgnoreCase))
                {
                    console.WriteLine(logEntry.ToString());
                }
            }
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
}

public class LogEntry
{
    public required DateTime Timestamp { get; init; }
    public required string LogLevel { get; init; }
    public required string Message { get; init; }
    public required string Category { get; init; }

    public override string ToString() =>
        $"{nameof(Timestamp)}: {Timestamp}, {nameof(LogLevel)}: {LogLevel}, {nameof(Message)}: {Message}, {nameof(Category)}: {Category}";
}
