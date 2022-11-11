using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using JL.Core;
using JL.Core.Freqs;
using JL.Core.Utilities;
using JL.Windows.Utilities;
using Path = System.IO.Path;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for ManageFrequenciesWindow.xaml
/// </summary>
public partial class ManageFrequenciesWindow : Window
{
    private static ManageFrequenciesWindow? s_instance;

    public static ManageFrequenciesWindow Instance
    {
        get { return s_instance ??= new(); }
    }
    public ManageFrequenciesWindow()
    {
        InitializeComponent();
        UpdateFreqsDisplay();
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
        await Utils.SerializeFreqs().ConfigureAwait(false);
        await Storage.LoadFrequencies().ConfigureAwait(false);
    }

    // probably should be split into several methods
    private void UpdateFreqsDisplay()
    {
        List<DockPanel> resultDockPanels = new();

        foreach (Freq freq in Storage.FreqDicts.Values.ToList())
        {
            DockPanel dockPanel = new();

            var checkBox = new CheckBox { Width = 20, IsChecked = freq.Active, Margin = new Thickness(10), };
            var buttonIncreasePriority = new Button { Width = 25, Content = "↑", Margin = new Thickness(1), };
            var buttonDecreasePriority = new Button { Width = 25, Content = "↓", Margin = new Thickness(1), };
            var priority = new TextBlock
            {
                Name = "priority",
                // Width = 20,
                Width = 0,
                Text = freq.Priority.ToString(),
                Visibility = Visibility.Collapsed,
                // Margin = new Thickness(10),
            };
            var freqTypeDisplay = new TextBlock
            {
                Width = 177,
                Text = freq.Name,
                Margin = new Thickness(10),
            };
            var freqPathValidityDisplay = new TextBlock
            {
                Width = 13,
                Text = "❌",
                ToolTip = "Invalid Path",
                Foreground = Brushes.Crimson,
                Margin = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Visibility = !Directory.Exists(freq.Path) && !File.Exists(freq.Path)
                    ? Visibility.Visible
                    : Visibility.Collapsed
            };
            var freqPathDisplay = new TextBlock
            {
                Width = 200,
                Text = freq.Path,
                Margin = new Thickness(10),
                Cursor = Cursors.Hand
            };

            freqPathDisplay.PreviewMouseLeftButtonUp += PathTextbox_PreviewMouseLeftButtonUp;
            freqPathDisplay.MouseEnter += (_, _) => freqPathDisplay.TextDecorations = TextDecorations.Underline;
            freqPathDisplay.MouseLeave += (_, _) => freqPathDisplay.TextDecorations = null;

            var buttonRemove = new Button
            {
                Width = 75,
                Height = 30,
                Content = "Remove",
                Foreground = Brushes.White,
                Background = Brushes.Red,
                BorderThickness = new Thickness(1),
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
            };

            checkBox.Unchecked += (_, _) => freq.Active = false;
            checkBox.Checked += (_, _) => freq.Active = true;
            buttonIncreasePriority.Click += (_, _) =>
            {
                PrioritizeFreq(freq);
                UpdateFreqsDisplay();
            };
            buttonDecreasePriority.Click += (_, _) =>
            {
                UnPrioritizeFreq(freq);
                UpdateFreqsDisplay();
            };
            buttonRemove.Click += (_, _) =>
            {
                if (MessageBox.Show("Really remove frequency?", "Confirmation",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question,
                        MessageBoxResult.No,
                        MessageBoxOptions.DefaultDesktopOnly) == MessageBoxResult.Yes)
                {
                    freq.Contents.Clear();
                    Storage.FreqDicts.Remove(freq.Name);
                    UpdateFreqsDisplay();

                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);
                }
            };
            buttonEdit.Click += (_, _) =>
            {
                new EditFrequencyWindow(freq).ShowDialog();
                UpdateFreqsDisplay();
            };

            resultDockPanels.Add(dockPanel);

            dockPanel.Children.Add(checkBox);
            dockPanel.Children.Add(buttonIncreasePriority);
            dockPanel.Children.Add(buttonDecreasePriority);
            dockPanel.Children.Add(priority);
            dockPanel.Children.Add(freqTypeDisplay);
            dockPanel.Children.Add(freqPathValidityDisplay);
            dockPanel.Children.Add(freqPathDisplay);
            dockPanel.Children.Add(buttonEdit);
            dockPanel.Children.Add(buttonRemove);
        }

        FrequenciesDisplay!.ItemsSource = resultDockPanels.OrderBy(dockPanel =>
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

    private static void PrioritizeFreq(Freq freq)
    {
        if (freq.Priority == 0) return;

        Storage.FreqDicts.Single(f => f.Value.Priority == freq.Priority - 1).Value.Priority += 1;
        freq.Priority -= 1;
    }

    private static void UnPrioritizeFreq(Freq freq)
    {
        // lowest priority means highest number
        int lowestPriority = Storage.FreqDicts.Select(f => f.Value.Priority).Max();
        if (freq.Priority == lowestPriority) return;

        Storage.FreqDicts.Single(f => f.Value.Priority == freq.Priority + 1).Value.Priority -= 1;
        freq.Priority += 1;
    }


    private void ButtonAddFrequency_OnClick(object sender, RoutedEventArgs e)
    {
        new AddFrequencyWindow().ShowDialog();
        UpdateFreqsDisplay();
    }
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
