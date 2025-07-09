using System.Text.RegularExpressions;

namespace GDVM.Godot;

/// <summary>
///     Interface for managing Godot project information and version files
/// </summary>
public interface IProjectManager
{
    /// <summary>
    ///     Finds project information including version and runtime environment.
    /// </summary>
    /// <param name="directory">The directory to search in. If null, uses current working directory.</param>
    /// <returns>ProjectInfo if found, null otherwise.</returns>
    ProjectManager.ProjectInfo? FindProjectInfo(string? directory = null);

    /// <summary>
    ///     Finds the project version using the following priority:
    ///     1. `.gdvm-version` file (user override) or
    ///     2. `project.godot` file (automatic detection) and creates a `.gdvm-version` file based on the contents.
    /// </summary>
    /// <param name="directory">The directory to search in. If null, uses current working directory.</param>
    /// <returns>The version string if found, null otherwise.</returns>
    string? FindProjectVersion(string? directory = null);

    /// <summary>
    ///     Finds the path to the project.godot file in the specified directory.
    /// </summary>
    /// <param name="directory">The directory to search in. If null, uses current working directory.</param>
    /// <returns>The full path to project.godot if found, null otherwise.</returns>
    string? FindProjectFilePath(string? directory = null);

    /// <summary>
    ///     Creates or updates a `.gdvm-version` file in the specified directory.
    /// </summary>
    /// <param name="version">The version to write to the file</param>
    /// <param name="directory">The directory to create the file in (null for current directory)</param>
    void CreateVersionFile(string version, string? directory = null);

    /// <summary>
    ///     Finds project info from `.gdvm-version` file
    /// </summary>
    /// <param name="directory">The directory to search in. If null, uses current working directory.</param>
    /// <returns>ProjectInfo if .gdvm-version file found, null otherwise.</returns>
    ProjectManager.ProjectInfo? FindExplicitProjectInfo(string? directory = null);
}

public partial class ProjectManager : IProjectManager
{
    private const string VersionFile = ".gdvm-version";
    private const string ProjectFile = "project.godot";

    /// <summary>
    ///     Finds the project version using the following priority:
    ///     1. `.gdvm-version` file (user override) or
    ///     2. `project.godot` file (automatic detection) and creates a `.gdvm-version` file based on the contents.
    /// </summary>
    /// <param name="directory">The directory to search in. If null, uses current working directory.</param>
    /// <returns>The version string if found, null otherwise.</returns>
    public string? FindProjectVersion(string? directory = null)
    {
        var projectInfo = FindProjectInfo(directory);
        return projectInfo?.Version;
    }

    /// <summary>
    ///     Finds project information including version and .NET status.
    /// </summary>
    /// <param name="directory">The directory to search in. If null, uses current working directory.</param>
    /// <returns>ProjectInfo if found, null otherwise.</returns>
    public ProjectInfo? FindProjectInfo(string? directory = null)
    {
        var targetDir = directory ?? Directory.GetCurrentDirectory();

        // 1. Check for `.gdvm-version` file first (user override)
        var versionFile = Path.Combine(targetDir, VersionFile);
        if (File.Exists(versionFile))
        {
            var content = File.ReadAllText(versionFile).Trim();
            if (!string.IsNullOrEmpty(content))
            {
                // Determine runtime from version string
                var runtime = content.Contains("mono", StringComparison.OrdinalIgnoreCase)
                    ? RuntimeEnvironment.Mono
                    : RuntimeEnvironment.Standard;

                return new ProjectInfo(content, runtime);
            }
        }

        // Check `project.godot` file for automatic detection
        var projectFile = Path.Combine(targetDir, ProjectFile);
        return File.Exists(projectFile) ? ParseProjectGodot(projectFile) : null;
    }

    /// <summary>
    ///     Finds the path to the project.godot file in the specified directory.
    /// </summary>
    /// <param name="directory">The directory to search in. If null, uses current working directory.</param>
    /// <returns>The full path to project.godot if found, null otherwise.</returns>
    public string? FindProjectFilePath(string? directory = null)
    {
        var targetDir = directory ?? Directory.GetCurrentDirectory();
        var projectFile = Path.Combine(targetDir, ProjectFile);
        return File.Exists(projectFile) ? projectFile : null;
    }

