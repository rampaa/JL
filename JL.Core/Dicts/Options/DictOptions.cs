namespace JL.Core.Dicts.Options;

public class DictOptions
{
    public DictOptions(NewlineBetweenDefinitionsOption? newlineBetweenDefinitions,
        ExamplesOption? examples,
        RequireKanjiModeOption? requireKanjiMode)
    {
        NewlineBetweenDefinitions = newlineBetweenDefinitions;
        Examples = examples;
        RequireKanjiMode = requireKanjiMode;
    }

    public NewlineBetweenDefinitionsOption? NewlineBetweenDefinitions { get; }

    public ExamplesOption? Examples { get; }

    public RequireKanjiModeOption? RequireKanjiMode { get; }
}
