namespace JL.Core.Dicts.JMdict;

internal sealed class Sense(
    string[] glossArray,
    string[]? posArray,
    string? sInf,
    string[]? stagKArray,
    string[]? stagRArray,
    string[]? fieldArray,
    string[]? miscArray,
    string[]? dialArray,
    string[]? xRefArray,
    string[]? antArray,
    LoanwordSource[]? lSourceArray)
{
    public string[] GlossArray { get; } = glossArray; // English meaning
    public string[]? PosArray { get; } = posArray; // e.g. "noun"
    public string? SInf { get; } = sInf; // e.g. "often derog"
    public string[]? StagKArray { get; } = stagKArray; // Meaning only valid for these kebs.
    public string[]? StagRArray { get; } = stagRArray; // Meaning only valid for these rebs.
    public string[]? FieldArray { get; } = fieldArray; // e.g. "martial arts"
    public string[]? MiscArray { get; } = miscArray; // e.g. "abbr"
    public string[]? DialArray { get; } = dialArray; // e.g. ksb
    public string[]? XRefArray { get; } = xRefArray; // Related terms
    public string[]? AntArray { get; } = antArray; // Antonyms
    public LoanwordSource[]? LSourceArray { get; } = lSourceArray;
}
