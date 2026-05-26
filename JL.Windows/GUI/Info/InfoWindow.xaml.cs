using System.Windows;
using System.Windows.Controls;

namespace JL.Windows.GUI.Info;

/// <summary>
/// Interaction logic for AbbreviationWindow.xaml
/// </summary>
internal sealed partial class InfoWindow
{
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
