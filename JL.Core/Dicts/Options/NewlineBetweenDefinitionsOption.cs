using System.Text.Json.Serialization;

namespace JL.Core.Dicts.Options;

public class NewlineBetweenDefinitionsOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes =
        Enum.GetValues<DictType>().Except(new[] { DictType.Kanjium, DictType.Kanjidic }).ToArray();

    public bool Value { get; init; }
}
