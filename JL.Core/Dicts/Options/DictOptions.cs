namespace JL.Core.Dicts.Options;

public sealed class DictOptions
{
    public NewlineBetweenDefinitionsOption? NewlineBetweenDefinitions { get; }
    public ExamplesOption? Examples { get; }
    public NoAllOption? NoAll { get; }
    public PitchAccentMarkerColorOption? PitchAccentMarkerColor { get; }
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
    public RelatedTermOption? RelatedTerm { get; }
    public AntonymOption? Antonym { get; }
    public LoanwordEtymologyOption? LoanwordEtymology { get; }
    public UseDBOption? UseDB { get; }

    public DictOptions
    (
        NewlineBetweenDefinitionsOption? newlineBetweenDefinitions = null,
        ExamplesOption? examples = null,
        NoAllOption? noAll = null,
        PitchAccentMarkerColorOption? pitchAccentMarkerColor = null,
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
        MiscInfoOption? miscInfo = null,
        LoanwordEtymologyOption? loanwordEtymology = null,
        RelatedTermOption? relatedTerm = null,
        AntonymOption? antonym = null,
        UseDBOption? useDB = null
    )
    {
        NewlineBetweenDefinitions = newlineBetweenDefinitions;
        Examples = examples;
        NoAll = noAll;
        PitchAccentMarkerColor = pitchAccentMarkerColor;
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
        LoanwordEtymology = loanwordEtymology;
        RelatedTerm = relatedTerm;
        Antonym = antonym;
        UseDB = useDB;
    }
}
