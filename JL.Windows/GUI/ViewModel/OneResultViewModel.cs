using System.Globalization;
using System.Text;
using JetBrains.Annotations;
using JL.Core;
using JL.Core.Anki;
using JL.Core.Dicts;
using JL.Core.Lookup;
using JL.Core.Utilities;
using JL.Windows.GUI.MVVM;

namespace JL.Windows.GUI.ViewModel;

public class OneResultViewModel : ViewModelBase
{
    public ConfigManager ConfigManager => ConfigManager.Instance;

    public PopupViewModel PopupVm { get; }

    public OneResultViewModel(LookupResult result, PopupViewModel popupVm, int index)
    {
        PopupVm = popupVm;
        Index = index;
        Gen(result);
    }

    private int _index;

    public int Index
    {
        get { return _index; }
        set { SetProperty(ref _index, value); }
    }

    private string _matchedText = null!;

    public string MatchedText
    {
        get { return _matchedText; }
        set { SetProperty(ref _matchedText, value); }
    }

    private string _deconjugatedMatchedText = null!;

    public string DeconjugatedMatchedText
    {
        get { return _deconjugatedMatchedText; }
        set { SetProperty(ref _deconjugatedMatchedText, value); }
    }

    private string _edictId = null!;

    public string EdictId
    {
        get { return _edictId; }
        set { SetProperty(ref _edictId, value); }
    }

    private string _primarySpelling = null!;

    public string PrimarySpelling
    {
        get { return _primarySpelling; }
        set { SetProperty(ref _primarySpelling, value); }
    }

    private string _pOrthographyInfo = null!;

    public string POrthographyInfo
    {
        get { return _pOrthographyInfo; }
        set { SetProperty(ref _pOrthographyInfo, value); }
    }

    private string _readings = null!;

    public string Readings
    {
        get { return _readings; }
        set { SetProperty(ref _readings, value); }
    }

    private string _alternativeSpellings = null!;

    public string AlternativeSpellings
    {
        get { return _alternativeSpellings; }
        set { SetProperty(ref _alternativeSpellings, value); }
    }

    private string _process = null!;

    public string Process
    {
        get { return _process; }
        set { SetProperty(ref _process, value); }
    }

    private string _frequency = null!;

    public string Frequency
    {
        get { return _frequency; }
        set { SetProperty(ref _frequency, value); }
    }

    private Dict _dict = null!;

    public Dict Dict
    {
        get { return _dict; }
        set { SetProperty(ref _dict, value); }
    }

    private string _definitions = null!;

    public string Definitions
    {
        get { return _definitions; }
        set { SetProperty(ref _definitions, value); }
    }

    private string _onReadings = null!;

    public string OnReadings
    {
        get { return _onReadings; }
        set { SetProperty(ref _onReadings, value); }
    }

    private string _kunReadings = null!;

    public string KunReadings
    {
        get { return _kunReadings; }
        set { SetProperty(ref _kunReadings, value); }
    }

    private string _nanoriReadings = null!;

    public string NanoriReadings
    {
        get { return _nanoriReadings; }
        set { SetProperty(ref _nanoriReadings, value); }
    }

    private string _kanjiGrade = null!;

    public string KanjiGrade
    {
        get { return _kanjiGrade; }
        set { SetProperty(ref _kanjiGrade, value); }
    }

    private string _strokeCount = null!;

    public string StrokeCount
    {
        get { return _strokeCount; }
        set { SetProperty(ref _strokeCount, value); }
    }

    private string _kanjiComposition = null!;

    public string KanjiComposition
    {
        get { return _kanjiComposition; }
        set { SetProperty(ref _kanjiComposition, value); }
    }

    private string _kanjiStats = null!;

    public string KanjiStats
    {
        get { return _kanjiStats; }
        set { SetProperty(ref _kanjiStats, value); }
    }

    // private ICommand? _mouseLeaveCommand;
    //
    // public ICommand MouseLeaveCommand
    // {
    //     get
    //     {
    //         return _mouseLeaveCommand ??= new Command(() =>
    //             Storage.Frontend.Alert(AlertLevel.Information, "MouseLeave"));
    //     }
    //     set { _mouseLeaveCommand = value; }
    // }

    // TODO: Consider making these dependent on OneResult itself rather than just the PrimarySpelling
    [UsedImplicitly]
    public void PrimarySpelling_MouseEnter()
    {
        PopupVm.PlayAudioIndex = Index;
    }

    [UsedImplicitly]
    public void PrimarySpelling_MouseLeave()
    {
        PopupVm.PlayAudioIndex = 0;
    }

    public async Task VM_PrimarySpelling_PreviewMouseUp()
    {
        PopupVm.MiningMode = false;
        // PopupVm.Visibility = false; // todo

        var miningParams = new Dictionary<JLField, string>();

        if (PopupVm.CurrentText != null)
        {
            miningParams[JLField.SourceText] = PopupVm.CurrentText;
            miningParams[JLField.Sentence] = Utils.FindSentence(PopupVm.CurrentText, PopupVm.CurrentCharPosition);
        }

        miningParams[JLField.Readings] = Readings;
        miningParams[JLField.AlternativeSpellings] = AlternativeSpellings;
        miningParams[JLField.PrimarySpelling] = PrimarySpelling;
        miningParams[JLField.MatchedText] = MatchedText;
        miningParams[JLField.DeconjugatedMatchedText] = DeconjugatedMatchedText;
        miningParams[JLField.EdictId] = EdictId;
        miningParams[JLField.Frequencies] = Frequency;
        miningParams[JLField.DictionaryName] = Dict.Name;
        miningParams[JLField.DeconjugationProcess] = Process;
        miningParams[JLField.Definitions] = Definitions.Replace("\n", "<br/>");
        miningParams[JLField.OnReadings] = OnReadings;
        miningParams[JLField.KunReadings] = KunReadings;
        miningParams[JLField.NanoriReadings] = NanoriReadings;
        miningParams[JLField.StrokeCount] = StrokeCount;
        miningParams[JLField.KanjiGrade] = KanjiGrade;
        miningParams[JLField.KanjiComposition] = KanjiComposition;
        miningParams[JLField.KanjiStats] = KanjiStats; // TODO(rampaa): This is new. Test if it works correctly.
        miningParams[JLField.LocalTime] = DateTime.Now.ToString("s", CultureInfo.InvariantCulture);

        bool miningResult = await Mining.Mine(miningParams).ConfigureAwait(false);

        if (miningResult)
        {
            Stats.IncrementStat(StatType.CardsMined);
        }
    }

