using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using JL.Windows.Utilities;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for ReadingSelectionWindow.xaml
/// </summary>
internal sealed partial class ReadingSelectionWindow : Window
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
        currentInstance._primarySpelling = primarySpelling;
        currentInstance.ReadingsListView.ItemsSource = readings;
        currentInstance.ReadingsListView.Background = ConfigManager.PopupBackgroundColor;
        currentInstance.ReadingsListView.Foreground = ConfigManager.ReadingsColor;
        currentInstance.ReadingsListView.FontSize = 18;
        currentInstance.Background = ConfigManager.PopupBackgroundColor;
        currentInstance.Foreground = ConfigManager.DefinitionsColor;
        currentInstance.FontFamily = ConfigManager.PopupFont;
        currentInstance.Owner = owner;
        currentInstance.Show();
        currentInstance.UpdatePosition(WinApi.GetMousePosition());
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _windowHandle = new WindowInteropHelper(this).Handle;
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);

        if (ConfigManager.Focusable)
        {
            WinApi.AllowActivation(_windowHandle);
        }
        else
        {
            WinApi.PreventActivation(_windowHandle);
        }
    }

    // ReSharper disable once AsyncVoidMethod
    private async void ReadingsListView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        string selectedReading = (string)((ListViewItem)sender).Content;
        Hide();
        await PopupWindowUtils.PlayAudio(_primarySpelling!, selectedReading).ConfigureAwait(false);
    }

    private void UpdatePosition(Point cursorPosition)
    {
        double mouseX = cursorPosition.X;
        double mouseY = cursorPosition.Y;

        double currentWidth = ActualWidth * WindowsUtils.Dpi.DpiScaleX;
        double currentHeight = ActualHeight * WindowsUtils.Dpi.DpiScaleY;

        double dpiAwareXOffSet = 5 * WindowsUtils.Dpi.DpiScaleX;
        double dpiAwareYOffset = 15 * WindowsUtils.Dpi.DpiScaleY;

        bool needsFlipX = mouseX + currentWidth > WindowsUtils.ActiveScreen.Bounds.Right;
        bool needsFlipY = mouseY + currentHeight > WindowsUtils.ActiveScreen.Bounds.Bottom;

        double newLeft;
        double newTop;

        if (needsFlipX)
        {
            // flip Leftwards while preventing -OOB
            newLeft = mouseX - currentWidth - dpiAwareXOffSet;
            if (newLeft < WindowsUtils.ActiveScreen.Bounds.X)
            {
                newLeft = WindowsUtils.ActiveScreen.Bounds.X;
            }
        }
        else
        {
            // no flip
            newLeft = mouseX - dpiAwareXOffSet;
        }

        if (needsFlipY)
        {
            // flip Upwards while preventing -OOB
            newTop = mouseY - (currentHeight + dpiAwareYOffset);
            if (newTop < WindowsUtils.ActiveScreen.Bounds.Y)
            {
                newTop = WindowsUtils.ActiveScreen.Bounds.Y;
            }
        }
        else
        {
            // no flip
            newTop = mouseY + dpiAwareYOffset;
        }

        // stick to edges if +OOB
        if (newLeft + currentWidth > WindowsUtils.ActiveScreen.Bounds.Right)
        {
            newLeft = WindowsUtils.ActiveScreen.Bounds.Right - currentWidth;
        }

        if (newTop + currentHeight > WindowsUtils.ActiveScreen.Bounds.Bottom)
        {
            newTop = WindowsUtils.ActiveScreen.Bounds.Bottom - currentHeight;
        }

        WinApi.MoveWindowToPosition(_windowHandle, newLeft, newTop);
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
