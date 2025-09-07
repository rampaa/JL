using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using JL.Windows.Config;
using JL.Windows.Interop;

namespace JL.Windows.GUI.Info;

/// <summary>
/// Interaction logic for AbbreviationWindow.xaml
/// </summary>
internal sealed partial class InfoWindow
{
    private nint _windowHandle;

    public InfoWindow(string[] items)
    {
        InitializeComponent();
        InfoTextBox.Visibility = Visibility.Collapsed;
        InfoListBox.ItemsSource = items;
    }

    public InfoWindow(string text)
    {
        InitializeComponent();
        InfoSearchTextBox.Visibility = Visibility.Collapsed;
        InfoListBox.Visibility = Visibility.Collapsed;
        InfoTextBox.Text = text;
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

    private void InfoSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        InfoListBox.Items.Filter = InfoFilter;
    }

    private bool InfoFilter(object item)
    {
        string preferenceName = (string)item;
        return preferenceName.AsSpan().Contains(InfoSearchTextBox.Text, StringComparison.OrdinalIgnoreCase);
    }
}
