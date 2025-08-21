using System.Collections.Frozen;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using JL.Core.Dicts;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;
using JL.Windows.Utilities;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for ManageDictionariesWindow.xaml
/// </summary>
internal sealed partial class ManageDictionariesWindow
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

        if (ConfigManager.Instance.Focusable)
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

        await Task.Run(static async () =>
        {
            await DictUtils.SerializeDicts().ConfigureAwait(false);
            await DictUtils.LoadDictionaries().ConfigureAwait(false);
            await DictUtils.SerializeDicts().ConfigureAwait(false);

            Utils.ClearStringPoolIfDictsAreReady();
        }).ConfigureAwait(false);
    }

    private void UpdateDictionariesDisplay()
    {
        Dict[] sortedDicts = DictUtils.Dicts.Values.OrderBy(static d => d.Priority).ToArray();
        DockPanel[] resultDockPanels = new DockPanel[sortedDicts.Length];
        for (int i = 0; i < sortedDicts.Length; i++)
        {
            Dict dict = sortedDicts[i];

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
                Width = 180,
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
                IsEnabled = !dict.Updating,
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
                Margin = new Thickness(0, 0, 5, 0),
                Visibility = dict.AutoUpdatable ? Visibility.Visible : Visibility.Collapsed,
                IsEnabled = dict.Ready && !dict.Updating,
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
                Visibility = dict.Type is DictType.JMdict or DictType.JMnedict
                    ? Visibility.Visible
                    : Visibility.Collapsed
            };

            if (dict.Type is DictType.JMdict)
            {
                infoButton.Click += JmdictInfoButton_Click;
            }
            else if (dict.Type is DictType.JMnedict)
            {
                infoButton.Click += JmnedictInfoButton_Click;
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

            resultDockPanels[i] = dockPanel;
        }

        DictionariesDisplay.ItemsSource = resultDockPanels;
    }

    private static void PathTextBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        string? fullPath = Path.GetFullPath(((TextBlock)sender).Text, Utils.ApplicationPath);
        if (Path.Exists(fullPath))
        {
            if (File.Exists(fullPath))
            {
                fullPath = Path.GetDirectoryName(fullPath);
            }

            if (string.IsNullOrEmpty(fullPath))
            {
                fullPath = Utils.ApplicationPath;
            }

            using Process process = Process.Start("explorer.exe", fullPath);
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
        if (Keyboard.Modifiers is ModifierKeys.Control)
        {
            PrioritizeDictToMax(dict);
        }
        else
        {
            PrioritizeDict(dict);
        }

        UpdateDictionariesDisplay();
    }

    private void DecreasePriorityButton_Click(object sender, RoutedEventArgs e)
    {
        Dict dict = (Dict)((Button)sender).Tag;
        if (Keyboard.Modifiers is ModifierKeys.Control)
        {
            DeprioritizeToMin(dict);
        }
        else
        {
            DeprioritizeDict(dict);
        }

        UpdateDictionariesDisplay();
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!WindowsUtils.ShowYesNoDialog("Do you really want to remove this dictionary?", "Confirmation", this))
        {
            return;
        }

        Dict dict = (Dict)((Button)sender).Tag;
        dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
        _ = DictUtils.Dicts.Remove(dict.Name);

        string dbPath = DBUtils.GetDictDBPath(dict.Name);
        if (File.Exists(dbPath))
        {
            DBUtils.DeleteDB(dbPath);
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

        Button? editButton = ((DockPanel)updateButton.Parent).GetChildByName<Button>("editButton");
        Debug.Assert(editButton is not null);

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
        else if (DictUtils.YomichanDictTypes.Contains(dict.Type))
        {
            await DictUpdater.UpdateYomichanDict(dict, true, false).ConfigureAwait(true);
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

    private static void PrioritizeDictToMax(Dict dict)
    {
        if (dict.Priority is 1)
        {
            return;
        }

        foreach (Dict otherDict in DictUtils.Dicts.Values)
        {
            if (otherDict.Priority < dict.Priority)
            {
                otherDict.Priority += 1;
            }
        }

        dict.Priority = 1;
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

    private static void DeprioritizeToMin(Dict dict)
    {
        if (dict.Priority == DictUtils.Dicts.Count)
        {
            return;
        }

        foreach (Dict otherDict in DictUtils.Dicts.Values)
        {
            if (otherDict.Priority > dict.Priority)
            {
                otherDict.Priority -= 1;
            }
        }

        dict.Priority = DictUtils.Dicts.Count;
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

    private static string[] EntityDictToArray(Dictionary<string, string> entityDict)
    {
        if (entityDict.Count is 0)
        {
            return [""];
        }

        IOrderedEnumerable<KeyValuePair<string, string>> sortedJmdictEntities = entityDict.OrderBy(static e => e.Key, StringComparer.InvariantCulture);
        string[] itemArray = new string[entityDict.Count];
        int index = 0;
        foreach ((string name, string description) in sortedJmdictEntities)
        {
            itemArray[index] = $"{name}: {description}";
            ++index;
        }

        return itemArray;
    }

    private void ShowInfoWindow(Dictionary<string, string> entityDict, string title)
    {
        InfoWindow infoWindow = new(EntityDictToArray(entityDict))
        {
            Owner = this,
            Title = title,
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
