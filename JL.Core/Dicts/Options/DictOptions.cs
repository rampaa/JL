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
    public NewlineBetweenDefinitionsOption? NewlineBetweenDefinitions { get; internal set; } = newlineBetweenDefinitions;
    public ExamplesOption? Examples { get; internal set; } = examples;
    public NoAllOption? NoAll { get; internal set; } = noAll;
    public PitchAccentMarkerColorOption? PitchAccentMarkerColor { get; internal set; } = pitchAccentMarkerColor;
    public WordClassInfoOption? WordClassInfo { get; internal set; } = wordClassInfo;
    public DialectInfoOption? DialectInfo { get; internal set; } = dialectInfo;
    public POrthographyInfoOption? POrthographyInfo { get; internal set; } = pOrthographyInfo;
    public POrthographyInfoColorOption? POrthographyInfoColor { get; internal set; } = pOrthographyInfoColor;
    public POrthographyInfoFontSizeOption? POrthographyInfoFontSize { get; internal set; } = pOrthographyInfoFontSize;
    public AOrthographyInfoOption? AOrthographyInfo { get; internal set; } = aOrthographyInfo;
    public ROrthographyInfoOption? ROrthographyInfo { get; internal set; } = rOrthographyInfo;
    public WordTypeInfoOption? WordTypeInfo { get; internal set; } = wordTypeInfo;
    public SpellingRestrictionInfoOption? SpellingRestrictionInfo { get; internal set; } = spellingRestrictionInfo;
    public ExtraDefinitionInfoOption? ExtraDefinitionInfo { get; internal set; } = extraDefinitionInfo;
    public MiscInfoOption? MiscInfo { get; internal set; } = miscInfo;
    public RelatedTermOption? RelatedTerm { get; internal set; } = relatedTerm;
    public AntonymOption? Antonym { get; internal set; } = antonym;
    public LoanwordEtymologyOption? LoanwordEtymology { get; internal set; } = loanwordEtymology;
    public UseDBOption? UseDB { get; internal set; } = useDB;
    public ShowPitchAccentWithDottedLinesOption? ShowPitchAccentWithDottedLines { get; internal set; } = showPitchAccentWithDottedLines;
    public AutoUpdateAfterNDaysOption? AutoUpdateAfterNDays { get; internal set; } = autoUpdateAfterNDays;
}
