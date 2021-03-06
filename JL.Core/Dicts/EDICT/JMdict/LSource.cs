namespace JL.Core.Dicts.EDICT.JMdict;

public readonly struct LSource
{
    public bool IsWasei { get; }
    public bool IsPart { get; }
    public string Language { get; }
    public string? OriginalWord { get; }

    public LSource(string language, bool isPart, bool isWasei, string? originalWord)
    {
        IsWasei = isWasei;
        IsPart = isPart;
        Language = language;
        OriginalWord = originalWord;
    }
}
