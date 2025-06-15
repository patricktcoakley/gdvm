using ConsoleAppFramework;
using GDVM.Error;
using Spectre.Console;

namespace GDVM.Filter;

internal sealed class ExitCodeFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        var exitCode = ExitCodes.Success;
        try
        {
            await Next.InvokeAsync(context, cancellationToken);
        }
        catch (InvalidSymlinkException)
        {
            exitCode = ExitCodes.SymlinkError;
        }
        catch (TaskCanceledException)
        {
            exitCode = ExitCodes.Cancelled;
        }
        catch (ConfigurationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Configuration error: {ex.Message}[/]");
            exitCode = ExitCodes.ConfigurationError;
        }
        catch (Exception e) when (e is ArgumentOutOfRangeException or ArgumentNullException or ArgumentException or ArgumentParseFailedException)
        {
            if (e is ArgumentParseFailedException)
            {
                AnsiConsole.MarkupLine($"[red]{e.Message}.[/]");
            }

            exitCode = ExitCodes.ArgumentError;
        }
        catch (Exception)
        {
            exitCode = ExitCodes.GeneralError;
        }
        finally
        {
            System.Environment.Exit(exitCode);
        }
    }
}
