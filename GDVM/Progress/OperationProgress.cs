namespace GDVM.Progress;

/// <summary>
///     Progress model for any operation with typed stages
/// </summary>
/// <typeparam name="TStage">The enum type representing operation stages</typeparam>
/// <param name="Stage">The current stage of the operation</param>
/// <param name="Message">A descriptive message about the current progress</param>
public readonly record struct OperationProgress<TStage>(TStage Stage, string Message) where TStage : Enum;
