using System.Text.Json.Serialization;
using MessagePack;

namespace JL.Core.Dicts.JMdict;

[MessagePackObject(AllowPrivate = true)]
[method: JsonConstructor]
internal readonly record struct LoanwordSource([property: Key(0)] string Language, [property: Key(1)] bool IsPart, [property: Key(2)] bool IsWasei, [property: Key(3)] string? OriginalWord = null);
