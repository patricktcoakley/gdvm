namespace GDVM.Progress;

/// <summary>
///     Generic progress model for any operation with stages
/// </summary>
/// <typeparam name="TStage">The enum type representing operation stages</typeparam>
public record OperationProgress<TStage>(
    string Message,
    TStage Stage = default!)
    where TStage : Enum;

/// <summary>
///     Stages for installation operations
/// </summary>
public enum InstallationStage
{
    Downloading,
    VerifyingChecksum,
    Extracting,
    SettingDefault
}
