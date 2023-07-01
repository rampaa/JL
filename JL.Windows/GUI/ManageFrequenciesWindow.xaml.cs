using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using JL.Core.Freqs;
using JL.Core.Utilities;
using JL.Windows.Utilities;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for ManageFrequenciesWindow.xaml
/// </summary>
internal sealed partial class ManageFrequenciesWindow : Window
{
    private static ManageFrequenciesWindow? s_instance;

    private IntPtr _windowHandle;

    public static ManageFrequenciesWindow Instance => s_instance ??= new ManageFrequenciesWindow();

    public ManageFrequenciesWindow()
    {
        InitializeComponent();
        UpdateFreqsDisplay();
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
        await FreqUtils.SerializeFreqs().ConfigureAwait(false);
        await FreqUtils.LoadFrequencies().ConfigureAwait(false);
    }

    // probably should be split into several methods
    private void UpdateFreqsDisplay()
    {
        List<DockPanel> resultDockPanels = new();

        foreach (Freq freq in FreqUtils.FreqDicts.Values.ToList())
        {
            DockPanel dockPanel = new();

            var checkBox = new CheckBox
            {
                Width = 20,
                IsChecked = freq.Active,
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
                Text = freq.Priority.ToString(CultureInfo.InvariantCulture),
                Visibility = Visibility.Collapsed
                // Margin = new Thickness(10),
            };
            var freqTypeDisplay = new TextBlock
            {
                Width = 177,
                Text = freq.Name,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(10)
            };

            bool invalidPath = !Directory.Exists(freq.Path) && !File.Exists(freq.Path);
            var freqPathValidityDisplay = new TextBlock
            {
                Width = 13,
                Text = invalidPath ? "❌" : "",
                ToolTip = invalidPath ? "Invalid Path" : null,
                Foreground = Brushes.Crimson,
                Margin = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var freqPathDisplay = new TextBlock
            {
                Width = 300,
                Text = freq.Path,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(10),
                Cursor = Cursors.Hand
            };

            freqPathDisplay.PreviewMouseLeftButtonUp += PathTextBox_PreviewMouseLeftButtonUp;
            freqPathDisplay.MouseEnter += (_, _) => freqPathDisplay.TextDecorations = TextDecorations.Underline;
            freqPathDisplay.MouseLeave += (_, _) => freqPathDisplay.TextDecorations = null;

            var buttonRemove = new Button
            {
                Width = 75,
                Height = 30,
                Content = "Remove",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Foreground = Brushes.White,
                Background = Brushes.Red,
                BorderThickness = new Thickness(1)
            };

            var buttonEdit = new Button
            {
                Width = 45,
                Height = 30,
                Content = "Edit",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Foreground = Brushes.White,
                Background = Brushes.DodgerBlue,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 5, 0)
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
                DeprioritizeFreq(freq);
                UpdateFreqsDisplay();
            };

            buttonRemove.Click += (_, _) =>
            {
                if (Utils.Frontend.ShowYesNoDialog("Do you really want to remove this frequency dictionary?", "Confirmation"))
                {
                    freq.Contents.Clear();
                    freq.Contents.TrimExcess();
                    _ = FreqUtils.FreqDicts.Remove(freq.Name);

                    int priorityOfDeletedFreq = freq.Priority;

                    foreach (Freq f in FreqUtils.FreqDicts.Values)
                    {
                        if (f.Priority > priorityOfDeletedFreq)
                        {
                            f.Priority -= 1;
                        }
                    }

                    UpdateFreqsDisplay();

                    //GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);
                }
            };
            buttonEdit.Click += (_, _) =>
            {
                _ = new EditFrequencyWindow(freq) { Owner = this }.ShowDialog();
                UpdateFreqsDisplay();
            };

            resultDockPanels.Add(dockPanel);

            _ = dockPanel.Children.Add(checkBox);
            _ = dockPanel.Children.Add(buttonIncreasePriority);
            _ = dockPanel.Children.Add(buttonDecreasePriority);
            _ = dockPanel.Children.Add(priority);
            _ = dockPanel.Children.Add(freqTypeDisplay);
            _ = dockPanel.Children.Add(freqPathValidityDisplay);
            _ = dockPanel.Children.Add(freqPathDisplay);
            _ = dockPanel.Children.Add(buttonEdit);
            _ = dockPanel.Children.Add(buttonRemove);
        }

        FrequenciesDisplay.ItemsSource = resultDockPanels.OrderBy(static dockPanel =>
            dockPanel.Children
                .OfType<TextBlock>()
                .Where(static textBlock => textBlock.Name is "priority")
                .Select(static textBlockPriority => Convert.ToInt32(textBlockPriority.Text, CultureInfo.InvariantCulture)).First());
    }

    private void PathTextBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        string? path = ((TextBlock)sender).Text;

        if (File.Exists(path) || Directory.Exists(path))
        {
            if (File.Exists(path))
            {
                path = Path.GetDirectoryName(path);
            }

            if (path is not null)
            {
                _ = Process.Start("explorer.exe", path);
            }
        }
    }

    private static void PrioritizeFreq(Freq freq)
    {
        if (freq.Priority is 1)
        {
            return;
        }

        FreqUtils.FreqDicts.First(f => f.Value.Priority == freq.Priority - 1).Value.Priority += 1;
        freq.Priority -= 1;
    }

    private static void DeprioritizeFreq(Freq freq)
    {
        if (freq.Priority == FreqUtils.FreqDicts.Count)
        {
            return;
        }

        FreqUtils.FreqDicts.First(f => f.Value.Priority == freq.Priority + 1).Value.Priority -= 1;
        freq.Priority += 1;
    }

    private void ButtonAddFrequency_OnClick(object sender, RoutedEventArgs e)
    {
        _ = new AddFrequencyWindow { Owner = this }.ShowDialog();
        UpdateFreqsDisplay();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
