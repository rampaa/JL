namespace JL.Core.Dicts.Options;

public sealed class DictOptions(
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
    UseDBOption? useDB = null,
    ShowPitchAccentWithDottedLinesOption? showPitchAccentWithDottedLines = null,
    AutoUpdateAfterNDaysOption? autoUpdateAfterNDays = null)
{
    public NewlineBetweenDefinitionsOption? NewlineBetweenDefinitions { get; } = newlineBetweenDefinitions;
    public ExamplesOption? Examples { get; } = examples;
    public NoAllOption? NoAll { get; } = noAll;
    public PitchAccentMarkerColorOption? PitchAccentMarkerColor { get; } = pitchAccentMarkerColor;
    public WordClassInfoOption? WordClassInfo { get; } = wordClassInfo;
    public DialectInfoOption? DialectInfo { get; } = dialectInfo;
    public POrthographyInfoOption? POrthographyInfo { get; } = pOrthographyInfo;
    public POrthographyInfoColorOption? POrthographyInfoColor { get; } = pOrthographyInfoColor;
    public POrthographyInfoFontSizeOption? POrthographyInfoFontSize { get; } = pOrthographyInfoFontSize;
    public AOrthographyInfoOption? AOrthographyInfo { get; } = aOrthographyInfo;
    public ROrthographyInfoOption? ROrthographyInfo { get; } = rOrthographyInfo;
    public WordTypeInfoOption? WordTypeInfo { get; } = wordTypeInfo;
    public SpellingRestrictionInfoOption? SpellingRestrictionInfo { get; } = spellingRestrictionInfo;
    public ExtraDefinitionInfoOption? ExtraDefinitionInfo { get; } = extraDefinitionInfo;
    public MiscInfoOption? MiscInfo { get; } = miscInfo;
    public RelatedTermOption? RelatedTerm { get; } = relatedTerm;
    public AntonymOption? Antonym { get; } = antonym;
    public LoanwordEtymologyOption? LoanwordEtymology { get; } = loanwordEtymology;
    public UseDBOption? UseDB { get; } = useDB;
    public ShowPitchAccentWithDottedLinesOption? ShowPitchAccentWithDottedLines { get; } = showPitchAccentWithDottedLines;
    public AutoUpdateAfterNDaysOption? AutoUpdateAfterNDays { get; } = autoUpdateAfterNDays;
}
