namespace Fgvm.Progress;

/// <summary>
///     Interface for progress handling that can be implemented by different UI frameworks
/// </summary>
/// <typeparam name="TStage">The enum type representing operation stages</typeparam>
public interface IProgressHandler<TStage> where TStage : Enum
{
    /// <summary>
    ///     Executes an operation with progress tracking
    /// </summary>
    /// <param name="operation">The operation to perform with progress tracking</param>
    /// <returns>The result of the operation</returns>
    Task<T> TrackProgressAsync<T>(Func<IProgress<OperationProgress<TStage>>, Task<T>> operation);
}
