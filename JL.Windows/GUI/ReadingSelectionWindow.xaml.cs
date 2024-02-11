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

    public static void Show(string primarySpelling, string[] readings, Window window)
    {
        ReadingSelectionWindow currentInstance = s_instance ??= new ReadingSelectionWindow();
        currentInstance._primarySpelling = primarySpelling;
        currentInstance.ReadingsListView.ItemsSource = readings;
        currentInstance.ReadingsListView.Background = ConfigManager.PopupBackgroundColor;
        currentInstance.ReadingsListView.Foreground = ConfigManager.ReadingsColor;
        currentInstance.Background = ConfigManager.PopupBackgroundColor;
        currentInstance.Foreground = ConfigManager.DefinitionsColor;
        currentInstance.FontFamily = ConfigManager.PopupFont;
        currentInstance.Show();
        currentInstance.UpdatePosition(window.PointToScreen(Mouse.GetPosition(window)));
        WinApi.BringToFront(currentInstance._windowHandle);
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

        if (ConfigManager.Focusable)
        {
            WinApi.AllowActivation(_windowHandle);
        }
        else
        {
            WinApi.PreventActivation(_windowHandle);
        }
    }

    private async void ReadingsListView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        string selectedReading = (string)((ListViewItem)sender).Content;
        Hide();
        await PopupWindowUtils.PlayAudio(_primarySpelling!, selectedReading).ConfigureAwait(false);
    }

    private void UpdatePosition(Point cursorPosition)
    {
        double mouseX = cursorPosition.X / WindowsUtils.Dpi.DpiScaleX;
        double mouseY = cursorPosition.Y / WindowsUtils.Dpi.DpiScaleY;

        bool needsFlipX = (mouseX + ActualWidth) > (WindowsUtils.ActiveScreen.Bounds.X + WindowsUtils.DpiAwareWorkAreaWidth);
        bool needsFlipY = (mouseY + ActualHeight) > (WindowsUtils.ActiveScreen.Bounds.Y + WindowsUtils.DpiAwareWorkAreaHeight);

        double newLeft;
        double newTop;

        if (needsFlipX)
        {
            // flip Leftwards while preventing -OOB
            newLeft = mouseX - ActualWidth - 5;
            if (newLeft < WindowsUtils.ActiveScreen.Bounds.X)
            {
                newLeft = WindowsUtils.ActiveScreen.Bounds.X;
            }
        }
        else
        {
            // no flip
            newLeft = mouseX - 5;
        }

        if (needsFlipY)
        {
            // flip Upwards while preventing -OOB
            newTop = mouseY - (ActualHeight + 15);
            if (newTop < WindowsUtils.ActiveScreen.Bounds.Y)
            {
                newTop = WindowsUtils.ActiveScreen.Bounds.Y;
            }
        }
        else
        {
            // no flip
            newTop = mouseY + 15;
        }

        // stick to edges if +OOB
        if ((newLeft + ActualWidth) > (WindowsUtils.ActiveScreen.Bounds.X + WindowsUtils.DpiAwareWorkAreaWidth))
        {
            newLeft = WindowsUtils.ActiveScreen.Bounds.X + WindowsUtils.DpiAwareWorkAreaWidth - ActualWidth;
        }

        if ((newTop + ActualHeight) > (WindowsUtils.ActiveScreen.Bounds.Y + WindowsUtils.DpiAwareWorkAreaHeight))
        {
            newTop = WindowsUtils.ActiveScreen.Bounds.Y + WindowsUtils.DpiAwareWorkAreaHeight - ActualHeight;
        }

        Left = newLeft;
        Top = newTop;
    }

    public static void HideWindow()
    {
        s_instance?.Hide();
    }
}
