using GDVM.Error;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.ComponentModel;
using System.IO.Enumeration;
using ZLogger;

namespace GDVM.Environment;

public interface IHostSystem
{
    SystemInfo SystemInfo { get; }
    void CreateOrOverwriteSymbolicLink(string symlinkTargetPath);
    void RemoveSymbolicLinks();
    void DisplaySymbolicLinks();
    IEnumerable<string> ListInstallations();
}

public class HostSystem(SystemInfo systemInfo, ILogger<HostSystem> logger) : IHostSystem
{
    public SystemInfo SystemInfo { get; } = systemInfo;

    public void CreateOrOverwriteSymbolicLink(string symlinkTargetPath)
    {
        RemoveSymbolicLinks();

        switch (SystemInfo.CurrentOS)
        {
            // We link to both the .app and the Godot command-line binary on macOS.
            case OS.MacOS:
                Directory.CreateSymbolicLink(Paths.MacAppSymlinkPath, symlinkTargetPath);
                File.CreateSymbolicLink(Paths.SymlinkPath, Path.Combine(symlinkTargetPath, "Contents/MacOS/Godot"));
                break;
            case OS.Windows:
                try
                {
                    File.CreateSymbolicLink(Paths.SymlinkPath, symlinkTargetPath);
                }
                // Special case where we can assume that the user has not enabled Developer Mode.
                // We don't necessarily want to fail because symlinks aren't required.
                // TODO: Consider adding an option to ignore/disable symlinks for people who don't care
                catch (Exception e) when (e.Message.StartsWith("A required privilege is not held by the client"))
                {
                    AnsiConsole.MarkupLine(
                        "[red] Windows requires Developer Mode enabled to create symlinks. See: https://learn.microsoft.com/en-us/windows/apps/get-started/enable-your-device-for-development. [/]");
                }

                break;

            case OS.Linux:
                File.CreateSymbolicLink(Paths.SymlinkPath, symlinkTargetPath);
                break;

            // TODO: Untested but possibly works with Linux builds?
            case OS.FreeBSD:
                throw new InvalidEnumArgumentException("FreeBSD is unsupported at this time.");
        }

        if (SystemInfo.CurrentOS == OS.MacOS && !IsSymbolicLinkValid(Paths.MacAppSymlinkPath))
        {
            throw new InvalidSymlinkException("Symlink was created but appears to be invalid.", Paths.MacAppSymlinkPath);
        }

        if (!IsSymbolicLinkValid(Paths.SymlinkPath))
        {
            throw new InvalidSymlinkException("Symlink was created but appears to be invalid.", Paths.SymlinkPath);
        }
    }

    public void RemoveSymbolicLinks()
    {
        // ATTN: On macOS the behavior of #.Exists can be unreliable due to the differences in how symbolic links are handled on the filesystem.
        // This is possibly related to the fact that the .app is a symlink to a directory and not a file, so it is technically "neither."
        // Therefore, we need to check if it has the ReparsePoint attribute to see if it "truly" exists.
        var macAppSymlinkFileInfo = new FileInfo(Paths.MacAppSymlinkPath);
        if ((macAppSymlinkFileInfo.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
        {
            macAppSymlinkFileInfo.Delete();
        }

        if (File.Exists(Paths.SymlinkPath))
        {
            File.Delete(Paths.SymlinkPath);
        }
    }

    public void DisplaySymbolicLinks()
    {
        var file = new FileInfo(Paths.SymlinkPath);
        if (file.LinkTarget is null)
        {
            logger.ZLogInformation($"Ran `which` without version set");
            AnsiConsole.MarkupLine("[red] No Godot version is set. [/]");
            return;
        }

        if (!IsSymbolicLinkValid(Paths.SymlinkPath))
        {
            throw new InvalidSymlinkException("Symlink was created but appears to be invalid.", file.LinkTarget);
        }

        AnsiConsole.MarkupLine($"[green]{Paths.SymlinkPath} is currently set to:[/] [blue]{file.LinkTarget}[/]");

        // Only macOS has two symlinks
        if (SystemInfo.CurrentOS != OS.MacOS)
        {
            return;
        }

        file = new FileInfo(Paths.MacAppSymlinkPath);
        if (file.LinkTarget is null)
        {
            throw new FileNotFoundException($"{Paths.MacAppSymlinkPath} not set.");
        }

        if (!IsSymbolicLinkValid(Paths.MacAppSymlinkPath))
        {
            throw new InvalidSymlinkException("Symlink was created but appears to be invalid.", file.LinkTarget);
        }

        AnsiConsole.MarkupLine($"[green]{Paths.MacAppSymlinkPath} is currently set to:[/] [blue]{file.LinkTarget}[/]");
    }

    /// <summary>
    ///     Lists the local installations naively by just seeing what directories exist. Possibly will use some kind of ledger
    ///     in the future.
    /// </summary>
    /// <returns>List of local installations</returns>
    public IEnumerable<string> ListInstallations()
    {
        var installed = new FileSystemEnumerable<string>(
            Paths.RootPath,
            (ref FileSystemEntry entry) => entry.FileName.ToString())
        {
            ShouldIncludePredicate = (ref FileSystemEntry entry) =>
                entry is { IsDirectory: true, FileName: not "bin", IsHidden: false }
        };

        return installed
            .OrderDescending()
            .ThenBy(x => x.EndsWith("standard"));
    }

    public bool IsSymbolicLinkValid(string symlinkTargetPath)
    {
        var symlinkFileInfo = new FileInfo(symlinkTargetPath);

        // check if considered a symlink
        if ((symlinkFileInfo.Attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
        {
            return false;
        }

        var linkTarget = symlinkFileInfo.ResolveLinkTarget(true);

        return linkTarget is not null &&
               (File.Exists(linkTarget.FullName) || Directory.Exists(linkTarget.FullName));
    }
}
