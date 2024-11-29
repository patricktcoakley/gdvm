namespace GDVM.Error;

// TODO: Adjust exit code based on this
// An easy way to pattern match on errors so we don't necessarily fail if a symlink isn't created
public class InvalidSymlinkException(string message, string symlinkPath) : IOException(message)
{
    public string SymlinkPath { get; } = symlinkPath;
}
