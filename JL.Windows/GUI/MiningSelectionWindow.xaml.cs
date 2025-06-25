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

    internal static void Show(PopupWindow owner, LookupResult[] lookupResults, int currentLookupResultIndex, string currentSourceText, int currentSourceTextCharPosition, Point position)
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
        WindowsUtils.UpdatePositionForSelectionWindows(currentInstance, currentInstance._windowHandle, position);

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
        WinApi.SetNoRedirectionBitmap(_windowHandle);
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
        string selectedSpelling = (string)((ListViewItem)sender).Content;
        await MineSelectedSpelling(_popupWindow, _lookupResults, _currentLookupResultIndex, _currentSourceText, _currentSourceTextCharPosition, selectedSpelling).ConfigureAwait(false);
    }

    private static Task MineSelectedSpelling()
    {
        if (s_instance is null)
        {
            return Task.CompletedTask;
        }

        string selectedSpelling = (string)((ListViewItem)s_instance.MiningListView.SelectedItem).Content;
        return MineSelectedSpelling(s_instance._popupWindow, s_instance._lookupResults, s_instance._currentLookupResultIndex, s_instance._currentSourceText, s_instance._currentSourceTextCharPosition, selectedSpelling);
    }

    private static Task MineSelectedSpelling(PopupWindow popupWindow, LookupResult[] lookupResults, int currentLookupResultIndex, string currentSourceText, int currentSourceTextCharPosition, string selectedSpelling)
    {
        TextBox? definitionsTextBox = popupWindow.GetDefinitionTextBox(currentLookupResultIndex);
        string? formattedDefinitions = definitionsTextBox?.Text;
        string? selectedDefinitions = PopupWindowUtils.GetSelectedDefinitions(definitionsTextBox);

        if (popupWindow.PopupIndex is 0)
        {
            MainWindow mainWindow = MainWindow.Instance;
            if (ConfigManager.Instance.AutoPauseOrResumeMpvOnHoverChange)
            {
                mainWindow.MouseEnterDueToFirstPopupHide = mainWindow.IsMouseWithinWindowBounds();
            }
            popupWindow.HidePopup();
            mainWindow.ChangeVisibility();
        }
        else
        {
            popupWindow.HidePopup();
        }

        return ConfigManager.Instance.MineToFileInsteadOfAnki
            ? MiningUtils.MineToFile(lookupResults, currentLookupResultIndex, currentSourceText, formattedDefinitions, selectedDefinitions, currentSourceTextCharPosition, selectedSpelling)
            : MiningUtils.Mine(lookupResults, currentLookupResultIndex, currentSourceText, formattedDefinitions, selectedDefinitions, currentSourceTextCharPosition, selectedSpelling);
    }

    public static void CloseWindow()
    {
        if (s_instance is not null)
        {
            s_instance.Close();
            s_instance.MiningListView.SelectedItem = null;
            s_instance = null;
        }
    }

    private void Window_LostFocus(object sender, RoutedEventArgs e)
    {
        CloseWindow();
    }

    // ReSharper disable once AsyncVoidMethod
    private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        await KeyGestureUtils.HandleKeyDown(e).ConfigureAwait(false);
    }

    public static Task HandleHotKey(KeyGesture keyGesture)
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (keyGesture.IsEqual(configManager.ClosePopupKeyGesture))
        {
            CloseWindow();
        }

        else if (keyGesture.IsEqual(configManager.SelectNextItemKeyGesture))
        {
            if (s_instance is not null)
            {
                WindowsUtils.SelectNextListViewItem(s_instance.MiningListView);
            }
        }

        else if (keyGesture.IsEqual(configManager.SelectPreviousItemKeyGesture))
        {
            if (s_instance is not null)
            {
                WindowsUtils.SelectPreviousListViewItem(s_instance.MiningListView);
            }
        }

        else if (keyGesture.IsEqual(configManager.ConfirmItemSelectionKeyGesture))
        {
            return MineSelectedSpelling();
        }

        else if (keyGesture.IsEqual(KeyGestureUtils.AltF4KeyGesture))
        {
            CloseWindow();
        }

        return Task.CompletedTask;
    }
}
