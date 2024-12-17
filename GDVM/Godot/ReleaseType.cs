namespace GDVM.Godot;

public sealed record ReleaseType : IComparable<ReleaseType>
{
    // In order
    public static readonly string[] Prefixes = ["stable", "rc", "beta", "alpha", "dev"];

    private ReleaseType(string value, int? version = null)
    {
        Value = value;
        Version = version;
    }

    public string Value { get; }
    public int? Version { get; }

    public int CompareTo(ReleaseType? other)
    {
        if (ReferenceEquals(this, other))
        {
            return 0;
        }

        if (other is null)
        {
            return 1;
        }

        return (Value, other.Value) switch
        {
            ("stable", "stable") => 0,
            ("stable", _) => -1,
            (_, "stable") => 1,
            ("rc", "rc") => Nullable.Compare(Version, other.Version),
            ("rc", _) => -1,
            (_, "rc") => 1,
            ("beta", "beta") => Nullable.Compare(Version, other.Version),
            ("beta", _) => -1,
            (_, "beta") => 1,
            ("alpha", "alpha") => Nullable.Compare(Version, other.Version),
            ("alpha", _) => -1,
            (_, "alpha") => 1,
            ("dev", "dev") => Nullable.Compare(Version, other.Version),
            ("dev", _) => -1,
            (_, "dev") => 1,
            _ => throw new InvalidOperationException("Unknown release type")
        };
    }

    public static ReleaseType Stable() => new("stable");
    public static ReleaseType Rc(int version) => new("rc", version);
    public static ReleaseType Beta(int version) => new("beta", version);
    public static ReleaseType Alpha(int version) => new("alpha", version);
    public static ReleaseType Dev(int version) => new("dev", version);

    internal static ReleaseType? TryParse(string input)
    {
        var split = input.Split('-');

        var possiblePrefix = split.FirstOrDefault(x => Prefixes
            .Any(p => x.StartsWith(p, StringComparison.OrdinalIgnoreCase)));

        if (possiblePrefix == null)
        {
            return null;
        }

        if (possiblePrefix.Equals("stable", StringComparison.CurrentCultureIgnoreCase))
        {
            return Stable();
        }

        // Find the first possible version number
        var possibleVersion = possiblePrefix[possiblePrefix.TakeWhile(char.IsLetter).Count()..];

        // Reassign up to possible version
        possiblePrefix = possiblePrefix[..^possibleVersion.Length].ToLower();

        if (string.IsNullOrEmpty(possibleVersion) || !int.TryParse(possibleVersion, out var version) || version < 1)
        {
            return null;
        }

        return possiblePrefix switch
        {
            "rc" => Rc(version),
            "beta" => Beta(version),
            "alpha" => Alpha(version),
            "dev" => Dev(version),
            _ => null
        };
    }

    public override string ToString() => Version.HasValue ? $"{Value}{Version}" : Value;

    public static implicit operator string(ReleaseType type) => type.ToString();
}
