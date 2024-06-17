using System.Collections.Frozen;
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
using JL.Core.Utilities;
using JL.Windows.Utilities;
using Microsoft.Data.Sqlite;
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

    private nint _windowHandle;

    public static ManageDictionariesWindow Instance => s_instance ??= new ManageDictionariesWindow();

    private ManageDictionariesWindow()
    {
        InitializeComponent();
        UpdateDictionariesDisplay();
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

    public static bool IsItVisible()
    {
        return s_instance?.IsVisible ?? false;
    }

    // ReSharper disable once AsyncVoidMethod
    private async void Window_Closed(object sender, EventArgs e)
    {
        s_instance = null;

        WindowsUtils.UpdateMainWindowVisibility();
        _ = MainWindow.Instance.Focus();

        await DictUtils.LoadDictionaries().ConfigureAwait(false);
        await DictUtils.SerializeDicts().ConfigureAwait(false);

        Utils.ClearStringPoolIfDictsAreReady();
    }

    // probably should be split into several methods
    private void UpdateDictionariesDisplay()
    {
        List<DockPanel> resultDockPanels = [];

        foreach (Dict dict in DictUtils.Dicts.Values)
        {
            DockPanel dockPanel = new();

            CheckBox checkBox = new()
            {
                Width = 20,
                IsChecked = dict.Active,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Tag = dict
            };
            checkBox.Checked += CheckBox_Checked;
            checkBox.Unchecked += CheckBox_Unchecked;

            Button increasePriorityButton = new()
            {
                Width = 25,
                Content = '↑',
                Margin = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Tag = dict
            };
            increasePriorityButton.Click += IncreasePriorityButton_Click;

            Button decreasePriorityButton = new()
            {
                Width = 25,
                Content = '↓',
                Margin = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Tag = dict
            };
            decreasePriorityButton.Click += DecreasePriorityButton_Click;

            TextBlock dictNameTextBlock = new()
            {
                Width = 150,
                Text = dict.Name,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10)
            };

            string fullPath = Path.GetFullPath(dict.Path, Utils.ApplicationPath);
            bool invalidPath = !Directory.Exists(fullPath) && !File.Exists(fullPath);
            TextBlock dictPathValidityTextBlock = new()
            {
                Width = 13,
                Text = invalidPath ? "❌" : "",
                ToolTip = invalidPath ? "Invalid Path" : null,
                Foreground = Brushes.Crimson,
                Margin = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Tag = dict
            };

            TextBlock dictPathTextBlock = new()
            {
                Width = 300,
                Text = dict.Path,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10),
                Cursor = Cursors.Hand
            };
            dictPathTextBlock.PreviewMouseLeftButtonUp += PathTextBox_PreviewMouseLeftButtonUp;
            dictPathTextBlock.MouseEnter += PathTextBlock_MouseEnter;
            dictPathTextBlock.MouseLeave += PathTextBlock_MouseLeave;

            Button editButton = new()
            {
                Name = nameof(editButton),
                Width = 45,
                Height = 30,
                Content = "Edit",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                Background = Brushes.DodgerBlue,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 5, 0),
                Tag = dict
            };
            editButton.Click += EditButton_Click;

            Button updateButton = new()
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
                    : Visibility.Visible,
                Tag = dict
            };
            updateButton.Click += UpdateButton_Click;

            Button removeButton = new()
            {
                Width = 75,
                Height = 30,
                Content = "Remove",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                Background = Brushes.Red,
                BorderThickness = new Thickness(1),
                Visibility = DictUtils.BuiltInDicts.Values.Any(d => d.Type == dict.Type)
                    ? Visibility.Collapsed
                    : Visibility.Visible,
                Tag = dict
            };
            removeButton.Click += RemoveButton_Click;

            Button infoButton = new()
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

            if (dict.Type is DictType.JMdict)
            {
                updateButton.IsEnabled = !DictUtils.UpdatingJmdict;
                editButton.IsEnabled = !DictUtils.UpdatingJmdict;
                infoButton.Click += JmdictInfoButton_Click;
            }
            else if (dict.Type is DictType.JMnedict)
            {
                updateButton.IsEnabled = !DictUtils.UpdatingJmnedict;
                editButton.IsEnabled = !DictUtils.UpdatingJmnedict;
                infoButton.Click += JmnedictInfoButton_Click;
            }
            else if (dict.Type is DictType.Kanjidic)
            {
                updateButton.IsEnabled = !DictUtils.UpdatingKanjidic;
                editButton.IsEnabled = !DictUtils.UpdatingKanjidic;
            }

            _ = dockPanel.Children.Add(checkBox);
            _ = dockPanel.Children.Add(increasePriorityButton);
            _ = dockPanel.Children.Add(decreasePriorityButton);
            _ = dockPanel.Children.Add(dictNameTextBlock);
            _ = dockPanel.Children.Add(dictPathValidityTextBlock);
            _ = dockPanel.Children.Add(dictPathTextBlock);
            _ = dockPanel.Children.Add(editButton);
            _ = dockPanel.Children.Add(updateButton);
            _ = dockPanel.Children.Add(removeButton);
            _ = dockPanel.Children.Add(infoButton);

            resultDockPanels.Add(dockPanel);
        }

        DictionariesDisplay.ItemsSource = resultDockPanels
            .OrderBy(static dockPanel => ((Dict)((CheckBox)dockPanel.Children[0]).Tag).Priority);
    }

    private void PathTextBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        string? fullPath = Path.GetFullPath(((TextBlock)sender).Text, Utils.ApplicationPath);
        if (Path.Exists(fullPath))
        {
            if (File.Exists(fullPath))
            {
                fullPath = Path.GetDirectoryName(fullPath) ?? Utils.ApplicationPath;
            }

            if (fullPath is not null)
            {
                _ = Process.Start("explorer.exe", fullPath);
            }
        }
    }

    private static void PathTextBlock_MouseEnter(object sender, MouseEventArgs e)
    {
        ((TextBlock)sender).TextDecorations = TextDecorations.Underline;
    }

    private static void PathTextBlock_MouseLeave(object sender, MouseEventArgs e)
    {
        ((TextBlock)sender).TextDecorations = null;
    }
    private static void CheckBox_Checked(object sender, RoutedEventArgs e)
    {
        Dict dict = (Dict)((CheckBox)sender).Tag;
        dict.Active = true;
    }

    private static void CheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        Dict dict = (Dict)((CheckBox)sender).Tag;
        dict.Active = false;
    }

    private void IncreasePriorityButton_Click(object sender, RoutedEventArgs e)
    {
        Dict dict = (Dict)((Button)sender).Tag;
        PrioritizeDict(dict);
        UpdateDictionariesDisplay();
    }

    private void DecreasePriorityButton_Click(object sender, RoutedEventArgs e)
    {
        Dict dict = (Dict)((Button)sender).Tag;
        DeprioritizeDict(dict);
        UpdateDictionariesDisplay();
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!WindowsUtils.ShowYesNoDialog("Do you really want to remove this dictionary?", "Confirmation"))
        {
            return;
        }

        Dict dict = (Dict)((Button)sender).Tag;
        dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
        _ = DictUtils.Dicts.Remove(dict.Name);

        string dbPath = DBUtils.GetDictDBPath(dict.Name);
        if (File.Exists(dbPath))
        {
            DBUtils.SendOptimizePragmaToAllDBs();
            SqliteConnection.ClearAllPools();
            File.Delete(dbPath);
        }

        if (dict.Type is DictType.PitchAccentYomichan)
        {
            _ = DictUtils.SingleDictTypeDicts.Remove(DictType.PitchAccentYomichan);
        }

        int priorityOfDeletedDict = dict.Priority;

        foreach (Dict d in DictUtils.Dicts.Values)
        {
            if (d.Priority > priorityOfDeletedDict)
            {
                d.Priority -= 1;
            }
        }

        UpdateDictionariesDisplay();
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        Dict dict = (Dict)((Button)sender).Tag;
        _ = new EditDictionaryWindow(dict)
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        }.ShowDialog();

        UpdateDictionariesDisplay();
    }

    // ReSharper disable once AsyncVoidMethod
    private async void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        Button updateButton = (Button)sender;
        updateButton.IsEnabled = false;

        Button editButton = ((DockPanel)updateButton.Parent).GetChildByName<Button>("editButton")!;
        editButton.IsEnabled = false;

        Dict dict = (Dict)updateButton.Tag;
        if (dict.Type is DictType.JMdict)
        {
            await DictUpdater.UpdateJmdict(true, false).ConfigureAwait(true);
        }
        else if (dict.Type is DictType.JMnedict)
        {
            await DictUpdater.UpdateJmnedict(true, false).ConfigureAwait(true);
        }
        else if (dict.Type is DictType.Kanjidic)
        {
            await DictUpdater.UpdateKanjidic(true, false).ConfigureAwait(true);
        }

        UpdateDictionariesDisplay();
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
        _ = new AddDictionaryWindow
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        }.ShowDialog();

        UpdateDictionariesDisplay();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private static string EntityDictToString(Dictionary<string, string> entityDict)
    {
        if (entityDict.Count is 0)
        {
            return "";
        }

        StringBuilder sb = new();
        IOrderedEnumerable<KeyValuePair<string, string>> sortedJmdictEntities = entityDict.OrderBy(static e => e.Key, StringComparer.InvariantCulture);
        foreach (KeyValuePair<string, string> entity in sortedJmdictEntities)
        {
            _ = sb.Append(CultureInfo.InvariantCulture, $"{entity.Key}: {entity.Value}\n");
        }

        return sb.ToString(0, sb.Length - 1);
    }

    private void ShowInfoWindow(Dictionary<string, string> entityDict, string title)
    {
        InfoWindow infoWindow = new()
        {
            Owner = this,
            Title = title,
            InfoTextBox =
            {
                Text = EntityDictToString(entityDict)
            },
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
}
