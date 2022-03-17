using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Threading.Tasks;
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
using JL.Core.PoS;
using JL.Core.Utilities;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using Cursors = System.Windows.Input.Cursors;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MessageBoxOptions = System.Windows.MessageBoxOptions;
using Path = System.IO.Path;

namespace JL.Windows.GUI
{
    /// <summary>
    /// Interaction logic for ManageDictionariesWindow.xaml
    /// </summary>
    public partial class ManageDictionariesWindow : Window
    {
        private static ManageDictionariesWindow s_instance;

        public static ManageDictionariesWindow Instance
        {
            get
            {
                if (s_instance == null || !s_instance.IsLoaded)
                    s_instance = new ManageDictionariesWindow();

                return s_instance;
            }
        }

        public ManageDictionariesWindow()
        {
            InitializeComponent();
            UpdateDictionariesDisplay();
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Visibility = Visibility.Collapsed;
            Utils.SerializeDicts();
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
                    // Margin = new Thickness(10),
                };
                var dictTypeDisplay = new TextBlock
                {
                    Width = 135,
                    Text = dict.Type.GetDescription() ?? dict.Type.ToString(),
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

                buttonUpdate.Click += async (_, _) =>
                {
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
                        .Select(t => t.Type).ToList().Contains(dict.Type)
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
                    Visibility = Storage.BuiltInDicts.Values
                        .Select(t => t.Type).ToList().Contains(dict.Type)
                        ? Visibility.Collapsed
                        : Visibility.Visible,
                };

                checkBox.Unchecked += (_, _) => dict.Active = false;
                checkBox.Checked += (_, _) => dict.Active = true;
                buttonIncreasePriority.Click += (_, _) =>
                {
                    PrioritizeDict(Storage.Dicts, dict.Type);
                    UpdateDictionariesDisplay();
                };
                buttonDecreasePriority.Click += (_, _) =>
                {
                    UnPrioritizeDict(Storage.Dicts, dict.Type);
                    UpdateDictionariesDisplay();
                };
                buttonRemove.Click += (_, _) =>
                {
                    if (System.Windows.MessageBox.Show("Really remove dictionary?", "Confirmation",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question,
                            MessageBoxResult.No,
                            MessageBoxOptions.DefaultDesktopOnly) == MessageBoxResult.Yes)
                    {
                        dict.Contents.Clear();
                        Storage.Dicts.Remove(dict.Type);
                        UpdateDictionariesDisplay();

                        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);
                    }
                };
                buttonEdit.Click += (_, _) =>
                {
                    new EditDictionaryWindow(Storage.Dicts[dict.Type]).ShowDialog();
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
            }

            DictionariesDisplay.ItemsSource = resultDockPanels.OrderBy(dockPanel =>
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
                    path = Path.GetDirectoryName(path);

                Process.Start("explorer.exe", path ?? throw new InvalidOperationException());
            }
        }

        private static void PrioritizeDict(Dictionary<DictType, Dict> dicts, DictType typeToBePrioritized)
        {
            if (Storage.Dicts[typeToBePrioritized].Priority == 0) return;

            dicts.Single(dict => dict.Value.Priority == Storage.Dicts[typeToBePrioritized].Priority - 1).Value
                .Priority += 1;
            Storage.Dicts[typeToBePrioritized].Priority -= 1;
        }

        private static void UnPrioritizeDict(Dictionary<DictType, Dict> dicts, DictType typeToBeUnPrioritized)
        {
            // lowest priority means highest number
            int lowestPriority = Storage.Dicts.Select(dict => dict.Value.Priority).Max();
            if (Storage.Dicts[typeToBeUnPrioritized].Priority == lowestPriority) return;

            dicts.Single(dict => dict.Value.Priority == Storage.Dicts[typeToBeUnPrioritized].Priority + 1).Value
                .Priority -= 1;
            Storage.Dicts[typeToBeUnPrioritized].Priority += 1;
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

            bool isDownloaded = await ResourceUpdater.UpdateResource(Storage.Dicts[DictType.JMdict].Path,
                    new Uri("http://ftp.edrdg.org/pub/Nihongo/JMdict_e.gz"),
                    DictType.JMdict.ToString(), true, false)
                .ConfigureAwait(false);

            if (isDownloaded)
            {
                Storage.Dicts[DictType.JMdict].Contents.Clear();

                await Task.Run(async () => await JMdictLoader
                    .Load(Storage.Dicts[DictType.JMdict].Path).ConfigureAwait(false));

                await JmdictWcLoader.JmdictWordClassSerializer().ConfigureAwait(false);

                Storage.WcDict.Clear();

                await JmdictWcLoader.Load().ConfigureAwait(false);

                if (!Storage.Dicts[DictType.JMdict].Active)
                    Storage.Dicts[DictType.JMdict].Contents.Clear();

                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);
            }

            Storage.UpdatingJMdict = false;
        }

        private static async Task UpdateJMnedict()
        {
            Storage.UpdatingJMnedict = true;

            bool isDownloaded = await ResourceUpdater.UpdateResource(Storage.Dicts[DictType.JMnedict].Path,
                    new Uri("http://ftp.edrdg.org/pub/Nihongo/JMnedict.xml.gz"),
                    DictType.JMnedict.ToString(), true, false)
                .ConfigureAwait(false);

            if (isDownloaded)
            {
                Storage.Dicts[DictType.JMnedict].Contents.Clear();

                await Task.Run(async () => await JMnedictLoader
                    .Load(Storage.Dicts[DictType.JMnedict].Path).ConfigureAwait(false));

                if (!Storage.Dicts[DictType.JMnedict].Active)
                    Storage.Dicts[DictType.JMnedict].Contents.Clear();

                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);
            }

            Storage.UpdatingJMnedict = false;
        }

        private static async Task UpdateKanjidic()
        {
            Storage.UpdatingKanjidic = true;

            bool isDownloaded = await ResourceUpdater.UpdateResource(Storage.Dicts[DictType.Kanjidic].Path,
                    new Uri("http://www.edrdg.org/kanjidic/kanjidic2.xml.gz"),
                    DictType.Kanjidic.ToString(), true, false)
                .ConfigureAwait(false);

            if (isDownloaded)
            {
                Storage.Dicts[DictType.Kanjidic].Contents.Clear();

                await Task.Run(async () => await KanjiInfoLoader
                    .Load(Storage.Dicts[DictType.Kanjidic].Path).ConfigureAwait(false));

                if (!Storage.Dicts[DictType.Kanjidic].Active)
                    Storage.Dicts[DictType.Kanjidic].Contents.Clear();

                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);
            }

            Storage.UpdatingKanjidic = false;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
