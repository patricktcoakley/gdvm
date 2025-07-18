namespace GDVM.Services;

public interface IGodotArgumentService
{
    /// <summary>
    ///     Determines if certain Godot command line arguments require attached mode for proper output.
    /// </summary>
    /// <param name="argumentString">The command line arguments passed to Godot</param>
    /// <returns>True if attached mode should be forced</returns>
    bool ShouldForceAttachedMode(string? argumentString);
}

public sealed class GodotArgumentService : IGodotArgumentService
{
    public bool ShouldForceAttachedMode(string? argumentString)
    {
        if (string.IsNullOrEmpty(argumentString))
        {
            return false;
        }

        return
            // General options
            argumentString.Contains("--help", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("-h", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("--version", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("--verbose", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("-v", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("--quiet", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("-q", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("--headless", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("--no-header", StringComparison.OrdinalIgnoreCase) ||

            // Debug and profiling options
            argumentString.Contains("--debug", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("-d", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("--print-fps", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("--debug-stringnames", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("--gpu-profile", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("--profiling", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("--benchmark", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("--benchmark-file", StringComparison.OrdinalIgnoreCase) ||

            // Scripting and automation
            argumentString.Contains("--script", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("-s", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("--check-only", StringComparison.OrdinalIgnoreCase) ||

            // Project operations
            argumentString.Contains("--import", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("--export-release", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("--export-debug", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("--export-pack", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("--export-patch", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("--convert-3to4", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("--validate-conversion-3to4", StringComparison.OrdinalIgnoreCase) ||

            // Documentation tools
            argumentString.Contains("--doctool", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("--gdscript-docs", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("--gdextension-docs", StringComparison.OrdinalIgnoreCase) ||

            // Build and development tools
            argumentString.Contains("--build-solutions", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("--dump-gdextension-interface", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("--dump-extension-api", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("--dump-extension-api-with-docs", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("--validate-extension-api", StringComparison.OrdinalIgnoreCase) ||
            argumentString.Contains("--install-android-build-template", StringComparison.OrdinalIgnoreCase);
    }
}
