namespace JL.Core.Dicts.EPWING.Nazeka;

internal readonly record struct EpwingNazekaPreparedRecordBatch(
    EpwingNazekaPreparedRecord[] Records,
    int RecordCount,
    string[] SearchKeys,
    int SearchKeyCount);
