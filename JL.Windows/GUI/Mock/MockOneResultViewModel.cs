using JL.Core.Dicts;
using JL.Core.Dicts.Options;
using JL.Core.Lookup;
using JL.Windows.GUI.ViewModel;

namespace JL.Windows.GUI.Mock;

public static class MockOneResultViewModel
{
    static MockOneResultViewModel()
    {
        var result = new LookupResult
        (
            matchedText: "始まる",
            dict: new Dict(DictType.JMdict, "JMdict", "", true, 0, 0, new DictOptions()),
            frequencies: new List<LookupFrequencyResult> { new("VN", 759), new ("Novel", 634) },
            primarySpelling: "始まる",
            deconjugatedMatchedText: "始まる",
            readings: new List<string> { "はじまる" },
            formattedDefinitions:
            "(v5r, vi) (1) to begin; to start; to commence (v5r, vi) (2) to happen (again); to begin (anew) (v5r, vi) (3) to date (from); to originate (in)",
            edictId: 1307500,
            aOrthographyInfoList: new List<string>(),
            rOrthographyInfoList: new List<string>()
        );

        ViewModel = new OneResultViewModel(result, new PopupViewModel(), 0);
    }

    public static OneResultViewModel ViewModel { get; set; }
}
