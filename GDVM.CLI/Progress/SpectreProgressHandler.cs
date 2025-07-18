using Spectre.Console;

namespace GDVM.Progress;

/// <summary>
///     Spectre.Console-specific progress handler for CLI operations
/// </summary>
/// <typeparam name="TStage">The enum type representing operation stages</typeparam>
public class SpectreProgressHandler<TStage>(IAnsiConsole console) : IProgressHandler<TStage>
    where TStage : Enum
{
    /// <summary>
    ///     Creates a progress reporter and automatically starts a progress context
    /// </summary>
    /// <param name="operation">The operation to perform with progress tracking</param>
    /// <returns>The result of the operation</returns>
    public async Task<T> TrackProgressAsync<T>(Func<IProgress<OperationProgress<TStage>>, Task<T>> operation)
    {
        // Simple approach: just handle non-download stages
        var progress = new Progress<OperationProgress<TStage>>(progressUpdate =>
        {
            // Only handle InstallationStage for now, could be extended for other stage types
            if (progressUpdate.Stage is not InstallationStage stage)
            {
                return;
            }

            var message = stage switch
            {
                InstallationStage.Downloading => null, // Handled progress indicator
                InstallationStage.VerifyingChecksum => "Calculating checksum :input_numbers:",
                InstallationStage.Extracting => "Extracting files :file_folder:",
                InstallationStage.SettingDefault => $"{progressUpdate.Message} :gear:",
                _ => null // Handle any unknown stages gracefully
            };

            if (message != null)
            {
                console.MarkupLine(message);
            }
        });

        return await operation(progress);
    }
}
