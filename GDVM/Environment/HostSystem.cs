using GDVM.Error;
using GDVM.Types;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.IO.Enumeration;

namespace GDVM.Environment;

public interface IHostSystem
{
    SystemInfo SystemInfo { get; }
    void CreateOrOverwriteSymbolicLink(string symlinkTargetPath);
    void RemoveSymbolicLinks();
    Result<SymlinkInfo, SymlinkError> ResolveCurrentSymlinks();
    IEnumerable<string> ListInstallations();
}

/// <summary>
///     A way to organize host OS file operations.
/// </summary>
public sealed class HostSystem(SystemInfo systemInfo, IPathService pathService, ILogger<HostSystem> logger) : IHostSystem
{
    public SystemInfo SystemInfo { get; } = systemInfo;

    public void CreateOrOverwriteSymbolicLink(string symlinkTargetPath)
    {
        RemoveSymbolicLinks();

        switch (SystemInfo.CurrentOS)
        {
            // We link to both the .app and the Godot command-line binary on macOS.
            case OS.MacOS:
                Directory.CreateSymbolicLink(pathService.MacAppSymlinkPath, symlinkTargetPath);
                File.CreateSymbolicLink(pathService.SymlinkPath, Path.Combine(symlinkTargetPath, "Contents/MacOS/Godot"));
                break;
            case OS.Windows:
                try
                {
                    File.CreateSymbolicLink(pathService.SymlinkPath, symlinkTargetPath);
                }
                // Special case where we can assume that the user has not enabled Developer Mode.
                // We don't necessarily want to fail because symlinks aren't required.
                // TODO: Consider adding an option to ignore/disable symlinks for people who don't care
                catch (Exception e) when (e.Message.StartsWith("A required privilege is not held by the client"))
                {
                    logger.LogWarning(
                        "Windows requires Developer Mode enabled to create symlinks. See: https://learn.microsoft.com/en-us/windows/apps/get-started/enable-your-device-for-development.");
                }

                break;

            case OS.Linux:
                File.CreateSymbolicLink(pathService.SymlinkPath, symlinkTargetPath);
                break;

            // TODO: Untested but possibly works with Linux builds?
            case OS.FreeBSD:
            case OS.Unknown:
            default:
                throw new InvalidEnumArgumentException($"{SystemInfo.CurrentOS} is unsupported at this time.");
        }

        if (SystemInfo.CurrentOS == OS.MacOS && !IsSymbolicLinkValid(pathService.MacAppSymlinkPath))
        {
            throw new InvalidSymlinkException("Symlink was created but appears to be invalid.", pathService.MacAppSymlinkPath);
        }

        if (!IsSymbolicLinkValid(pathService.SymlinkPath))
        {
            throw new InvalidSymlinkException("Symlink was created but appears to be invalid.", pathService.SymlinkPath);
        }
    }

    public void RemoveSymbolicLinks()
    {
        // ATTN: On macOS the behavior of #.Exists can be unreliable due to the differences in how symbolic links are handled on the filesystem.
        // This is possibly related to the fact that the .app is a symlink to a directory and not a file, so it is technically "neither."
        // Therefore, we need to check if it has the ReparsePoint attribute to see if it "truly" exists.
        var macAppSymlinkFileInfo = new FileInfo(pathService.MacAppSymlinkPath);
        if ((macAppSymlinkFileInfo.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
        {
            macAppSymlinkFileInfo.Delete();
        }

        if (File.Exists(pathService.SymlinkPath))
        {
            File.Delete(pathService.SymlinkPath);
        }
    }

    public Result<SymlinkInfo, SymlinkError> ResolveCurrentSymlinks()
    {
        var file = new FileInfo(pathService.SymlinkPath);
        if (file.LinkTarget is null)
        {
            logger.LogInformation("Ran `which` without version set");
            return new Result<SymlinkInfo, SymlinkError>.Failure(
                new SymlinkError.NoVersionSet());
        }

        if (!IsSymbolicLinkValid(pathService.SymlinkPath))
        {
            return new Result<SymlinkInfo, SymlinkError>.Failure(
                new SymlinkError.InvalidSymlink(pathService.SymlinkPath, file.LinkTarget));
        }

        logger.LogInformation("{SymlinkPath} is currently set to: {LinkTarget}", pathService.SymlinkPath, file.LinkTarget);

        // Only macOS has two symlinks
        string? macAppSymlinkPath = null;
        if (SystemInfo.CurrentOS == OS.MacOS)
        {
            var appFile = new FileInfo(pathService.MacAppSymlinkPath);
            if (appFile.LinkTarget is not null)
            {
                if (IsSymbolicLinkValid(pathService.MacAppSymlinkPath))
                {
                    logger.LogInformation("{MacAppSymlinkPath} is currently set to: {LinkTarget}", pathService.MacAppSymlinkPath, appFile.LinkTarget);
                    macAppSymlinkPath = appFile.LinkTarget;
                }
                else
                {
                    logger.LogWarning("Mac App symlink {MacAppSymlinkPath} exists but is invalid", pathService.MacAppSymlinkPath);
                }
            }
            else
            {
                logger.LogWarning("Mac App symlink {MacAppSymlinkPath} is not set", pathService.MacAppSymlinkPath);
            }
        }

        return new Result<SymlinkInfo, SymlinkError>.Success(
            new SymlinkInfo(file.LinkTarget, macAppSymlinkPath));
    }

    /// <summary>
    ///     Lists the local installations naively by just seeing what directories exist. Possibly will use some kind of ledger
    ///     in the future.
    /// </summary>
    /// <returns>List of local installations</returns>
    public IEnumerable<string> ListInstallations()
    {
        var installed = new FileSystemEnumerable<string>(
            pathService.RootPath,
            (ref FileSystemEntry entry) => entry.FileName.ToString())
        {
            ShouldIncludePredicate = (ref FileSystemEntry entry) =>
                entry is { IsDirectory: true, FileName: not "bin", IsHidden: false }
        };

        return installed
            .OrderDescending()
            .ThenBy(x => x.EndsWith("standard"));
    }

    public void DisplaySymbolicLinks()
    {
        var file = new FileInfo(pathService.SymlinkPath);
        if (file.LinkTarget is null)
        {
            logger.LogInformation("Ran `which` without version set");
            throw new InvalidOperationException("No Godot version is set.");
        }

        if (!IsSymbolicLinkValid(pathService.SymlinkPath))
        {
            throw new InvalidSymlinkException("Symlink was created but appears to be invalid.", file.LinkTarget);
        }

        logger.LogInformation("{SymlinkPath} is currently set to: {LinkTarget}", pathService.SymlinkPath, file.LinkTarget);

        // Only macOS has two symlinks
        if (SystemInfo.CurrentOS != OS.MacOS)
        {
            return;
        }

        file = new FileInfo(pathService.MacAppSymlinkPath);
        if (file.LinkTarget is null)
        {
            throw new FileNotFoundException($"{pathService.MacAppSymlinkPath} not set.");
        }

        if (!IsSymbolicLinkValid(pathService.MacAppSymlinkPath))
        {
            throw new InvalidSymlinkException("Symlink was created but appears to be invalid.", file.LinkTarget);
        }

        logger.LogInformation("{MacAppSymlinkPath} is currently set to: {LinkTarget}", pathService.MacAppSymlinkPath, file.LinkTarget);
    }

    private static bool IsSymbolicLinkValid(string symlinkTargetPath)
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
