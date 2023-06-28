using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using JL.Core.Dicts;
using JL.Core.Dicts.EDICT;
using JL.Core.Utilities;
using JL.Windows.Utilities;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using Cursors = System.Windows.Input.Cursors;
namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for ManageDictionariesWindow.xaml
/// </summary>
internal sealed partial class ManageDictionariesWindow : Window
{
    private static ManageDictionariesWindow? s_instance;

    private IntPtr _windowHandle;

    public static ManageDictionariesWindow Instance => s_instance ??= new ManageDictionariesWindow();

    public ManageDictionariesWindow()
    {
        InitializeComponent();
        UpdateDictionariesDisplay();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _windowHandle = new WindowInteropHelper(this).Handle;
        WinApi.BringToFront(_windowHandle);
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

    public static bool IsItVisible()
    {
        return s_instance?.IsVisible ?? false;
    }

    private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        Utils.Frontend.InvalidateDisplayCache();
        WindowsUtils.UpdateMainWindowVisibility();
        _ = MainWindow.Instance.Focus();
        s_instance = null;
        await DictUtils.SerializeDicts().ConfigureAwait(false);
        await DictUtils.LoadDictionaries().ConfigureAwait(false);
    }

    // probably should be split into several methods
    private void UpdateDictionariesDisplay()
    {
        List<DockPanel> resultDockPanels = new();

        foreach (Dict dict in DictUtils.Dicts.Values.ToList())
        {
            DockPanel dockPanel = new();

            var checkBox = new CheckBox
            {
                Width = 20,
                IsChecked = dict.Active,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };

            var buttonIncreasePriority = new Button
            {
                Width = 25,
                Content = '↑',
                Margin = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };

            var buttonDecreasePriority = new Button
            {
                Width = 25,
                Content = '↓',
                Margin = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };

            var priority = new TextBlock
            {
                Name = "priority",
                // Width = 20,
                Width = 0,
                Text = dict.Priority.ToString(CultureInfo.InvariantCulture),
                Visibility = Visibility.Collapsed
                // Margin = new Thickness(10),
            };
            var dictTypeDisplay = new TextBlock
            {
                Width = 150,
                Text = dict.Name,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10)
            };

            bool invalidPath = !Directory.Exists(dict.Path) && !File.Exists(dict.Path);
            var dictPathValidityDisplay = new TextBlock
            {
                Width = 13,
                Text = invalidPath ? "❌" : "",
                ToolTip = invalidPath ? "Invalid Path" : null,
                Foreground = Brushes.Crimson,
                Margin = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var dictPathDisplay = new TextBlock
            {
                Width = 300,
                Text = dict.Path,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10),
                Cursor = Cursors.Hand
            };

            dictPathDisplay.PreviewMouseLeftButtonUp += PathTextbox_PreviewMouseLeftButtonUp;

            dictPathDisplay.MouseEnter += (_, _) => dictPathDisplay.TextDecorations = TextDecorations.Underline;

            dictPathDisplay.MouseLeave += (_, _) => dictPathDisplay.TextDecorations = null;

            var buttonUpdate = new Button
            {
                Width = 75,
                Height = 30,
                Content = invalidPath ? "Download" : "Update",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                Background = Brushes.DarkGreen,
                BorderThickness = new Thickness(1),
                Visibility = dict.Type is not DictType.JMdict
                    and not DictType.JMnedict
                    and not DictType.Kanjidic
                    ? Visibility.Collapsed
                    : Visibility.Visible
            };

            switch (dict.Type)
            {
                case DictType.JMdict:
                    buttonUpdate.IsEnabled = !DictUtils.UpdatingJmdict;
                    break;

                case DictType.JMnedict:
                    buttonUpdate.IsEnabled = !DictUtils.UpdatingJmnedict;
                    break;

                case DictType.Kanjidic:
                    buttonUpdate.IsEnabled = !DictUtils.UpdatingKanjidic;
                    break;
            }

            buttonUpdate.Click += async (_, _) =>
            {
                buttonUpdate.IsEnabled = false;

                switch (dict.Type)
                {
                    case DictType.JMdict:
                        await ResourceUpdater.UpdateJmdict().ConfigureAwait(true);
                        break;
                    case DictType.JMnedict:
                        await ResourceUpdater.UpdateJmnedict().ConfigureAwait(true);
                        break;
                    case DictType.Kanjidic:
                        await ResourceUpdater.UpdateKanjidic().ConfigureAwait(true);
                        break;
                }

                UpdateDictionariesDisplay();
            };

            var buttonRemove = new Button
            {
                Width = 75,
                Height = 30,
                Content = "Remove",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                Background = Brushes.Red,
                BorderThickness = new Thickness(1),
                Visibility = DictUtils.BuiltInDicts.Values
                    .Select(static d => d.Type).Contains(dict.Type)
                    ? Visibility.Collapsed
                    : Visibility.Visible
            };

            var buttonEdit = new Button
            {
                Width = 45,
                Height = 30,
                Content = "Edit",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                Background = Brushes.DodgerBlue,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 5, 0)
                // Visibility = DictUtils.BuiltInDicts.Values
                //     .Select(t => t.Type).ToList().Contains(dict.Type)
                //     ? Visibility.Collapsed
                //     : Visibility.Visible,
            };

