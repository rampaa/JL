using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using JL.Windows.Utilities;
using Rectangle = System.Drawing.Rectangle;

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
        currentInstance.UpdatePosition(WinApi.GetMousePosition());

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

    private void UpdatePosition(Point cursorPosition)
    {
        double mouseX = cursorPosition.X;
        double mouseY = cursorPosition.Y;

        DpiScale dpi = WindowsUtils.Dpi;
        double currentWidth = ActualWidth * dpi.DpiScaleX;
        double currentHeight = ActualHeight * dpi.DpiScaleY;

        double dpiAwareXOffSet = 5 * dpi.DpiScaleX;
        double dpiAwareYOffset = 15 * dpi.DpiScaleY;

        Rectangle bounds = WindowsUtils.ActiveScreen.Bounds;
        bool needsFlipX = mouseX + currentWidth > bounds.Right;
        bool needsFlipY = mouseY + currentHeight > bounds.Bottom;

        double newLeft;
        double newTop;

        if (needsFlipX)
        {
            // flip Leftwards while preventing -OOB
            newLeft = mouseX - currentWidth - dpiAwareXOffSet;
            if (newLeft < bounds.X)
            {
                newLeft = bounds.X;
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
            if (newTop < bounds.Y)
            {
                newTop = bounds.Y;
            }
        }
        else
        {
            // no flip
            newTop = mouseY + dpiAwareYOffset;
        }

        // stick to edges if +OOB
        if (newLeft + currentWidth > bounds.Right)
        {
            newLeft = bounds.Right - currentWidth;
        }

        if (newTop + currentHeight > bounds.Bottom)
        {
            newTop = bounds.Bottom - currentHeight;
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
