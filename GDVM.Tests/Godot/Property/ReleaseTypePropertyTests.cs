using FsCheck;
using FsCheck.Xunit;
using GDVM.Godot;

namespace GDVM.Test.Godot.Property;

public class ReleaseTypePropertyTests
{
    private static Gen<string> ValidPrefixGen =>
        Gen.Elements(ReleaseType.Prefixes);

    private static Gen<int> VersionGen =>
        Gen.Choose(1, 100);

    private static Arbitrary<ReleaseType> ReleaseTypeGen =>
        Gen.OneOf(
                Gen.Constant(ReleaseType.Stable()),
                ValidPrefixGen.Zip(VersionGen).Select(t => t.Item1 switch
                {
                    "rc" => ReleaseType.Rc(t.Item2),
                    "beta" => ReleaseType.Beta(t.Item2),
                    "alpha" => ReleaseType.Alpha(t.Item2),
                    "dev" => ReleaseType.Dev(t.Item2),
                    _ => ReleaseType.Stable()
                }))
            .ToArbitrary();

    [Property]
    public FsCheck.Property ToString_TryParse_Roundtrip() =>
        Prop.ForAll(ReleaseTypeGen, type =>
        {
            var str = type.ToString();
            var parsed = ReleaseType.TryParse(str);
            return parsed != null && parsed == type;
        });

    [Property]
    public FsCheck.Property TypeOrdering_IsTransitive() =>
        Prop.ForAll(ReleaseTypeGen, ReleaseTypeGen, ReleaseTypeGen, (r1, r2, r3) =>
        {
            var lessThanOrEqualTransitive = r1.CompareTo(r2) > 0 || r2.CompareTo(r3) > 0 || r1.CompareTo(r3) <= 0;
            var greaterThanOrEqualTransitive = r1.CompareTo(r2) < 0 || r2.CompareTo(r3) < 0 || r1.CompareTo(r3) >= 0;

            return lessThanOrEqualTransitive && greaterThanOrEqualTransitive;
        });

    [Property]
    public FsCheck.Property Version_OnlyWithNonStable() =>
        Prop.ForAll(ReleaseTypeGen, type =>
            string.Equals(type.Value, "stable", StringComparison.Ordinal) ? !type.Version.HasValue : type.Version.HasValue);

    [Property]
    public FsCheck.Property Parse_CaseInsensitive() =>
        Prop.ForAll(ReleaseTypeGen, type =>
        {
            var upper = type.ToString().ToUpper();
            var lower = type.ToString().ToLower();
            var parsedUpper = ReleaseType.TryParse(upper);
            var parsedLower = ReleaseType.TryParse(lower);
            return parsedLower == parsedUpper;
        });
}
