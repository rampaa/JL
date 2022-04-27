using System.Text.Json.Serialization;

namespace JL.Core.Dicts.Options
{
    public enum ExamplesOptionValue
    {
        None,
        One,
        All
    }

    public readonly struct ExamplesOption
    {
        [JsonIgnore] public static readonly DictType[] ValidDictTypes = { DictType.Kenkyuusha };

        public ExamplesOptionValue Value { get; init; }
    }

    public readonly struct NewlineBetweenDefinitionsOption
    {
        [JsonIgnore]
        public static readonly DictType[] ValidDictTypes =
            Enum.GetValues<DictType>().Except(new[] { DictType.Kanjium, DictType.Kanjidic, DictType.CustomNameDictionary })
                .ToArray();
        public bool Value { get; init; }
    }

    public readonly struct RequireKanjiModeOption
    {
        [JsonIgnore] public static readonly DictType[] ValidDictTypes = { DictType.Kanjidic };

        public bool Value { get; init; }
    }
}