    public void CreateVersionFile(string version, string? directory = null)
    {
        var targetDir = directory ?? Directory.GetCurrentDirectory();
        var filePath = Path.Combine(targetDir, VersionFile);
        File.WriteAllText(filePath, version + System.Environment.NewLine);
    }

    /// <summary>
    ///     Finds project info from `.gdvm-version` file
    /// </summary>
    /// <param name="directory">The directory to search in. If null, uses current working directory.</param>
    /// <returns>ProjectInfo if .gdvm-version file found, null otherwise.</returns>
    public ProjectInfo? FindExplicitProjectInfo(string? directory = null)
    {
        var targetDir = directory ?? Directory.GetCurrentDirectory();

        // Only check for `.gdvm-version` file (user override)
        var versionFile = Path.Combine(targetDir, VersionFile);
        if (!File.Exists(versionFile))
        {
            return null;
        }

        var content = File.ReadAllText(versionFile).Trim();
        if (string.IsNullOrEmpty(content))
        {
            return null;
        }

        // Determine runtime from version string
        var runtime = content.Contains("mono", StringComparison.OrdinalIgnoreCase)
            ? RuntimeEnvironment.Mono
            : RuntimeEnvironment.Standard;

        return new ProjectInfo(content, runtime);
    }

    /// <summary>
    ///     Parses a project.godot file to extract version and .NET information.
    /// </summary>
    /// <param name="projectFilePath">Path to the project.godot file.</param>
    /// <returns>ProjectInfo if parsing succeeds, null otherwise.</returns>
    private static ProjectInfo? ParseProjectGodot(string projectFilePath)
    {
        try
        {
            var content = File.ReadAllText(projectFilePath);
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            string? version = null;
            var runtime = RuntimeEnvironment.Standard;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Extract version from config/features
                if (trimmedLine.StartsWith("config/features=PackedStringArray("))
                {
                    version = ExtractVersionFromFeatures(trimmedLine);
                }

                // Check for .NET section
                if (trimmedLine == "[dotnet]")
                {
                    runtime = RuntimeEnvironment.Mono;
                }
            }

            if (version != null)
            {
                return new ProjectInfo(version, runtime);
            }
        }
        catch (Exception)
        {
            return null;
        }

        return null;
    }

    /// <summary>
    ///     Extracts the Godot version from a config/features line.
    /// </summary>
    /// <param name="featuresLine">The line containing config/features=PackedStringArray(...)</param>
    /// <returns>The version string if found, null otherwise.</returns>
    private static string? ExtractVersionFromFeatures(string featuresLine)
    {
        // Example: config/features=PackedStringArray("4.4", "Forward Plus")
        var startIndex = featuresLine.IndexOf('(');
        var endIndex = featuresLine.LastIndexOf(')');

        if (startIndex == -1 || endIndex == -1 || endIndex <= startIndex)
        {
            return null;
        }

        var featuresContent = featuresLine.Substring(startIndex + 1, endIndex - startIndex - 1);

        // Split by comma and look for version-like strings
        var features = featuresContent.Split(',')
            .Select(f => f.Trim().Trim('"'))
            .Where(f => !string.IsNullOrEmpty(f));

        return features.FirstOrDefault(feature => VersionRegex().IsMatch(feature));
    }

    [GeneratedRegex(@"^\d+\.\d+(\.\d+)?$")]
    private static partial Regex VersionRegex();

    public record ProjectInfo(string Version, RuntimeEnvironment Runtime)
    {
        /// <summary>
        ///     Gets the runtime display suffix for the project (e.g., " [.NET]" or empty string)
        /// </summary>
        public string RuntimeDisplaySuffix => Runtime == RuntimeEnvironment.Mono ? " [[.NET]]" : "";

        /// <summary>
        ///     Gets whether this project uses .NET runtime (true for Mono, false for Standard)
        /// </summary>
        public bool IsDotNet => Runtime == RuntimeEnvironment.Mono;
    }
}
