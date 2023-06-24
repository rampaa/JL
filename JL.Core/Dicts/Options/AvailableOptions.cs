using System.Text.Json.Serialization;

namespace JL.Core.Dicts.Options;

public enum ExamplesOptionValue
{
    None,
    One,
    All
}

public readonly record struct ExamplesOption
{
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = { DictType.Kenkyuusha };
    public ExamplesOptionValue Value { get; init; }

    public ExamplesOption(ExamplesOptionValue value)
    {
        Value = value;
    }
}

public readonly record struct NewlineBetweenDefinitionsOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes =
        Enum.GetValues<DictType>().Except(new[] { DictType.PitchAccentYomichan, DictType.Kanjidic, DictType.CustomNameDictionary })
            .ToArray();
    public bool Value { get; init; }

    public NewlineBetweenDefinitionsOption(bool value)
    {
        Value = value;
    }
}

public readonly record struct NoAllOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = Enum.GetValues<DictType>()
        .Except(new[] { DictType.PitchAccentYomichan }).ToArray();
    public bool Value { get; init; }

    public NoAllOption(bool value)
    {
        Value = value;
    }
}

public readonly record struct WordClassInfoOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
    public bool Value { get; init; }
    public WordClassInfoOption(bool value)
    {
        Value = value;
    }
}

public readonly record struct DialectInfoOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
    public bool Value { get; init; }

    public DialectInfoOption(bool value)
    {
        Value = value;
    }
}

public readonly record struct POrthographyInfoOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
    public bool Value { get; init; }

    public POrthographyInfoOption(bool value)
    {
        Value = value;
    }
}

public readonly record struct POrthographyInfoColorOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
    public string Value { get; init; }

    public POrthographyInfoColorOption(string value)
    {
        Value = value;
    }
}

public readonly record struct POrthographyInfoFontSizeOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
    public double Value { get; init; }

    public POrthographyInfoFontSizeOption(double value)
    {
        Value = value;
    }
}

public readonly record struct AOrthographyInfoOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
    public bool Value { get; init; }

    public AOrthographyInfoOption(bool value)
    {
        Value = value;
    }
}
public readonly record struct ROrthographyInfoOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
    public bool Value { get; init; }

    public ROrthographyInfoOption(bool value)
    {
        Value = value;
    }
}

public readonly record struct WordTypeInfoOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
    public bool Value { get; init; }

    public WordTypeInfoOption(bool value)
    {
        Value = value;
    }
}

public readonly record struct SpellingRestrictionInfoOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
    public bool Value { get; init; }

    public SpellingRestrictionInfoOption(bool value)
    {
        Value = value;
    }
}

public readonly record struct ExtraDefinitionInfoOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
    public bool Value { get; init; }

    public ExtraDefinitionInfoOption(bool value)
    {
        Value = value;
    }
}

public readonly record struct MiscInfoOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
    public bool Value { get; init; }

    public MiscInfoOption(bool value)
    {
        Value = value;
    }
}

public readonly record struct RelatedTermOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
    public bool Value { get; init; }

    public RelatedTermOption(bool value)
    {
        Value = value;
    }
}

public readonly record struct AntonymOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
    public bool Value { get; init; }

    public AntonymOption(bool value)
    {
        Value = value;
    }
}

public readonly record struct LoanwordEtymologyOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = { DictType.JMdict };
    public bool Value { get; init; }

    public LoanwordEtymologyOption(bool value)
    {
        Value = value;
    }
}

public readonly record struct PitchAccentMarkerColorOption
{
    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = { DictType.PitchAccentYomichan };
    public string Value { get; init; }

    public PitchAccentMarkerColorOption(string value)
    {
        Value = value;
    }
}
