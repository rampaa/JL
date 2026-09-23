namespace JL.Core.Dicts.EPWING.Yomichan;

internal readonly record struct ImportRecordBatch(EpwingYomichanImportRecord[] Records, int Count);
