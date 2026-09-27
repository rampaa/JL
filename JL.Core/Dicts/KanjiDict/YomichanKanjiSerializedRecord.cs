namespace JL.Core.Dicts.KanjiDict;

internal readonly record struct YomichanKanjiSerializedRecord(
    string Kanji,
    byte[]? OnReadings,
    byte[]? KunReadings,
    byte[]? Definitions,
    byte[]? Stats);
