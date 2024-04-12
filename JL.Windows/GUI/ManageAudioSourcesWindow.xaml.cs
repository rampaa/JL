using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using JL.Core.Audio;
using JL.Core.Utilities;
using JL.Windows.SpeechSynthesis;
using JL.Windows.Utilities;

namespace JL.Windows.GUI;

/// <summary>
/// Interaction logic for ManageAudioSourcesWindow.xaml
/// </summary>
internal sealed partial class ManageAudioSourcesWindow : Window
{
    private static ManageAudioSourcesWindow? s_instance;

    private nint _windowHandle;
    public static ManageAudioSourcesWindow Instance => s_instance ??= new ManageAudioSourcesWindow();

    private ManageAudioSourcesWindow()
    {
        InitializeComponent();
        UpdateAudioSourcesDisplay();
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

        await AudioUtils.SerializeAudioSources().ConfigureAwait(false);
        SpeechSynthesisUtils.SetInstalledVoiceWithHighestPriority();
    }

    // probably should be split into several methods
    private void UpdateAudioSourcesDisplay()
    {
        List<DockPanel> resultDockPanels = [];

        foreach ((string uri, AudioSource audioSource) in AudioUtils.AudioSources.OrderBy(static a => a.Value.Priority))
        {
            DockPanel dockPanel = new();

            CheckBox checkBox = new()
            {
                Width = 20,
                IsChecked = audioSource.Active,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Tag = audioSource
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
                Tag = audioSource
            };
            increasePriorityButton.Click += IncreasePriorityButton_Click;

            Button decreasePriorityButton = new()
            {
                Width = 25,
                Content = '↓',
                Margin = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Tag = audioSource
            };
            decreasePriorityButton.Click += DecreasePriorityButton_Click;

            TextBlock audioSourceTypeTextBlock = new()
            {
                Width = 80,
                Text = audioSource.Type.GetDescription(),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10)
            };

            TextBlock audioSourceUriTextBlock = new()
            {
                Name = nameof(audioSourceUriTextBlock),
                Width = 470,
                Text = uri,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10),
                Cursor = Cursors.Hand
            };

            Button editButton = new()
            {
                Width = 45,
                Height = 30,
                Content = "Edit",
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                Background = Brushes.DodgerBlue,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 5, 0),
                Tag = audioSource
            };
            editButton.Click += EditButton_Click;

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
                Tag = audioSource
            };
            removeButton.Click += RemoveButton_Click;

            _ = dockPanel.Children.Add(checkBox);
            _ = dockPanel.Children.Add(increasePriorityButton);
            _ = dockPanel.Children.Add(decreasePriorityButton);
            _ = dockPanel.Children.Add(audioSourceTypeTextBlock);
            _ = dockPanel.Children.Add(audioSourceUriTextBlock);
            _ = dockPanel.Children.Add(editButton);
            _ = dockPanel.Children.Add(removeButton);

            resultDockPanels.Add(dockPanel);
        }

        AudioSourceListBox.ItemsSource = resultDockPanels
            .OrderBy(static dockPanel => ((AudioSource)((CheckBox)dockPanel.Children[0]).Tag).Priority);
    }

    private static void CheckBox_Checked(object sender, RoutedEventArgs e)
    {
        AudioSource audioSource = (AudioSource)((CheckBox)sender).Tag;
        audioSource.Active = true;
    }

    private static void CheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        AudioSource audioSource = (AudioSource)((CheckBox)sender).Tag;
        audioSource.Active = false;
    }

    private void IncreasePriorityButton_Click(object sender, RoutedEventArgs e)
    {
        AudioSource audioSource = (AudioSource)((Button)sender).Tag;
        PrioritizeAudioSource(audioSource);
        UpdateAudioSourcesDisplay();
    }

    private void DecreasePriorityButton_Click(object sender, RoutedEventArgs e)
    {
        AudioSource audioSource = (AudioSource)((Button)sender).Tag;
        DeprioritizeAudioSource(audioSource);
        UpdateAudioSourcesDisplay();
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (WindowsUtils.ShowYesNoDialog("Do you really want to remove this audio source?", "Confirmation"))
        {
            Button removeButton = (Button)sender;

            string uri = removeButton.Parent.GetChildByName<TextBlock>("audioSourceUriTextBlock")!.Text;
            _ = AudioUtils.AudioSources.Remove(uri);

            AudioSource audioSource = (AudioSource)removeButton.Tag;
            int priorityOfDeletedAudioSource = audioSource.Priority;

            foreach (AudioSource a in AudioUtils.AudioSources.Values)
            {
                if (a.Priority > priorityOfDeletedAudioSource)
                {
                    a.Priority -= 1;
                }
            }

            UpdateAudioSourcesDisplay();
        }
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        Button editButton = (Button)sender;
        AudioSource audioSource = (AudioSource)editButton.Tag;
        string uri = editButton.Parent.GetChildByName<TextBlock>("audioSourceUriTextBlock")!.Text;

        _ = new EditAudioSourceWindow(uri, audioSource)
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        }.ShowDialog();

        UpdateAudioSourcesDisplay();
    }

    private static void PrioritizeAudioSource(AudioSource audioSource)
    {
        if (audioSource.Priority is 1)
        {
            return;
        }

        AudioUtils.AudioSources.First(f => f.Value.Priority == audioSource.Priority - 1).Value.Priority += 1;
        audioSource.Priority -= 1;
    }

    private static void DeprioritizeAudioSource(AudioSource audioSource)
    {
        if (audioSource.Priority == AudioUtils.AudioSources.Count)
        {
            return;
        }

        AudioUtils.AudioSources.First(a => a.Value.Priority == audioSource.Priority + 1).Value.Priority -= 1;
        audioSource.Priority += 1;
    }

    private void ButtonAddAudioSource_OnClick(object sender, RoutedEventArgs e)
    {
        _ = new AddAudioSourceWindow
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        }.ShowDialog();

        UpdateAudioSourcesDisplay();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
