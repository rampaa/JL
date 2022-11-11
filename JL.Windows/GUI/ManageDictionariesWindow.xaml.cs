using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using JL.Core;
using JL.Core.Dicts;
using JL.Core.Dicts.EDICT;
using JL.Core.Dicts.EDICT.JMdict;
using JL.Core.Dicts.EDICT.JMnedict;
using JL.Core.Dicts.EDICT.KANJIDIC;
using JL.Core.Utilities;
using JL.Core.WordClass;
using JL.Windows.Utilities;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using Cursors = System.Windows.Input.Cursors;
using MessageBoxOptions = System.Windows.MessageBoxOptions;
using Path = System.IO.Path;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for ManageDictionariesWindow.xaml
/// </summary>
public partial class ManageDictionariesWindow : Window
{
    private static ManageDictionariesWindow? s_instance;
    private InfoWindow? _jmdictAbbreviationWindow = null;
    private InfoWindow? _jmnedictAbbreviationWindow = null;

    public static ManageDictionariesWindow Instance
    {
        get { return s_instance ??= new(); }
    }

    public ManageDictionariesWindow()
    {
        InitializeComponent();
        UpdateDictionariesDisplay();
    }

    public static bool IsItVisible()
    {
        return s_instance?.IsVisible ?? false;
    }

