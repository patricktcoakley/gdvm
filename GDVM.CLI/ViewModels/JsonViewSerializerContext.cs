using GDVM.Command;
using System.Text.Json.Serialization;

namespace GDVM.ViewModels;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ListView))]
[JsonSerializable(typeof(List<ListView>))]
[JsonSerializable(typeof(RemoteReleaseView))]
[JsonSerializable(typeof(List<RemoteReleaseView>))]
[JsonSerializable(typeof(WhichView))]
[JsonSerializable(typeof(LogEntryView))]
[JsonSerializable(typeof(List<LogEntryView>))]
internal partial class JsonViewSerializerContext : JsonSerializerContext;
