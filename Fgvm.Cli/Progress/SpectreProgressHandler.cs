using Fgvm.Progress;
using Spectre.Console;

namespace Fgvm.Cli.Progress;

/// <summary>
///     Spectre.Console-specific progress handler for CLI operations
/// </summary>
/// <typeparam name="TStage">The enum type representing operation stages</typeparam>
public class SpectreProgressHandler<TStage>(IAnsiConsole console) : IProgressHandler<TStage>
    where TStage : Enum
{
    /// <summary>
    ///     Executes an operation with progress tracking
    /// </summary>
    /// <param name="operation">The operation to perform with progress tracking</param>
    /// <returns>The result of the operation</returns>
    public async Task<T> TrackProgressAsync<T>(Func<IProgress<OperationProgress<TStage>>, Task<T>> operation)
    {
        return await console.Status()
            .StartAsync("Starting operation...", async ctx =>
            {
                var progress = new Progress<OperationProgress<TStage>>(progressUpdate =>
                {
                    ctx.Status = progressUpdate.Message;
                    ctx.Refresh();
                });

                return await operation(progress);
            });
    }
}
