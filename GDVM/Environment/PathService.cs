namespace GDVM.Environment;

/// <summary>
///     Service for providing path-related functionality.
/// </summary>
public interface IPathService
{
    /// <summary>
    ///     The root path for GDVM installations and configuration.
    /// </summary>
    string RootPath { get; }

    /// <summary>
    ///     Path to the GDVM configuration file.
    /// </summary>
    string ConfigPath { get; }

    /// <summary>
    ///     Path to the releases cache directory.
    /// </summary>
    string ReleasesPath { get; }

    /// <summary>
    ///     Path to the bin directory.
    /// </summary>
    string BinPath { get; }

    /// <summary>
    ///     Path to the Godot symlink.
    /// </summary>
    string SymlinkPath { get; }

    /// <summary>
    ///     Path to the macOS Godot.app symlink.
    /// </summary>
    string MacAppSymlinkPath { get; }

    /// <summary>
    ///     Path to the log directory.
    /// </summary>
    string LogPath { get; }
}

public sealed class PathService : IPathService
{
    public string RootPath =>
        Path.GetFullPath("gdvm", System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile));
    public string ConfigPath => Path.Combine(RootPath, "gdvm.ini");
    public string ReleasesPath => Path.Combine(RootPath, ".releases");
    public string BinPath => Path.Combine(RootPath, "bin");
    public string SymlinkPath => Path.Combine(BinPath, "godot");
    public string MacAppSymlinkPath => Path.Combine(BinPath, "Godot.app");
    public string LogPath => Path.Combine(RootPath, ".log");
}
