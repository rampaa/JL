using System.Globalization;
using System.Windows.Controls;
using System.Windows.Interop;
using JL.Core.Japanese;
using JL.Windows.Config;
using JL.Windows.Interop;

namespace JL.Windows.GUI.Info;

/// <summary>
/// Interaction logic for AbbreviationWindow.xaml
/// </summary>
internal sealed partial class InfoDataGridWindow
{
    private nint _windowHandle;
    private string _searchTextInHiragana = "";

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
        _searchTextInHiragana = JapaneseUtils.NormalizeText(InfoDataGridSearchTextBox.Text);
        InfoDataGrid.Items.Filter = InfoDataGridFilter;
    }

    private bool InfoDataGridFilter(object item)
    {
        if (_searchTextInHiragana.Length is 0)
        {
            return true;
        }

        (string term, int count) = (KeyValuePair<string, int>)item;
        string termInHiragana = JapaneseUtils.NormalizeText(term);
        return termInHiragana.AsSpan().Contains(_searchTextInHiragana, StringComparison.Ordinal)
            || count.ToString(CultureInfo.InvariantCulture).AsSpan().Contains(_searchTextInHiragana, StringComparison.Ordinal);
    }
}
