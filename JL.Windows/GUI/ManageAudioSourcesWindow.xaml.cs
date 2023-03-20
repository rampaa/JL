using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using JL.Core;
using JL.Core.Audio;
using JL.Core.Utilities;
using JL.Windows.Utilities;

namespace JL.Windows.GUI;
/// <summary>
/// Interaction logic for ManageAudioSourcesWindow.xaml
/// </summary>
internal sealed partial class ManageAudioSourcesWindow : Window
{
    private static ManageAudioSourcesWindow? s_instance;

    private IntPtr _windowHandle;
    public static ManageAudioSourcesWindow Instance => s_instance ??= new ManageAudioSourcesWindow();

    public ManageAudioSourcesWindow()
    {
        InitializeComponent();
        UpdateAudioSourcesDisplay();
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
        Storage.Frontend.InvalidateDisplayCache();
        WindowsUtils.UpdateMainWindowVisibility();
        _ = MainWindow.Instance.Focus();
        s_instance = null;
        await Utils.SerializeAudioSources().ConfigureAwait(false);
    }

    // probably should be split into several methods
    private void UpdateAudioSourcesDisplay()
    {
        List<DockPanel> resultDockPanels = new();

        foreach ((string uri, AudioSource audioSource) in Storage.AudioSources.OrderBy(static a => a.Value.Priority))
        {
            DockPanel dockPanel = new();

            var checkBox = new CheckBox
            {
                Width = 20,
                IsChecked = audioSource.Active,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var buttonIncreasePriority = new Button
            {
                Width = 25,
                Content = "↑",
                Margin = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var buttonDecreasePriority = new Button
            {
                Width = 25,
                Content = "↓",
                Margin = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var priority = new TextBlock
            {
                Name = "priority",
                // Width = 20,
                Width = 0,
                Text = audioSource.Priority.ToString(CultureInfo.InvariantCulture),
                Visibility = Visibility.Collapsed
                // Margin = new Thickness(10),
            };

            var audioSourceTypeDisplay = new TextBlock
            {
                Width = 60,
                Text = audioSource.Type.GetDescription(),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10)
            };
            var audioSourceUriDisplay = new TextBlock
            {
                Width = 470,
                Text = uri,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10),
                Cursor = Cursors.Hand
            };

            audioSourceUriDisplay.MouseEnter += (_, _) => audioSourceUriDisplay.TextDecorations = TextDecorations.Underline;
            audioSourceUriDisplay.MouseLeave += (_, _) => audioSourceUriDisplay.TextDecorations = null;

            var buttonRemove = new Button
            {
                Width = 75,
                Height = 30,
                Content = "Remove",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                Background = Brushes.Red,
                BorderThickness = new Thickness(1)
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
            };

            checkBox.Unchecked += (_, _) => audioSource.Active = false;
            checkBox.Checked += (_, _) => audioSource.Active = true;

            buttonIncreasePriority.Click += (_, _) =>
            {
                PrioritizeAudioSource(audioSource);
                UpdateAudioSourcesDisplay();
            };

            buttonDecreasePriority.Click += (_, _) =>
            {
                UnPrioritizeAudioSource(audioSource);
                UpdateAudioSourcesDisplay();
            };

            buttonRemove.Click += (_, _) =>
            {
                if (Storage.Frontend.ShowYesNoDialog("Do you really want to remove this audio source?", "Confirmation"))
                {
                    _ = Storage.AudioSources.Remove(uri);

                    int priorityOfDeletedAudioSource = audioSource.Priority;

                    foreach (AudioSource a in Storage.AudioSources.Values)
                    {
                        if (a.Priority > priorityOfDeletedAudioSource)
                        {
                            audioSource.Priority -= 1;
                        }
                    }

                    UpdateAudioSourcesDisplay();
                }
            };
            buttonEdit.Click += (_, _) =>
            {
                _ = new EditAudioSourceWindow(uri, audioSource) { Owner = this }.ShowDialog();
                UpdateAudioSourcesDisplay();
            };

            resultDockPanels.Add(dockPanel);

            _ = dockPanel.Children.Add(checkBox);
            _ = dockPanel.Children.Add(buttonIncreasePriority);
            _ = dockPanel.Children.Add(buttonDecreasePriority);
            _ = dockPanel.Children.Add(priority);
            _ = dockPanel.Children.Add(audioSourceTypeDisplay);
            _ = dockPanel.Children.Add(audioSourceUriDisplay);
            _ = dockPanel.Children.Add(buttonEdit);
            _ = dockPanel.Children.Add(buttonRemove);
        }

        AudioSourceListBox.ItemsSource = resultDockPanels.OrderBy(static dockPanel =>
            dockPanel.Children
                .OfType<TextBlock>()
                .Where(static textBlock => textBlock.Name is "priority")
                .Select(static textBlockPriority => Convert.ToInt32(textBlockPriority.Text, CultureInfo.InvariantCulture)).First());
    }

    private static void PrioritizeAudioSource(AudioSource audioSource)
    {
        if (audioSource.Priority is 1)
        {
            return;
        }

        Storage.AudioSources.First(f => f.Value.Priority == audioSource.Priority - 1).Value.Priority += 1;
        audioSource.Priority -= 1;
    }

    private static void UnPrioritizeAudioSource(AudioSource audioSource)
    {
        if (audioSource.Priority == Storage.AudioSources.Count)
        {
            return;
        }

        Storage.AudioSources.First(a => a.Value.Priority == audioSource.Priority + 1).Value.Priority -= 1;
        audioSource.Priority += 1;
    }

    private void ButtonAddAudioSource_OnClick(object sender, RoutedEventArgs e)
    {
        _ = new AddAudioSourceWindow { Owner = this }.ShowDialog();
        UpdateAudioSourcesDisplay();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
