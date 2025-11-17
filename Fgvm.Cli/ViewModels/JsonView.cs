using System.Text.Json;

namespace Fgvm.Cli.ViewModels;

/// <summary>
///     Centralizes JSON serializer options for CLI view models.
/// </summary>
internal static class JsonView
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
}

/// <summary>
///     Contract implemented by CLI view models that can serialize themselves using the shared options.
/// </summary>
internal interface IJsonView<TSelf> where TSelf : struct, IJsonView<TSelf>
{
    static JsonSerializerOptions JsonOptions => JsonView.Options;
}

internal static class JsonViewExtensions
{
    public static string ToJson<TView>(this TView value)
        where TView : struct, IJsonView<TView> =>
        JsonSerializer.Serialize(value, typeof(TView), JsonViewSerializerContext.Default);

    public static string ToJson<TView>(this IReadOnlyList<TView> values)
        where TView : struct, IJsonView<TView> =>
        JsonSerializer.Serialize(values, values.GetType(), JsonViewSerializerContext.Default);
}
