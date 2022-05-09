namespace JL.Core.Dicts.Options;

public class DictOptions
{
    public NewlineBetweenDefinitionsOption? NewlineBetweenDefinitions { get; }
    public ExamplesOption? Examples { get; }
    public NoAllOption? NoAll { get; }
    public WordClassInfoOption? WordClassInfo { get; }
    public DialectInfoOption? DialectInfo { get; }
    public POrthographyInfoOption? POrthographyInfo { get; }
    public POrthographyInfoColorOption? POrthographyInfoColor { get; }
    public POrthographyInfoFontSizeOption? POrthographyInfoFontSize { get; }
    public AOrthographyInfoOption? AOrthographyInfo { get; }
    public ROrthographyInfoOption? ROrthographyInfo { get; }
    public WordTypeInfoOption? WordTypeInfo { get; }
    public SpellingRestrictionInfoOption? SpellingRestrictionInfo { get; }
    public ExtraDefinitionInfoOption? ExtraDefinitionInfo { get; }
    public MiscInfoOption? MiscInfo { get; }

    public DictOptions
        (
        NewlineBetweenDefinitionsOption? newlineBetweenDefinitions = null,
        ExamplesOption? examples = null,
        NoAllOption? noAll = null,
        WordClassInfoOption? wordClassInfo = null,
        DialectInfoOption? dialectInfo = null,
        POrthographyInfoOption? pOrthographyInfo = null,
        POrthographyInfoColorOption? pOrthographyInfoColor = null,
        POrthographyInfoFontSizeOption? pOrthographyInfoFontSize = null,
        AOrthographyInfoOption? aOrthographyInfo = null,
        ROrthographyInfoOption? rOrthographyInfo = null,
        WordTypeInfoOption? wordTypeInfo = null,
        SpellingRestrictionInfoOption? spellingRestrictionInfo = null,
        ExtraDefinitionInfoOption? extraDefinitionInfo = null,
        MiscInfoOption? miscInfo = null
        )
    {
        NewlineBetweenDefinitions = newlineBetweenDefinitions;
        Examples = examples;
        NoAll = noAll;
        WordClassInfo = wordClassInfo;
        DialectInfo = dialectInfo;
        POrthographyInfo = pOrthographyInfo;
        POrthographyInfoColor = pOrthographyInfoColor;
        POrthographyInfoFontSize = pOrthographyInfoFontSize;
        AOrthographyInfo = aOrthographyInfo;
        ROrthographyInfo = rOrthographyInfo;
        WordTypeInfo = wordTypeInfo;
        SpellingRestrictionInfo = spellingRestrictionInfo;
        ExtraDefinitionInfo = extraDefinitionInfo;
        MiscInfo = miscInfo;
    }
}
