using System.Text.Json.Serialization;

namespace JL.Core.Dicts.Options;

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

public readonly struct NoAllOption
{
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = Enum.GetValues<DictType>()
        .Except(new[] { DictType.Kanjium }).ToArray();

    public bool Value { get; init; }
}

public readonly struct WordClassInfoOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
    public bool Value { get; init; }
}

public readonly struct DialectInfoOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
    public bool Value { get; init; }
}

public readonly struct POrthographyInfoOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
    public bool Value { get; init; }
}

public readonly struct POrthographyInfoColorOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
    public string Value { get; init; }
}

public readonly struct POrthographyInfoFontSizeOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
    public double Value { get; init; }
}

public readonly struct AOrthographyInfoOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
    public bool Value { get; init; }
}
public readonly struct ROrthographyInfoOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
    public bool Value { get; init; }
}

public readonly struct WordTypeInfoOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
    public bool Value { get; init; }
}

public readonly struct SpellingRestrictionInfoOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
    public bool Value { get; init; }
}

public readonly struct ExtraDefinitionInfoOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
    public bool Value { get; init; }
}

public readonly struct MiscInfoOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
    public bool Value { get; init; }
}
