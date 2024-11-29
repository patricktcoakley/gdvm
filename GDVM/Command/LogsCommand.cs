using GDVM.Environment;
using GDVM.Error;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Globalization;
using System.Text;
using ZLogger;

namespace GDVM.Command;

public sealed class LogsCommand(ILogger<LogsCommand> logger)
{
    private static readonly string[] LogLevels = ["DEFAULT", "DEBUG", "INFORMATION", "WARNING", "ERROR", "CRITICAL"];
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";


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
            if (!Path.Exists(Paths.LogPath))
            {
                throw new FileNotFoundException($"Path to logs {Paths.LogPath} doesn't exist.");
            }

            if (!string.IsNullOrEmpty(level) && !LogLevels.Any(x => x.StartsWith(level, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentOutOfRangeException(
                    $"{level} is not valid. Accepted values include: default, debug, info, warning, error, or critical");
            }


            await using var stream = new FileStream(
                Paths.LogPath,
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
                    AnsiConsole.WriteLine(logEntry.ToString());
                }
            }
        }
        catch (TaskCanceledException)
        {
            logger.ZLogError($"User cancelled reading the logs.");
            AnsiConsole.MarkupLine(Messages.UserCancelled("reading logs"));

            throw;
        }
        catch (Exception e)
        {
            logger.ZLogError($"Error reading the logs: {e.Message}");
            AnsiConsole.MarkupLine(
                Messages.SomethingWentWrong("when trying to read the logs")
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
