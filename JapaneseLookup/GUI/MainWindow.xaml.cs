using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Media;

namespace JapaneseLookup.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _lastWord = "";

        internal static bool MiningMode = false;

        public MainWindow()
        {
            InitializeComponent();
            MainWindowUtilities.MainWindowInitializer();
            ConfigManager.ApplySettings(this);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var windowClipboardManager = new ClipboardManager(this);
            windowClipboardManager.ClipboardChanged += ClipboardChanged;

            CopyFromClipboard();
        }

        private void CopyFromClipboard()
        {
            bool gotTextFromClipboard = false;
            while (Clipboard.ContainsText() && !gotTextFromClipboard)
            {
                try
                {
                    string text = Clipboard.GetText();
                    gotTextFromClipboard = true;
                    if (MainWindowUtilities.JapaneseRegex.IsMatch(text))
                    {
                        text = text.Trim();
                        MainWindowUtilities.Backlog += text + "\n";
                        MainTextBox.Text = text;
                    }
                }
                catch
                {
                }
            }
        }

        private void ClipboardChanged(object sender, EventArgs e)
        {
            CopyFromClipboard();
        }

        private void MainTextBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (MiningMode) return;

            // nazeka-style popup movement
            PopupWindow.UpdatePosition(PointToScreen(Mouse.GetPosition(this)));

            int charPosition = MainTextBox.GetCharacterIndexFromPoint(Mouse.GetPosition(MainTextBox), false);
            if (charPosition != -1)
            {
                (string sentence, int endPosition) = MainWindowUtilities.FindSentence(MainTextBox.Text, charPosition);
                string text;
                if (endPosition - charPosition + 1 < ConfigManager.MaxSearchLength)
                    text = MainTextBox.Text[charPosition..(endPosition + 1)];
                else
                    text = MainTextBox.Text[charPosition..(charPosition + ConfigManager.MaxSearchLength)];

                if (text == _lastWord) return;
                _lastWord = text;

                var results = MainWindowUtilities.LookUp(text);

                if (results != null)
                {
                    PopupWindow.Instance.StackPanel.Children.Clear();

                    // yomichan-style popup movement
                    // PopupWindow.UpdatePosition(PointToScreen(Mouse.GetPosition(this)));

                    PopupWindow.Instance.Show();
                    PopupWindow.Instance.Activate();
                    PopupWindow.Instance.Focus();
                    PopupWindow.DisplayResults(sentence, results);
                }

                else
                    PopupWindow.Instance.Hide();
            }
            else
            {
                _lastWord = "";
                PopupWindow.Instance.Hide();
            }
        }

        private void MWindow_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MainTextBox_MouseLeave(object sender, MouseEventArgs e)
        {
            if (MiningMode) return;

            PopupWindow.Instance.Hide();
            _lastWord = "";
        }

        private void MainTextBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                if (MainTextBox.Text != MainWindowUtilities.Backlog)
                {
                    if (MainTextBox.GetFirstVisibleLineIndex() == 0)
                    {
                        int caretIndex = MainWindowUtilities.Backlog.Length - MainTextBox.Text.Length;
                        MainTextBox.Text = MainWindowUtilities.Backlog;
                        MainTextBox.CaretIndex = caretIndex;
                        MainTextBox.ScrollToEnd();
                    }
                }
            }
        }

        private void MinimizeButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MinimizeButton_MouseEnter(object sender, MouseEventArgs e)
        {
            MinimizeButton.Foreground = new SolidColorBrush(Colors.SteelBlue);
        }

        private void MinimizeButton_MouseLeave(object sender, MouseEventArgs e)
        {
            MinimizeButton.Foreground = new SolidColorBrush(Colors.White);
        }

        private void CloseButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void CloseButton_MouseEnter(object sender, MouseEventArgs e)
        {
            CloseButton.Foreground = new SolidColorBrush(Colors.SteelBlue);
        }

        private void CloseButton_MouseLeave(object sender, MouseEventArgs e)
        {
            CloseButton.Foreground = new SolidColorBrush(Colors.White);
        }

        private void OpacityButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(MWindow.Background.Opacity == 0)
                MWindow.Background.Opacity = OpacitySlider.Value / 100;

            else if (OpacitySlider.Visibility == Visibility.Collapsed)
                OpacitySlider.Visibility = Visibility.Visible;

            else
                OpacitySlider.Visibility = Visibility.Collapsed;
        }

        private void FontSizeButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (FontSizeSlider.Visibility == Visibility.Collapsed)
                FontSizeSlider.Visibility = Visibility.Visible;
            else
                FontSizeSlider.Visibility = Visibility.Collapsed;
        }
        private void MWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ConfigManager.SaveBeforeClosing(this);
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MWindow.Background.Opacity = OpacitySlider.Value/100;
        }

        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MainTextBox.FontSize = FontSizeSlider.Value;
        }

        private void MWindow_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.K:
                    {
                        MiningSetupWindow.Instance.ShowDialog();
                        break;
                    }
                case Key.L:
                    {
                        ConfigManager.LoadPreferences(PreferencesWindow.Instance);
                        PreferencesWindow.Instance.ShowDialog();
                        break;
                    }
                case Key.T:
                    {
                        MWindow.Background.Opacity = 0;
                        break;
                    }
            }
        }

        private void OpacitySlider_LostMouseCapture(object sender, MouseEventArgs e)
        {
            OpacitySlider.Visibility = Visibility.Collapsed;
        }

        private void FontSizeSlider_LostMouseCapture(object sender, MouseEventArgs e)
        {
            FontSizeSlider.Visibility = Visibility.Collapsed;
        }
    }
}