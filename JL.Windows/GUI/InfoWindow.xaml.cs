using System.Windows;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for AbbreviationWindow.xaml
/// </summary>
public partial class InfoWindow : Window
{
    public InfoWindow()
    {
        InitializeComponent();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
