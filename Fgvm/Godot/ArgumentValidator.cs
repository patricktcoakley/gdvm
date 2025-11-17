namespace Fgvm.Godot;

public static class ArgumentValidator
{
    public static List<string> GetInvalidArguments(string[] query) =>
        query.Where(arg => !IsValidArgument(arg)).ToList();

    private static bool IsValidArgument(string arg) =>
        IsRuntimeOrKeyword(arg) ||
        IsReleaseType(arg) ||
        IsVersionNumber(arg);

    private static bool IsRuntimeOrKeyword(string arg) =>
        arg is "mono" or "standard" or "latest";

    private static bool IsReleaseType(string arg) =>
        ReleaseType.Prefixes.Any(prefix => arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    private static bool IsVersionNumber(string arg) =>
        !string.IsNullOrEmpty(arg) &&
        char.IsDigit(arg[0]) &&
        arg.All(c => char.IsDigit(c) || c is '.' or '-' || char.IsLetter(c));
}
