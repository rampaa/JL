using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using JL.Core.Freqs;
using JL.Core.Utilities;
using JL.Windows.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for ManageFrequenciesWindow.xaml
/// </summary>
internal sealed partial class ManageFrequenciesWindow : Window
{
    private static ManageFrequenciesWindow? s_instance;

    private nint _windowHandle;

    public static ManageFrequenciesWindow Instance => s_instance ??= new ManageFrequenciesWindow();

    private ManageFrequenciesWindow()
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

    private async void Window_Closed(object sender, EventArgs e)
    {
        s_instance = null;

        Utils.Frontend.InvalidateDisplayCache();

        WindowsUtils.UpdateMainWindowVisibility();
        _ = MainWindow.Instance.Focus();

        await FreqUtils.LoadFrequencies().ConfigureAwait(false);
        await FreqUtils.SerializeFreqs().ConfigureAwait(false);

        Utils.ClearStringPoolIfDictsAreReady();
    }

    // probably should be split into several methods
    private void UpdateFreqsDisplay()
    {
        List<DockPanel> resultDockPanels = new();

        foreach (Freq freq in FreqUtils.FreqDicts.Values.ToList())
        {
            DockPanel dockPanel = new();

            CheckBox checkBox = new()
            {
                Width = 20,
                IsChecked = freq.Active,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Tag = freq
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
                Tag = freq
            };
            increasePriorityButton.Click += IncreasePriorityButton_Click;

            Button decreasePriorityButton = new()
            {
                Width = 25,
                Content = '↓',
                Margin = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Tag = freq
            };
            decreasePriorityButton.Click += DecreasePriorityButton_Click;

            TextBlock freqTypeTextBlock = new()
            {
                Width = 177,
                Text = freq.Name,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(10)
            };

            string fullPath = Path.GetFullPath(freq.Path, Utils.ApplicationPath);
            bool invalidPath = !Directory.Exists(fullPath) && !File.Exists(fullPath);
            TextBlock freqPathValidityTextBlock = new()
            {
                Width = 13,
                Text = invalidPath ? "❌" : "",
                ToolTip = invalidPath ? "Invalid Path" : null,
                Foreground = Brushes.Crimson,
                Margin = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            TextBlock freqPathTextBlock = new()
            {
                Width = 300,
                Text = freq.Path,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(10),
                Cursor = Cursors.Hand
            };
            freqPathTextBlock.PreviewMouseLeftButtonUp += PathTextBlock_PreviewMouseLeftButtonUp;
            freqPathTextBlock.MouseEnter += PathTextBlock_MouseEnter;
            freqPathTextBlock.MouseLeave += PathTextBlock_MouseLeave;

            Button editButton = new()
            {
                Width = 45,
                Height = 30,
                Content = "Edit",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Foreground = Brushes.White,
                Background = Brushes.DodgerBlue,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 5, 0),
                Tag = freq
            };
            editButton.Click += EditButton_Click;

            Button removeButton = new()
            {
                Width = 75,
                Height = 30,
                Content = "Remove",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Foreground = Brushes.White,
                Background = Brushes.Red,
                BorderThickness = new Thickness(1),
                Tag = freq
            };
            removeButton.Click += RemoveButton_Click;

            _ = dockPanel.Children.Add(checkBox);
            _ = dockPanel.Children.Add(increasePriorityButton);
            _ = dockPanel.Children.Add(decreasePriorityButton);
            _ = dockPanel.Children.Add(freqTypeTextBlock);
            _ = dockPanel.Children.Add(freqPathValidityTextBlock);
            _ = dockPanel.Children.Add(freqPathTextBlock);
            _ = dockPanel.Children.Add(editButton);
            _ = dockPanel.Children.Add(removeButton);

            resultDockPanels.Add(dockPanel);
        }

        FrequenciesDisplay.ItemsSource = resultDockPanels
            .OrderBy(static dockPanel => ((Freq)((CheckBox)dockPanel.Children[0]).Tag).Priority);
    }

    private void PathTextBlock_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        string? fullPath = Path.GetFullPath(((TextBlock)sender).Text, Utils.ApplicationPath);
        if (File.Exists(fullPath) || Directory.Exists(fullPath))
        {
            if (File.Exists(fullPath))
            {
                fullPath = Path.GetDirectoryName(fullPath);
            }

            if (fullPath is not null)
            {
                _ = Process.Start("explorer.exe", fullPath);
            }
        }
    }

    private void PathTextBlock_MouseEnter(object sender, MouseEventArgs e)
    {
        ((TextBlock)sender).TextDecorations = TextDecorations.Underline;
    }

    private void PathTextBlock_MouseLeave(object sender, MouseEventArgs e)
    {
        ((TextBlock)sender).TextDecorations = null;
    }

    private void CheckBox_Checked(object sender, RoutedEventArgs e)
    {
        Freq freq = (Freq)((CheckBox)sender).Tag;
        freq.Active = true;
    }

    private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        Freq freq = (Freq)((CheckBox)sender).Tag;
        freq.Active = false;
    }

    private void IncreasePriorityButton_Click(object sender, RoutedEventArgs e)
    {
        Freq freq = (Freq)((Button)sender).Tag;
        PrioritizeFreq(freq);
        UpdateFreqsDisplay();
    }

    private void DecreasePriorityButton_Click(object sender, RoutedEventArgs e)
    {
        Freq freq = (Freq)((Button)sender).Tag;
        DeprioritizeFreq(freq);
        UpdateFreqsDisplay();
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (Utils.Frontend.ShowYesNoDialog("Do you really want to remove this frequency dictionary?", "Confirmation"))
        {
            Freq freq = (Freq)((Button)sender).Tag;
            freq.Contents.Clear();
            freq.Contents.TrimExcess();
            _ = FreqUtils.FreqDicts.Remove(freq.Name);

            string dbPath = FreqUtils.GetDBPath(freq.Name);
            if (File.Exists(dbPath))
            {
                SqliteConnection.ClearAllPools();
                File.Delete(dbPath);
            }

            int priorityOfDeletedFreq = freq.Priority;

            foreach (Freq f in FreqUtils.FreqDicts.Values)
            {
                if (f.Priority > priorityOfDeletedFreq)
                {
                    f.Priority -= 1;
                }
            }

            UpdateFreqsDisplay();
        }
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        Freq freq = (Freq)((Button)sender).Tag;
        _ = new EditFrequencyWindow(freq) { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog();
        UpdateFreqsDisplay();
    }

    private static void PrioritizeFreq(Freq freq)
    {
        if (freq.Priority is 1)
        {
            return;
        }

        FreqUtils.FreqDicts.First(f => f.Value.Priority == (freq.Priority - 1)).Value.Priority += 1;
        freq.Priority -= 1;
    }

    private static void DeprioritizeFreq(Freq freq)
    {
        if (freq.Priority == FreqUtils.FreqDicts.Count)
        {
            return;
        }

        FreqUtils.FreqDicts.First(f => f.Value.Priority == (freq.Priority + 1)).Value.Priority -= 1;
        freq.Priority += 1;
    }

    private void ButtonAddFrequency_OnClick(object sender, RoutedEventArgs e)
    {
        _ = new AddFrequencyWindow { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog();
        UpdateFreqsDisplay();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
