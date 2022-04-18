using System.Text.Json.Serialization;

namespace JL.Core.Dicts.Options;

public class ExamplesOption
{
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = { DictType.Kenkyuusha };

    public ExamplesOptionValue Value { get; init; }
}
