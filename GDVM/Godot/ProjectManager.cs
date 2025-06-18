using System.Text.RegularExpressions;

namespace GDVM.Godot;

public partial class ProjectManager
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
    public static string? FindProjectVersion(string? directory = null)
    {
        var projectInfo = FindProjectInfo(directory);
        return projectInfo?.Version;
    }

    /// <summary>
    ///     Finds project information including version and .NET status.
    /// </summary>
    /// <param name="directory">The directory to search in. If null, uses current working directory.</param>
    /// <returns>ProjectInfo if found, null otherwise.</returns>
    public static ProjectInfo? FindProjectInfo(string? directory = null)
    {
        var targetDir = directory ?? Directory.GetCurrentDirectory();

        // 1. Check for `.gdvm-version` file first (user override)
        var versionFile = Path.Combine(targetDir, VersionFile);
        if (File.Exists(versionFile))
        {
            var content = File.ReadAllText(versionFile).Trim();
            if (!string.IsNullOrEmpty(content))
            {
                // Try to determine if it's .NET from the version string
                var isDotNet = content.Contains("mono", StringComparison.OrdinalIgnoreCase);
                return new ProjectInfo(content, isDotNet);
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
    public static string? FindProjectFilePath(string? directory = null)
    {
        var targetDir = directory ?? Directory.GetCurrentDirectory();
        var projectFile = Path.Combine(targetDir, ProjectFile);
        return File.Exists(projectFile) ? projectFile : null;
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
            var isDotNet = false;

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
                    isDotNet = true;
                }
            }

            if (version != null)
            {
                return new ProjectInfo(version, isDotNet);
            }
        }
        catch (Exception)
        {
            // If parsing fails, return null
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

    public static void CreateVersionFile(string version, string? directory = null)
    {
        var targetDir = directory ?? Directory.GetCurrentDirectory();
        var filePath = Path.Combine(targetDir, VersionFile);
        File.WriteAllText(filePath, version + System.Environment.NewLine);
    }

    [GeneratedRegex(@"^\d+\.\d+(\.\d+)?$")]
    private static partial Regex VersionRegex();

    public record ProjectInfo(string Version, bool IsDotNet);
}