            var buttonInfo = new Button
            {
                Width = 50,
                Height = 30,
                Content = "Info",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                Background = Brushes.LightSlateGray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(5, 0, 0, 0),
                Visibility = dict.Type is DictType.JMdict or DictType.JMnedict
                    ? Visibility.Visible
                    : Visibility.Collapsed
            };

            switch (dict.Type)
            {
                case DictType.JMdict:
                    buttonInfo.Click += JmdictInfoButton_Click;
                    break;
                case DictType.JMnedict:
                    buttonInfo.Click += JmnedictInfoButton_Click;
                    break;
            }

            checkBox.Unchecked += (_, _) => dict.Active = false;

            checkBox.Checked += (_, _) => dict.Active = true;

            buttonIncreasePriority.Click += (_, _) =>
            {
                PrioritizeDict(dict);
                UpdateDictionariesDisplay();
            };
            buttonDecreasePriority.Click += (_, _) =>
            {
                DeprioritizeDict(dict);
                UpdateDictionariesDisplay();
            };
            buttonRemove.Click += (_, _) =>
            {
                if (Utils.Frontend.ShowYesNoDialog("Do you really want to remove this dictionary?", "Confirmation"))
                {
                    dict.Contents.Clear();
                    _ = DictUtils.Dicts.Remove(dict.Name);

                    int priorityOfDeletedDict = dict.Priority;

                    foreach (Dict d in DictUtils.Dicts.Values)
                    {
                        if (d.Priority > priorityOfDeletedDict)
                        {
                            d.Priority -= 1;
                        }
                    }

                    UpdateDictionariesDisplay();

                    //GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);
                }
            };
            buttonEdit.Click += (_, _) =>
            {
                _ = new EditDictionaryWindow(dict) { Owner = this }.ShowDialog();
                UpdateDictionariesDisplay();
            };

            resultDockPanels.Add(dockPanel);

            _ = dockPanel.Children.Add(checkBox);
            _ = dockPanel.Children.Add(buttonIncreasePriority);
            _ = dockPanel.Children.Add(buttonDecreasePriority);
            _ = dockPanel.Children.Add(priority);
            _ = dockPanel.Children.Add(dictTypeDisplay);
            _ = dockPanel.Children.Add(dictPathValidityDisplay);
            _ = dockPanel.Children.Add(dictPathDisplay);
            _ = dockPanel.Children.Add(buttonEdit);
            _ = dockPanel.Children.Add(buttonUpdate);
            _ = dockPanel.Children.Add(buttonRemove);
            _ = dockPanel.Children.Add(buttonInfo);
        }

        DictionariesDisplay.ItemsSource = resultDockPanels.OrderBy(static dockPanel =>
            dockPanel.Children
                .OfType<TextBlock>()
                .Where(static textBlock => textBlock.Name is "priority")
                .Select(static textBlockPriority => Convert.ToInt32(textBlockPriority.Text, CultureInfo.InvariantCulture)).First());
    }

    private void PathTextbox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        string? path = ((TextBlock)sender).Text;

        if (File.Exists(path) || Directory.Exists(path))
        {
            if (File.Exists(path))
            {
                path = Path.GetDirectoryName(path) ?? Utils.ApplicationPath;
            }

            _ = Process.Start("explorer.exe", path);
        }
    }

    private static void PrioritizeDict(Dict dict)
    {
        if (dict.Priority is 1)
        {
            return;
        }

        DictUtils.Dicts.First(d => d.Value.Priority == dict.Priority - 1).Value.Priority += 1;
        dict.Priority -= 1;
    }

    private static void DeprioritizeDict(Dict dict)
    {
        if (dict.Priority == DictUtils.Dicts.Count)
        {
            return;
        }

        DictUtils.Dicts.First(d => d.Value.Priority == dict.Priority + 1).Value.Priority -= 1;
        dict.Priority += 1;
    }

    private void ButtonAddDictionary_OnClick(object sender, RoutedEventArgs e)
    {
        _ = new AddDictionaryWindow { Owner = this }.ShowDialog();
        UpdateDictionariesDisplay();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private static string EntityDictToString(Dictionary<string, string> entityDict)
    {
        StringBuilder sb = new();

        IOrderedEnumerable<KeyValuePair<string, string>> sortedJmdictEntities = entityDict.OrderBy(static e => e.Key);
        foreach (KeyValuePair<string, string> entity in sortedJmdictEntities)
        {
            _ = sb.Append(CultureInfo.InvariantCulture, $"{entity.Key}: {entity.Value}\n");
        }

        return sb.Length > 0
            ? sb.Remove(sb.Length - 1, 1).ToString()
            : "";
    }

    private void ShowInfoWindow(Dictionary<string, string> entityDict, string title)
    {
        InfoWindow infoWindow = new()
        {
            Owner = this,
            Title = title,
            InfoTextBox = { Text = EntityDictToString(entityDict) },
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        _ = infoWindow.ShowDialog();
    }

    private void JmdictInfoButton_Click(object sender, RoutedEventArgs e)
    {
        ShowInfoWindow(DictUtils.JmdictEntities, "JMdict Abbreviations");
    }

    private void JmnedictInfoButton_Click(object sender, RoutedEventArgs e)
    {
        ShowInfoWindow(DictUtils.JmnedictEntities, "JMnedict Abbreviations");
    }

    private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            UpdateDictionariesDisplay();
        }
    }
}
