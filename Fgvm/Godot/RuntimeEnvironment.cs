namespace Fgvm.Godot;

public enum RuntimeEnvironment
{
    Standard,
    Mono
}

public static class RuntimeEnvironmentExtensions
{
    extension(RuntimeEnvironment env)
    {
        public string Name() => env.ToString().ToLowerInvariant();
    }
}
