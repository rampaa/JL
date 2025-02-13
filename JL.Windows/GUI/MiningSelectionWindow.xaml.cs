using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using JL.Core.Lookup;
using JL.Core.Mining;
using JL.Windows.Utilities;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for MiningSelectionWindow.xaml
/// </summary>
internal sealed partial class MiningSelectionWindow
{
    private readonly PopupWindow _popupWindow;
    private readonly LookupResult[] _lookupResults;
    private readonly int _currentLookupResultIndex;
    private readonly string _currentSourceText;
    private readonly int _currentSourceTextCharPosition;

    private nint _windowHandle;

    private static MiningSelectionWindow? s_instance;

    private MiningSelectionWindow(PopupWindow owner, LookupResult[] lookupResults, int currentLookupResultIndex, string currentSourceText, int currentSourceTextCharPosition)
    {
        InitializeComponent();
        Owner = owner;
        _popupWindow = owner;
        _lookupResults = lookupResults;
        _currentLookupResultIndex = currentLookupResultIndex;
        _currentSourceText = currentSourceText;
        _currentSourceTextCharPosition = currentSourceTextCharPosition;
    }

    public static bool IsItVisible()
    {
        return s_instance?.IsVisible ?? false;
    }

    internal static void Show(PopupWindow owner, LookupResult[] lookupResults, int currentLookupResultIndex, string currentSourceText, int currentSourceTextCharPosition)
    {
        MiningSelectionWindow currentInstance = s_instance ??= new MiningSelectionWindow(owner, lookupResults, currentLookupResultIndex, currentSourceText, currentSourceTextCharPosition);
        ConfigManager configManager = ConfigManager.Instance;

        ListViewItem[] listViewItem;
        LookupResult lookupResult = lookupResults[currentLookupResultIndex];
        if (lookupResult.Readings is not null)
        {
            listViewItem = new ListViewItem[1 + lookupResult.Readings.Length];
            for (int i = 0; i < lookupResult.Readings.Length; i++)
            {
                listViewItem[i + 1] = new ListViewItem { Content = lookupResult.Readings[i], Foreground = configManager.ReadingsColor };
            }
        }
        else
        {
            listViewItem = new ListViewItem[1];
        }

        listViewItem[0] = new ListViewItem { Content = lookupResult.PrimarySpelling, Foreground = configManager.PrimarySpellingColor };
        currentInstance.MiningListView.ItemsSource = listViewItem;
        currentInstance.MiningListView.Background = configManager.PopupBackgroundColor;
        currentInstance.MiningListView.FontSize = 18;
        currentInstance.Background = configManager.PopupBackgroundColor;
        currentInstance.Foreground = configManager.DefinitionsColor;
        currentInstance.FontFamily = configManager.PopupFont;
        currentInstance.Owner = owner;
        currentInstance.Show();
        WindowsUtils.UpdatePositionForSelectionWindows(currentInstance, currentInstance._windowHandle, WinApi.GetMousePosition());

        if (configManager.Focusable)
        {
            _ = currentInstance.Activate();
        }
        _ = currentInstance.Focus();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _windowHandle = new WindowInteropHelper(this).Handle;
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);

        if (ConfigManager.Instance.Focusable)
        {
            WinApi.AllowActivation(_windowHandle);
        }
        else
        {
            WinApi.PreventActivation(_windowHandle);
        }
    }

    // ReSharper disable once AsyncVoidMethod
    private async void MiningListView_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        TextBox? definitionsTextBox = _popupWindow.GetDefinitionTextBox(_currentLookupResultIndex);
        string? formattedDefinitions = definitionsTextBox?.Text;
        string? selectedDefinitions = PopupWindowUtils.GetSelectedDefinitions(definitionsTextBox);

        string selectedSpelling = (string)((ListViewItem)sender).Content;
        _popupWindow.HidePopup();

        ConfigManager configManager = ConfigManager.Instance;
        if (configManager.MineToFileInsteadOfAnki)
        {
            await MiningUtils.MineToFile(_lookupResults, _currentLookupResultIndex, _currentSourceText, formattedDefinitions, selectedDefinitions, _currentSourceTextCharPosition, selectedSpelling).ConfigureAwait(false);
        }
        else
        {
            await MiningUtils.Mine(_lookupResults, _currentLookupResultIndex, _currentSourceText, formattedDefinitions, selectedDefinitions, _currentSourceTextCharPosition, selectedSpelling).ConfigureAwait(false);
        }
    }

    public static void CloseWindow()
    {
        s_instance?.Close();
        s_instance = null;
    }

    private void Window_LostFocus(object sender, RoutedEventArgs e)
    {
        CloseWindow();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        s_instance = null;
    }
}
