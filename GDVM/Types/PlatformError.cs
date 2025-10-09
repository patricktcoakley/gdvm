using GDVM.Environment;
using GDVM.Godot;
using System.Runtime.InteropServices;

namespace GDVM.Types;

/// <summary>
///     Represents the possible errors for platform detection.
/// </summary>
public abstract record PlatformError
{
    public record Unsupported(Release Release, OS OS, Architecture Architecture) : PlatformError;
}