    private void Gen(LookupResult result)
    {
        MatchedText = result.MatchedText;
        DeconjugatedMatchedText = result.DeconjugatedMatchedText;
        EdictId = result.EdictId.ToString();

        PrimarySpelling = result.PrimarySpelling;

        if ((result.POrthographyInfoList?.Any() ?? false) && (result.Dict.Options?.POrthographyInfo?.Value ?? true))
        {
            POrthographyInfo = $"({string.Join(", ", result.POrthographyInfoList)})";
        }

        if (result.Readings?.Any() ?? false)
        {
            List<string> rOrthographyInfoList = result.ROrthographyInfoList ?? new();
            List<string> readings = result.Readings;
            Readings = rOrthographyInfoList.Any() && (result.Dict.Options?.ROrthographyInfo?.Value ?? true)
                ? PopupWindowUtilities.MakeUiElementReadingsText(readings, rOrthographyInfoList)
                : string.Join(", ", result.Readings);
        }

        if (result.AlternativeSpellings?.Any() ?? false)
        {
            List<string> aOrthographyInfoList = result.AOrthographyInfoList ?? new List<string>();
            List<string> alternativeSpellings = result.AlternativeSpellings;
            AlternativeSpellings = aOrthographyInfoList.Any() && (result.Dict.Options?.AOrthographyInfo?.Value ?? true)
                ? PopupWindowUtilities.MakeUiElementAlternativeSpellingsText(alternativeSpellings, aOrthographyInfoList)
                : "(" + string.Join(", ", alternativeSpellings) + ")";
        }

        if (result.Process != null)
        {
            Process = result.Process;
        }

        // TODO: MakeUiElementFrequenciesText
        if (result.Frequencies?.Count > 0)
        {
            string freqStr = "";

            if (result.Frequencies.Count == 1 && result.Frequencies[0].Freq > 0 &&
                result.Frequencies[0].Freq != int.MaxValue)
            {
                freqStr = "#" + result.Frequencies.First().Freq;
            }
            else if (result.Frequencies.Count > 1)
            {
                int freqResultCount = 0;
                StringBuilder freqStrBuilder = new();
                foreach (LookupFrequencyResult lookupFreqResult in result.Frequencies)
                {
                    if (lookupFreqResult.Freq is int.MaxValue or <= 0)
                        continue;

                    freqStrBuilder.Append($"{lookupFreqResult.Name}: #{lookupFreqResult.Freq}, ");
                    freqResultCount++;
                }

                if (freqResultCount > 0)
                {
                    freqStrBuilder.Remove(freqStrBuilder.Length - 2, 1);

                    freqStr = freqStrBuilder.ToString();
                }
            }

            Frequency = freqStr;
        }

        Dict = result.Dict;

        if (result.FormattedDefinitions != null && result.FormattedDefinitions.Any())
        {
            // string buf = "";
            // int lineLength = 0;
            // foreach (char ch in result.FormattedDefinitions)
            // {
            //     buf += ch;
            //     lineLength += 1;
            //
            //     if (ch == '\n')
            //     {
            //         lineLength = 0;
            //     }
            //
            //     if (lineLength % 35 == 0)
            //     {
            //         buf += "\n";
            //     }
            // }
            //
            // Definitions = buf;

            Definitions = result.FormattedDefinitions;
        }

        // Kanji results
        if (result.OnReadings?.Any() ?? false)
        {
            OnReadings = "On" + ": " + string.Join(", ", result.OnReadings);
        }

        if (result.KunReadings?.Any() ?? false)
        {
            KunReadings = "Kun" + ": " + string.Join(", ", result.KunReadings);
        }

        if (result.NanoriReadings?.Any() ?? false)
        {
            NanoriReadings = "Nanori" + ": " + string.Join(", ", result.NanoriReadings);
        }

        // TODO: MakeUiElementKanjiGradeText
        if (result.KanjiGrade > 0)
        {
            string gradeString = "";
            int gradeInt = result.KanjiGrade;
            switch (gradeInt)
            {
                case 0:
                    gradeString = "Hyougai";
                    break;
                case <= 6:
                    gradeString = $"{gradeInt} (Kyouiku)";
                    break;
                case 8:
                    gradeString = $"{gradeInt} (Jouyou)";
                    break;
                case <= 10:
                    gradeString = $"{gradeInt} (Jinmeiyou)";
                    break;
            }

            KanjiGrade = "Grade" + ": " + gradeString;
        }

        if (result.StrokeCount > 0)
        {
            StrokeCount = "Strokes" + ": " + result.StrokeCount;
        }

        if (result.KanjiComposition?.Any() ?? false)
        {
            KanjiComposition = "Composition: " + result.KanjiComposition;
        }

        if (result.KanjiStats?.Any() ?? false)
        {
            KanjiStats = "Statistics:\n" + result.KanjiStats;
        }
    }
}
