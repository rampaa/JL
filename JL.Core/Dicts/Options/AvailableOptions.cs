using System.Text.Json.Serialization;

namespace JL.Core.Dicts.Options;

public enum ExamplesOptionValue
{
    None,
    One,
    All
}

public readonly record struct ExamplesOption(ExamplesOptionValue Value)
{
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = { DictType.Kenkyuusha };
}

public readonly record struct NewlineBetweenDefinitionsOption(bool Value)
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes =
        Enum.GetValues<DictType>().Except(new DictType[] { DictType.PitchAccentYomichan, DictType.Kanjidic, DictType.CustomNameDictionary })
            .ToArray();
}

public readonly record struct NoAllOption(bool Value)
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = Enum.GetValues<DictType>()
        .Except(new DictType[] { DictType.PitchAccentYomichan }).ToArray();
}

public readonly record struct PitchAccentMarkerColorOption(string Value)
{
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = { DictType.PitchAccentYomichan };
}

public readonly record struct WordClassInfoOption(bool Value)
{
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
}

public readonly record struct DialectInfoOption(bool Value)
{
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
}

public readonly record struct POrthographyInfoOption(bool Value)
{
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
}

public readonly record struct POrthographyInfoColorOption(string Value)
{
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
}

public readonly record struct POrthographyInfoFontSizeOption(double Value)
{
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
}

public readonly record struct AOrthographyInfoOption(bool Value)
{
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
}

public readonly record struct ROrthographyInfoOption(bool Value)
{
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
}

public readonly record struct WordTypeInfoOption(bool Value)
{
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
}

public readonly record struct SpellingRestrictionInfoOption(bool Value)
{
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
}

public readonly record struct ExtraDefinitionInfoOption(bool Value)
{
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
}

public readonly record struct MiscInfoOption(bool Value)
{
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
}

public readonly record struct LoanwordEtymologyOption(bool Value)
{
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
}

public readonly record struct RelatedTermOption(bool Value)
{
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
}

public readonly record struct AntonymOption(bool Value)
{
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
}
