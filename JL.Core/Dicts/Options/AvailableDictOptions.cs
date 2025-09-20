using System.Text.Json.Serialization;
using JL.Core.Utilities.Database;

namespace JL.Core.Dicts.Options;

public sealed class NewlineBetweenDefinitionsOption(bool value)
{
    public bool Value { get; } = value;

    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes =
        Enum.GetValues<DictType>().Except([DictType.PitchAccentYomichan, DictType.Kanjidic, DictType.CustomNameDictionary, DictType.ProfileCustomNameDictionary])
            .ToArray();
}

public sealed class NoAllOption(bool value)
{
    public bool Value { get; } = value;

    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes = Enum.GetValues<DictType>()
        .Except([DictType.PitchAccentYomichan]).ToArray();
}

public sealed class PitchAccentMarkerColorOption(string value)
{
    public string Value { get; } = value;
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = [DictType.PitchAccentYomichan];
}

public sealed class WordClassInfoOption(bool value)
{
    public bool Value { get; } = value;
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = [DictType.JMdict];
}

public sealed class DialectInfoOption(bool value)
{
    public bool Value { get; } = value;
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = [DictType.JMdict];
}

public sealed class POrthographyInfoOption(bool value)
{
    public bool Value { get; } = value;
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = [DictType.JMdict];
}

public sealed class POrthographyInfoColorOption(string value)
{
    public string Value { get; } = value;
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = [DictType.JMdict];
}

public sealed class POrthographyInfoFontSizeOption(double value)
{
    public double Value { get; } = value;
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = [DictType.JMdict];
}

public sealed class AOrthographyInfoOption(bool value)
{
    public bool Value { get; } = value;
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = [DictType.JMdict];
}

public sealed class ROrthographyInfoOption(bool value)
{
    public bool Value { get; } = value;
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = [DictType.JMdict];
}

public sealed class WordTypeInfoOption(bool value)
{
    public bool Value { get; } = value;
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = [DictType.JMdict];
}

public sealed class SpellingRestrictionInfoOption(bool value)
{
    public bool Value { get; } = value;
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = [DictType.JMdict];
}

public sealed class ExtraDefinitionInfoOption(bool value)
{
    public bool Value { get; } = value;
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = [DictType.JMdict];
}

public sealed class MiscInfoOption(bool value)
{
    public bool Value { get; } = value;
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = [DictType.JMdict];
}

public sealed class LoanwordEtymologyOption(bool value)
{
    public bool Value { get; } = value;
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = [DictType.JMdict];
}

public sealed class RelatedTermOption(bool value)
{
    public bool Value { get; } = value;
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = [DictType.JMdict];
}

public sealed class AntonymOption(bool value)
{
    public bool Value { get; } = value;
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = [DictType.JMdict];
}

public sealed class UseDBOption(bool value)
{
    public bool Value { get; internal set; } = value;
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = DBUtils.s_dictTypesWithDBSupport;
}

public sealed class ShowPitchAccentWithDottedLinesOption(bool value)
{
    public bool Value { get; } = value;
    [JsonIgnore] public static readonly DictType[] ValidDictTypes = [DictType.PitchAccentYomichan];
}

public sealed class AutoUpdateAfterNDaysOption(int value)
{
    public int Value { get; } = value;

    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes =
        [DictType.JMdict, DictType.JMnedict, DictType.Kanjidic,
        DictType.NonspecificWordYomichan, DictType.NonspecificNameYomichan, DictType.NonspecificKanjiYomichan,
        DictType.NonspecificKanjiWithWordSchemaYomichan, DictType.NonspecificYomichan, DictType.PitchAccentYomichan];
}

public sealed class ShowImagesOption(bool value)
{
    public bool Value { get; set; } = value;

    [JsonIgnore]
    public static readonly DictType[] ValidDictTypes =
        [DictType.NonspecificWordYomichan, DictType.NonspecificNameYomichan, DictType.NonspecificKanjiWithWordSchemaYomichan, DictType.NonspecificYomichan, DictType.NonspecificKanjiYomichan];
}
