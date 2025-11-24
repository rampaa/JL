using System.Globalization;
using System.Windows.Controls;
using System.Windows.Interop;
using JL.Core.Utilities;
using JL.Windows.Config;
using JL.Windows.Interop;

namespace JL.Windows.GUI.Info;

/// <summary>
/// Interaction logic for AbbreviationWindow.xaml
/// </summary>
internal sealed partial class InfoDataGridWindow
{
    private nint _windowHandle;

    public InfoDataGridWindow()
    {
        InitializeComponent();
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

    private void Window_Closed(object sender, EventArgs e)
    {
        InfoDataGrid.ItemsSource = null;
    }

    private void InfoDataGridSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        InfoDataGrid.Items.Filter = InfoDataGridFilter;
    }

    private bool InfoDataGridFilter(object item)
    {
        (string term, int count) = (KeyValuePair<string, int>)item;
        string termInHiragana = JapaneseUtils.NormalizeText(term);
        string textInHiragana = JapaneseUtils.NormalizeText(InfoDataGridSearchTextBox.Text);

        return termInHiragana.AsSpan().Contains(textInHiragana, StringComparison.Ordinal)
            || count.ToString(CultureInfo.InvariantCulture).AsSpan().Contains(textInHiragana, StringComparison.Ordinal);
    }
}
