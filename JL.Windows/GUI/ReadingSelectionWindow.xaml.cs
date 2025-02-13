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
    private string? _primarySpelling;
    private nint _windowHandle;

    private static ReadingSelectionWindow? s_instance;

    private ReadingSelectionWindow()
    {
        InitializeComponent();
    }

    public static bool IsItVisible()
    {
        return s_instance?.IsVisible ?? false;
    }

    public static void Show(Window owner, string primarySpelling, string[] readings)
    {
        ReadingSelectionWindow currentInstance = s_instance ??= new ReadingSelectionWindow();
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
    private async void ReadingsListView_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        string selectedReading = (string)((ListViewItem)sender).Content;
        Hide();
        await PopupWindowUtils.PlayAudio(_primarySpelling!, selectedReading).ConfigureAwait(false);
    }

    public static void HideWindow()
    {
        s_instance?.Hide();
    }

    private void Window_LostFocus(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}
