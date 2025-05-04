using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using JL.Core.Dicts.CustomNameDict;
using JL.Core.Dicts.CustomWordDict;
using JL.Core.Dicts.EPWING.Nazeka;
using JL.Core.Dicts.EPWING.Yomichan;
using JL.Core.Dicts.JMdict;
using JL.Core.Dicts.JMnedict;
using JL.Core.Dicts.KANJIDIC;
using JL.Core.Dicts.KanjiDict;
using JL.Core.Dicts.PitchAccent;
using JL.Core.Utilities;

namespace JL.Core.Dicts;
public class DictBaseJsonConverter : JsonConverter<DictBase>
{
    public override DictBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement root = doc.RootElement;
        JsonElement typeProp = root.GetProperty("Type");
        DictType dictType = typeProp.GetString()!.GetEnum<DictType>();
        string json = root.GetRawText();
        Type? type = type = dictType switch
        {
            DictType.JMdict => typeof(Dict<JmdictRecord>),
            DictType.JMnedict => typeof(Dict<JmnedictRecord>),
            DictType.Kanjidic => typeof(Dict<KanjidicRecord>),
            DictType.CustomWordDictionary or DictType.ProfileCustomWordDictionary => typeof(Dict<CustomWordRecord>),
            DictType.CustomNameDictionary or DictType.ProfileCustomNameDictionary => typeof(Dict<CustomNameRecord>),
            DictType.NonspecificWordYomichan
                or DictType.NonspecificYomichan
                or DictType.NonspecificKanjiWithWordSchemaYomichan
                or DictType.NonspecificNameYomichan
                => typeof(Dict<EpwingYomichanRecord>),
            DictType.NonspecificKanjiYomichan => typeof(Dict<YomichanKanjiRecord>),
            DictType.PitchAccentYomichan => typeof(Dict<PitchAccentRecord>),
            DictType.NonspecificWordNazeka
                or DictType.NonspecificKanjiNazeka
                or DictType.NonspecificNameNazeka
                or DictType.NonspecificNazeka
                => typeof(Dict<EpwingNazekaRecord>),
            _ => null
        };

        Debug.Assert(type is not null);
        DictBase? result = (DictBase?)JsonSerializer.Deserialize(json, type, options);
        return result;
    }

    public override void Write(Utf8JsonWriter writer, DictBase value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