    private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        WindowsUtils.HideWindow(this);
        Storage.Frontend.InvalidateDisplayCache();
        await Utils.SerializeDicts().ConfigureAwait(false);
        await Storage.LoadDictionaries().ConfigureAwait(false);
    }

    // probably should be split into several methods
    private void UpdateDictionariesDisplay()
    {
        List<DockPanel> resultDockPanels = new();

        foreach (Dict dict in Storage.Dicts.Values.ToList())
        {
            DockPanel dockPanel = new();

            var checkBox = new CheckBox { Width = 20, IsChecked = dict.Active, Margin = new Thickness(10), };
            var buttonIncreasePriority = new Button { Width = 25, Content = "↑", Margin = new Thickness(1), };
            var buttonDecreasePriority = new Button { Width = 25, Content = "↓", Margin = new Thickness(1), };
            var priority = new TextBlock
            {
                Name = "priority",
                // Width = 20,
                Width = 0,
                Text = dict.Priority.ToString(),
                Visibility = Visibility.Collapsed,
                // Margin = new Thickness(10),
            };
            var dictTypeDisplay = new TextBlock
            {
                Width = 177,
                Text = dict.Name,
                Margin = new Thickness(10),
            };
            var dictPathValidityDisplay = new TextBlock
            {
                Width = 13,
                Text = "❌",
                ToolTip = "Invalid Path",
                Foreground = Brushes.Crimson,
                Margin = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Visibility = !Directory.Exists(dict.Path) && !File.Exists(dict.Path)
                    ? Visibility.Visible
                    : Visibility.Collapsed
            };
            var dictPathDisplay = new TextBlock
            {
                Width = 200,
                Text = dict.Path,
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
                Content = (Directory.Exists(dict.Path) || File.Exists(dict.Path)) ? "Update" : "Download",
                Foreground = Brushes.White,
                Background = Brushes.DarkGreen,
                BorderThickness = new Thickness(1),
                Visibility = (dict.Type != DictType.JMdict
                              && dict.Type != DictType.JMnedict
                              && dict.Type != DictType.Kanjidic)
                    ? Visibility.Collapsed
                    : Visibility.Visible,
            };

            switch (dict.Type)
            {
                case DictType.JMdict:
                    buttonUpdate.IsEnabled = !Storage.UpdatingJMdict;
                    break;

                case DictType.JMnedict:
                    buttonUpdate.IsEnabled = !Storage.UpdatingJMnedict;
                    break;

                case DictType.Kanjidic:
                    buttonUpdate.IsEnabled = !Storage.UpdatingKanjidic;
                    break;
            }

            buttonUpdate.Click += async (_, _) =>
            {
                buttonUpdate.IsEnabled = false;

                switch (dict.Type)
                {
                    case DictType.JMdict:
                        await UpdateJMdict();
                        break;
                    case DictType.JMnedict:
                        await UpdateJMnedict();
                        break;
                    case DictType.Kanjidic:
                        await UpdateKanjidic();
                        break;
                }

                UpdateDictionariesDisplay();
            };

            var buttonRemove = new Button
            {
                Width = 75,
                Height = 30,
                Content = "Remove",
                Foreground = Brushes.White,
                Background = Brushes.Red,
                BorderThickness = new Thickness(1),
                Visibility = Storage.BuiltInDicts.Values
                    .Select(d => d.Type).Contains(dict.Type)
                    ? Visibility.Collapsed
                    : Visibility.Visible,
            };

            var buttonEdit = new Button
            {
                Width = 45,
                Height = 30,
                Content = "Edit",
                Foreground = Brushes.White,
                Background = Brushes.DodgerBlue,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 5, 0),
                // Visibility = Storage.BuiltInDicts.Values
                //     .Select(t => t.Type).ToList().Contains(dict.Type)
                //     ? Visibility.Collapsed
                //     : Visibility.Visible,
            };

            var buttonInfo = new Button
            {
                Width = 50,
                Height = 30,
                Content = "Info",
                Foreground = Brushes.White,
                Background = Brushes.LightSlateGray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 5, 0),
                Visibility = dict.Type is DictType.JMdict or DictType.JMnedict
                    ? Visibility.Visible
                    : Visibility.Collapsed
            };

            if (dict.Type == DictType.JMdict)
            {
                buttonInfo.IsEnabled = Storage.JmdictEntities.Any();
                buttonInfo.Click += JmdictInfoButton_Click;
            }

            else if (dict.Type == DictType.JMnedict)
            {
                buttonInfo.Click += JmnedictInfoButton_Click;
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
                UnPrioritizeDict(dict);
                UpdateDictionariesDisplay();
            };
            buttonRemove.Click += (_, _) =>
            {
                if (MessageBox.Show("Really remove dictionary?", "Confirmation",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question,
                        MessageBoxResult.No,
                        MessageBoxOptions.DefaultDesktopOnly) == MessageBoxResult.Yes)
                {
                    dict.Contents.Clear();
                    Storage.Dicts.Remove(dict.Name);
                    UpdateDictionariesDisplay();

                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);
                }
            };
            buttonEdit.Click += (_, _) =>
            {
                new EditDictionaryWindow(dict).ShowDialog();
                UpdateDictionariesDisplay();
            };

            resultDockPanels.Add(dockPanel);

            dockPanel.Children.Add(checkBox);
            dockPanel.Children.Add(buttonIncreasePriority);
            dockPanel.Children.Add(buttonDecreasePriority);
            dockPanel.Children.Add(priority);
            dockPanel.Children.Add(dictTypeDisplay);
            dockPanel.Children.Add(dictPathValidityDisplay);
            dockPanel.Children.Add(dictPathDisplay);
            dockPanel.Children.Add(buttonEdit);
            dockPanel.Children.Add(buttonUpdate);
            dockPanel.Children.Add(buttonRemove);
            dockPanel.Children.Add(buttonInfo);
        }

        DictionariesDisplay!.ItemsSource = resultDockPanels.OrderBy(dockPanel =>
            dockPanel.Children
                .OfType<TextBlock>()
                .Where(textBlock => textBlock.Name == "priority")
                .Select(textBlockPriority => Convert.ToInt32(textBlockPriority.Text)).First());
    }

    private void PathTextbox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        string path = ((TextBlock)sender).Text;

        if (File.Exists(path) || Directory.Exists(path))
        {
            if (File.Exists(path))
                path = Path.GetDirectoryName(path)!;

            Process.Start("explorer.exe", path ?? throw new InvalidOperationException());
        }
    }

    private static void PrioritizeDict(Dict dict)
    {
        if (dict.Priority == 0) return;

        Storage.Dicts.Single(d => d.Value.Priority == dict.Priority - 1).Value.Priority += 1;
        dict.Priority -= 1;
    }

    private static void UnPrioritizeDict(Dict dict)
    {
        // lowest priority means highest number
        int lowestPriority = Storage.Dicts.Select(d => d.Value.Priority).Max();
        if (dict.Priority == lowestPriority) return;

        Storage.Dicts.Single(d => d.Value.Priority == dict.Priority + 1).Value.Priority -= 1;
        dict.Priority += 1;
    }

    private void ButtonAddDictionary_OnClick(object sender, RoutedEventArgs e)
    {
        new AddDictionaryWindow().ShowDialog();
        UpdateDictionariesDisplay();
    }

    //todo move to core
    private static async Task UpdateJMdict()
    {
        Storage.UpdatingJMdict = true;

        Dict dict = Storage.Dicts.Values.First(dict => dict.Type == DictType.JMdict);
        bool isDownloaded = await ResourceUpdater.UpdateResource(dict.Path,
                Storage.JmdictUrl,
                DictType.JMdict.ToString(), true, false)
            .ConfigureAwait(false);

        if (isDownloaded)
        {
            dict.Contents.Clear();

            await Task.Run(async () => await JmdictLoader
                .Load(dict).ConfigureAwait(false));

            await JmdictWordClassLoader.JmdictWordClassSerializer().ConfigureAwait(false);

            Storage.WordClassDictionary.Clear();

            await JmdictWordClassLoader.Load().ConfigureAwait(false);

            if (!dict.Active)
                dict.Contents.Clear();

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);
        }

        Storage.UpdatingJMdict = false;
    }

    private static async Task UpdateJMnedict()
    {
        Storage.UpdatingJMnedict = true;

        Dict dict = Storage.Dicts.Values.First(dict => dict.Type == DictType.JMnedict);
        bool isDownloaded = await ResourceUpdater.UpdateResource(dict.Path,
                Storage.JmnedictUrl,
                DictType.JMnedict.ToString(), true, false)
            .ConfigureAwait(false);

        if (isDownloaded)
        {
            dict.Contents.Clear();

            await Task.Run(async () => await JmnedictLoader
                .Load(dict).ConfigureAwait(false));

            if (!dict.Active)
                dict.Contents.Clear();

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);
        }

        Storage.UpdatingJMnedict = false;
    }

    private static async Task UpdateKanjidic()
    {
        Storage.UpdatingKanjidic = true;
        Dict dict = Storage.Dicts.Values.First(dict => dict.Type == DictType.Kanjidic);
        bool isDownloaded = await ResourceUpdater.UpdateResource(dict.Path,
                Storage.KanjidicUrl,
                DictType.Kanjidic.ToString(), true, false)
            .ConfigureAwait(false);

        if (isDownloaded)
        {
            dict.Contents.Clear();

            await Task.Run(async () => await KanjidicLoader
                .Load(dict).ConfigureAwait(false));

            if (!dict.Active)
                dict.Contents.Clear();

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);
        }

        Storage.UpdatingKanjidic = false;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private static string EntityDictToString(Dictionary<string, string> entityDict)
    {
        StringBuilder sb = new();
        IOrderedEnumerable<KeyValuePair<string, string>> sortedJmdictEntities = entityDict.OrderBy(e => e.Key);

        foreach (KeyValuePair<string, string> entity in sortedJmdictEntities)
        {
            sb.Append(entity.Key);
            sb.Append(": ");
            sb.Append(entity.Value);
            sb.Append(Environment.NewLine);
        }

        return sb.ToString()[..^Environment.NewLine.Length];
    }

    private static void ShowInfoWindow(ref InfoWindow? infoWindow, Dictionary<string, string> entityDict, string title)
    {
        if (infoWindow == null)
        {
            infoWindow = new()
            {
                Title = title,
                InfoTextBox = { Text = EntityDictToString(entityDict) }
            };
        }

        else
        {
            infoWindow.InfoTextBox.ScrollToHome();
        }

        infoWindow.ShowDialog();
    }

    private void JmdictInfoButton_Click(object sender, RoutedEventArgs e)
    {
        ShowInfoWindow(ref _jmdictAbbreviationWindow, Storage.JmdictEntities, "JMdict Abbreviations");
    }

    private void JmnedictInfoButton_Click(object sender, RoutedEventArgs e)
    {
        ShowInfoWindow(ref _jmnedictAbbreviationWindow, Storage.JmnedictEntities, "JMnedict Abbreviations");
    }

    private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            UpdateDictionariesDisplay();
        }
    }
}
