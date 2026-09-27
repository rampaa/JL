namespace JL.Core.Dicts.JMnedict;

internal sealed class JmnedictImportEntryBatch
{
    public const int MaxEntriesPerBatch = 256;

    public int EntryCount { get; set; }
    public int[] EntryIds { get; } = new int[MaxEntriesPerBatch];
    public List<string>[] SpellingLists { get; } = new List<string>[MaxEntriesPerBatch];
    public string[][] ReadingArrays { get; } = new string[MaxEntriesPerBatch][];
    public List<Translation>[] TranslationLists { get; } = new List<Translation>[MaxEntriesPerBatch];

    public JmnedictImportEntryBatch()
    {
        for (int i = 0; i < MaxEntriesPerBatch; i++)
        {
            SpellingLists[i] = [];
            TranslationLists[i] = [];
        }
    }
}
