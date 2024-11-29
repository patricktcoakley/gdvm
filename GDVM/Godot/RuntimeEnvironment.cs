namespace GDVM.Godot;

public enum RuntimeEnvironment
{
    Standard,
    Mono
}

public static class RuntimeEnvironmentExtensions
{
    public static string Name(this RuntimeEnvironment env) => env.ToString().ToLowerInvariant();
}
