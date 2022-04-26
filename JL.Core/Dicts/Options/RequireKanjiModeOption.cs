using System.Text.Json.Serialization;

namespace JL.Core.Dicts.Options;

public class RequireKanjiModeOption
{
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = { DictType.Kanjidic };

    public bool Value { get; init; }
}
