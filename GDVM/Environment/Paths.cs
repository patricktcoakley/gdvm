namespace GDVM.Environment;

public static class Paths
{
    public static readonly string RootPath =
        Path.GetFullPath("gdvm", System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile));

    public static readonly string ReleasesPath = Path.Combine(RootPath, ".releases");
    public static readonly string BinPath = Path.Combine(RootPath, "bin");
    public static readonly string SymlinkPath = Path.Combine(BinPath, "godot");
    public static readonly string MacAppSymlinkPath = Path.Combine(BinPath, "Godot.app");
    public static readonly string LogPath = Path.Combine(RootPath, ".log");
}
