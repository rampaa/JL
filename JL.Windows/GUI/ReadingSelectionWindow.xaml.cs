using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using JL.Windows.Utilities;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for ReadingSelectionWindow.xaml
/// </summary>
internal sealed partial class ReadingSelectionWindow
{
    private string _primarySpelling;
    private nint _windowHandle;

    private static ReadingSelectionWindow? s_instance;

    private ReadingSelectionWindow(string primarySpelling)
    {
        InitializeComponent();
        _primarySpelling = primarySpelling;
    }

    public static bool IsItVisible()
    {
        return s_instance?.IsVisible ?? false;
    }

    public static void Show(Window owner, string primarySpelling, string[] readings, Point position)
    {
        ReadingSelectionWindow currentInstance = s_instance ??= new ReadingSelectionWindow(primarySpelling);
        ConfigManager configManager = ConfigManager.Instance;
        currentInstance._primarySpelling = primarySpelling;
        currentInstance.ReadingsListView.ItemsSource = readings;
        currentInstance.ReadingsListView.Background = configManager.PopupBackgroundColor;
        currentInstance.ReadingsListView.Foreground = configManager.ReadingsColor;
        currentInstance.ReadingsListView.FontSize = 18;
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
        WinApi.SetNoRedirectionBitmapStyle(_windowHandle);
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
    private async void ReadingsListView_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        string selectedReading = (string)((ListViewItem)sender).Content;
        HideWindow();
        await PlaySelectedReading(_primarySpelling, selectedReading).ConfigureAwait(false);
    }

    private static Task PlaySelectedReading()
    {
        if (s_instance is null)
        {
            return Task.CompletedTask;
        }

        string selectedReading = (string)s_instance.ReadingsListView.SelectedItem;
        HideWindow();
        return PlaySelectedReading(s_instance._primarySpelling, selectedReading);
    }

    private static Task PlaySelectedReading(string primarySpelling, string selectedReading)
    {
        return PopupWindowUtils.PlayAudio(primarySpelling, selectedReading);
    }

    public static void HideWindow()
    {
        if (s_instance is not null)
        {
            s_instance.Hide();
            s_instance.ReadingsListView.SelectedItem = null;
        }
    }

    private void Window_LostFocus(object sender, RoutedEventArgs e)
    {
        HideWindow();
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
            HideWindow();
        }

        else if (keyGesture.IsEqual(configManager.SelectNextItemKeyGesture))
        {
            if (s_instance is not null)
            {
                WindowsUtils.SelectNextListViewItem(s_instance.ReadingsListView);
            }
        }

        else if (keyGesture.IsEqual(configManager.SelectPreviousItemKeyGesture))
        {
            if (s_instance is not null)
            {
                WindowsUtils.SelectPreviousListViewItem(s_instance.ReadingsListView);
            }
        }

        else if (keyGesture.IsEqual(configManager.ConfirmItemSelectionKeyGesture))
        {
            return PlaySelectedReading();
        }

        else if (keyGesture.IsEqual(KeyGestureUtils.AltF4KeyGesture))
        {
            HideWindow();
        }

        return Task.CompletedTask;
    }
}
