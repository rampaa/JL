namespace JL.Core.Dicts.Options;

public class DictOptions
{
    public DictOptions(NewlineBetweenDefinitionsOption? newlineBetweenDefinitions, ExamplesOption? examples)
    {
        NewlineBetweenDefinitions = newlineBetweenDefinitions;
        Examples = examples;
    }

    public NewlineBetweenDefinitionsOption? NewlineBetweenDefinitions { get; }

    public ExamplesOption? Examples { get; }
}
