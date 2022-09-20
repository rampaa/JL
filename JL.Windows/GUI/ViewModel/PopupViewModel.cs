using JL.Core.Dicts;
using JL.Core.Lookup;
using JL.Windows.GUI.MVVM;

namespace JL.Windows.GUI.ViewModel;

public class PopupViewModel : ViewModelBase
{
    // TODO: this binding currently doesn't work because PopupViewModel is not set as the DataContext of PopupWindow
    private bool _visibility;

    public bool Visibility
    {
        get { return _visibility; }
        set { SetProperty(ref _visibility, value); }
    }

    public void Hide()
    {
        Visibility = false;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    private int _playAudioIndex;

    public int PlayAudioIndex
    {
        get { return _playAudioIndex; }
        set { SetProperty(ref _playAudioIndex, value); }
    }

    private int _currentCharPosition;

    public int CurrentCharPosition
    {
        get { return _currentCharPosition; }
        set { SetProperty(ref _currentCharPosition, value); }
    }

    private string? _currentText;

    public string? CurrentText
    {
        get { return _currentText; }
        set { SetProperty(ref _currentText, value); }
    }

    private string? _lastSelectedText;

    public string? LastSelectedText
    {
        get { return _lastSelectedText; }
        set { SetProperty(ref _lastSelectedText, value); }
    }

    private WinApi? _winApi;

    public WinApi? WinApi
    {
        get { return _winApi; }
        set { SetProperty(ref _winApi, value); }
    }

    private List<LookupResult> _lastLookupResults = new();

    public List<LookupResult> LastLookupResults
    {
        get { return _lastLookupResults; }
        set { SetProperty(ref _lastLookupResults, value); }
    }

    private Dict? _filteredDict;

    public Dict? FilteredDict
    {
        get { return _filteredDict; }
        set { SetProperty(ref _filteredDict, value); }
    }

    private bool _unavoidableMouseEnter;

    public bool UnavoidableMouseEnter
    {
        get { return _unavoidableMouseEnter; }
        set { SetProperty(ref _unavoidableMouseEnter, value); }
    }

    private string? _lastText;

    public string? LastText
    {
        get { return _lastText; }
        set { SetProperty(ref _lastText, value); }
    }

    private bool _miningMode;

    public bool MiningMode
    {
        get { return _miningMode; }
        set { SetProperty(ref _miningMode, value); }
    }
}
