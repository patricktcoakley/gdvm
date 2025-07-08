namespace GDVM.Services;

public interface IGodotArgumentService
{
    /// <summary>
    ///     Determines if certain Godot command line arguments require attached mode for proper output.
    ///     Uses Godot's official argument categories for systematic detection.
    /// </summary>
    /// <param name="argumentString">The command line arguments passed to Godot</param>
    /// <returns>True if attached mode should be forced</returns>
    bool ShouldForceAttachedMode(string? argumentString);

    /// <summary>
    ///     Checks if the arguments contain general options that require terminal output.
    /// </summary>
    /// <param name="args">The command line arguments (case-insensitive)</param>
    /// <returns>True if general options are present</returns>
    bool HasGeneralOptions(string args);

    /// <summary>
    ///     Checks if the arguments contain debug options that require terminal output.
    /// </summary>
    /// <param name="args">The command line arguments (case-insensitive)</param>
    /// <returns>True if debug options are present</returns>
    bool HasDebugOptions(string args);

    /// <summary>
    ///     Checks if the arguments contain standalone tools that require terminal output.
    /// </summary>
    /// <param name="args">The command line arguments (case-insensitive)</param>
    /// <returns>True if standalone tools are present</returns>
    bool HasStandaloneTools(string args);
}

public sealed class GodotArgumentService : IGodotArgumentService
{
    public bool ShouldForceAttachedMode(string? argumentString)
    {
        if (string.IsNullOrEmpty(argumentString))
        {
            return false;
        }

        var args = argumentString.ToLowerInvariant();

        // General options - ALL require terminal output
        if (HasGeneralOptions(args))
        {
            return true;
        }

        // Debug options - Most require terminal output
        if (HasDebugOptions(args))
        {
            return true;
        }

        // Standalone tools - ALL require terminal output
        return HasStandaloneTools(args);
    }

    public bool HasGeneralOptions(string args) =>
        args.Contains("--help") || args.Contains("-h") ||
        args.Contains("--version") ||
        args.Contains("--verbose") || args.Contains("-v") ||
        args.Contains("--quiet") || args.Contains("-q") ||
        args.Contains("--headless") ||
        args.Contains("--no-header");

    public bool HasDebugOptions(string args) =>
        args.Contains("--debug") || args.Contains("-d") ||
        args.Contains("--print-fps") ||
        args.Contains("--debug-stringnames") ||
        args.Contains("--gpu-profile") ||
        args.Contains("--profiling") ||
        args.Contains("--benchmark") ||
        args.Contains("--benchmark-file");

    public bool HasStandaloneTools(string args) =>
        args.Contains("--script") || args.Contains("-s") ||
        args.Contains("--check-only") ||
        args.Contains("--import") ||
        args.Contains("--export-release") ||
        args.Contains("--export-debug") ||
        args.Contains("--export-pack") ||
        args.Contains("--export-patch") ||
        args.Contains("--convert-3to4") ||
        args.Contains("--validate-conversion-3to4") ||
        args.Contains("--doctool") ||
        args.Contains("--gdscript-docs") ||
        args.Contains("--gdextension-docs") ||
        args.Contains("--build-solutions") ||
        args.Contains("--dump-gdextension-interface") ||
        args.Contains("--dump-extension-api") ||
        args.Contains("--dump-extension-api-with-docs") ||
        args.Contains("--validate-extension-api") ||
        args.Contains("--install-android-build-template");
}
