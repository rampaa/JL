using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.Linq;
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
        private int _currentTextIndex;
        public static int CurrentCharPosition { get; set; }
        public static string CurrentText { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            ConfigManager.ApplyPreferences(this);
            MainWindowUtilities.MainWindowInitializer();
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
                        MainWindowUtilities.Backlog.Add(text);
                        MainTextBox.Text = text;
                        MainTextBox.Foreground = ConfigManager.MainWindowTextColor;
                        _currentTextIndex = MainWindowUtilities.Backlog.Count - 1;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
        }

        private void ClipboardChanged(object sender, EventArgs e)
        {
            CopyFromClipboard();
        }

        private void MainTextBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (MiningMode || MWindow.Background.Opacity == 0) return;

            int charPosition = MainTextBox.GetCharacterIndexFromPoint(Mouse.GetPosition(MainTextBox), false);

            if (charPosition != -1)
            {
                // popup follows cursor
                PopupWindow.Instance.UpdatePosition(PointToScreen(Mouse.GetPosition(this)));

                if (charPosition > 0 && char.IsHighSurrogate(MainTextBox.Text[charPosition - 1]))
                    --charPosition;

                CurrentText = MainTextBox.Text;
                CurrentCharPosition = charPosition;

                int endPosition = MainWindowUtilities.FindWordBoundary(MainTextBox.Text, charPosition);

                string text;
                if (endPosition - charPosition <= ConfigManager.MaxSearchLength)
                    text = MainTextBox.Text[charPosition..endPosition];
                else
                    text = MainTextBox.Text[charPosition..(charPosition + ConfigManager.MaxSearchLength)];

                if (text == _lastWord) return;
                _lastWord = text;

                var lookupResults = MainWindowUtilities.Lookup(text);
                if (lookupResults != null && lookupResults.Any())
                {
                    PopupWindowUtilities.DisposeResultStackPanels();

                    // popup doesn't follow cursor
                    // PopupWindow.Instance.UpdatePosition(PointToScreen(Mouse.GetPosition(this)));

                    PopupWindow.Instance.Visibility = Visibility.Visible;
                    PopupWindow.Instance.Activate();
                    PopupWindow.Instance.Focus();

                    PopupWindowUtilities.LastLookupResults = lookupResults;
                    PopupWindowUtilities.DisplayResults(false);
                }
                else
                    PopupWindow.Instance.Visibility = Visibility.Hidden;
            }
            else
            {
                _lastWord = "";
                PopupWindow.Instance.Visibility = Visibility.Hidden;
            }
        }

        private void MWindow_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MainTextBox_MouseLeave(object sender, MouseEventArgs e)
        {
            //OpacitySlider.Visibility = Visibility.Collapsed;
            //FontSizeSlider.Visibility = Visibility.Collapsed;

            if (MiningMode) return;

            PopupWindow.Instance.Hide();
            _lastWord = "";
        }

        private void MainTextBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                var allBacklogText = String.Join("\n", MainWindowUtilities.Backlog);
                if (MainTextBox.Text != allBacklogText)
                {
                    if (MainTextBox.GetFirstVisibleLineIndex() == 0)
                    {
                        int caretIndex = allBacklogText.Length - MainTextBox.Text.Length;
                        MainTextBox.Text = allBacklogText;
                        MainTextBox.Foreground = ConfigManager.MainWindowBacklogTextColor;
                        MainTextBox.CaretIndex = caretIndex;
                        MainTextBox.ScrollToEnd();
                    }
                }
            }
        }

        private void MinimizeButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpacitySlider.Visibility = Visibility.Collapsed;
            FontSizeSlider.Visibility = Visibility.Collapsed;
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
            FontSizeSlider.Visibility = Visibility.Collapsed;

            if (MWindow.Background.Opacity == 0)
                MWindow.Background.Opacity = OpacitySlider.Value / 100;

            else if (OpacitySlider.Visibility == Visibility.Collapsed)
            {
                OpacitySlider.Visibility = Visibility.Visible;
                OpacitySlider.Focus();
            }

            else
                OpacitySlider.Visibility = Visibility.Collapsed;
        }

        private void FontSizeButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OpacitySlider.Visibility = Visibility.Collapsed;

            if (FontSizeSlider.Visibility == Visibility.Collapsed)
            {
                FontSizeSlider.Visibility = Visibility.Visible;
                FontSizeSlider.Focus();
            }

            else
                FontSizeSlider.Visibility = Visibility.Collapsed;
        }

        private void MWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ConfigManager.SaveBeforeClosing(this);
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MWindow.Background.Opacity = OpacitySlider.Value / 100;
        }

        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MainTextBox.FontSize = FontSizeSlider.Value;
        }

        private void MWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == ConfigManager.ShowPreferencesWindowKey)
            {
                MainWindowUtilities.ShowPreferencesWindow();
            }
            else if (e.Key == ConfigManager.TransparentModeKey)
            {
                MWindow.Background.Opacity = 0;
                Keyboard.ClearFocus();
            }
            else if (e.Key == ConfigManager.KanjiModeKey)
            {
                ConfigManager.KanjiMode = !ConfigManager.KanjiMode;
            }
            else if (e.Key == ConfigManager.ShowAddNameWindowKey)
            {
                MainWindowUtilities.ShowAddNameWindow();
            }
            else if (e.Key == ConfigManager.ShowAddWordWindowKey)
            {
                MainWindowUtilities.ShowAddWordWindow();
            }
            else if (e.Key == ConfigManager.SearchWithBrowserKey)
            {
                MainWindowUtilities.SearchWithBrowser();
            }
        }

        private void AddName(object sender, RoutedEventArgs e)
        {
            MainWindowUtilities.ShowAddNameWindow();
        }

        private void AddWord(object sender, RoutedEventArgs e)
        {
            MainWindowUtilities.ShowAddWordWindow();
        }

        private void ShowPreferences(object sender, RoutedEventArgs e)
        {
            MainWindowUtilities.ShowPreferencesWindow();
        }

        private void SearchWithBrowser(object sender, RoutedEventArgs e)
        {
            MainWindowUtilities.SearchWithBrowser();
        }

        private void MWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            SteppedBacklog(e);
        }

        private void SteppedBacklog(KeyEventArgs e)
        {
            if (e.Key == ConfigManager.SteppedBacklogBackwardsKey)
            {
                if (_currentTextIndex != 0)
                {
                    _currentTextIndex--;
                    MainTextBox.Foreground = ConfigManager.MainWindowBacklogTextColor;
                }

                MainTextBox.Text = MainWindowUtilities.Backlog[_currentTextIndex];
            }
            else if (e.Key == ConfigManager.SteppedBacklogForwardsKey)
            {
                if (_currentTextIndex < MainWindowUtilities.Backlog.Count - 1)
                {
                    _currentTextIndex++;
                    MainTextBox.Foreground = ConfigManager.MainWindowBacklogTextColor;
                }

                if (_currentTextIndex == MainWindowUtilities.Backlog.Count - 1)
                {
                    MainTextBox.Foreground = ConfigManager.MainWindowTextColor;
                }

                MainTextBox.Text = MainWindowUtilities.Backlog[_currentTextIndex];
            }
        }

        private void OpacitySlider_LostMouseCapture(object sender, MouseEventArgs e)
        {
            OpacitySlider.Visibility = Visibility.Collapsed;
        }

        private void OpacitySlider_LostFocus(object sender, RoutedEventArgs e)
        {
            OpacitySlider.Visibility = Visibility.Collapsed;
        }

        private void FontSizeSlider_LostMouseCapture(object sender, MouseEventArgs e)
        {
            FontSizeSlider.Visibility = Visibility.Collapsed;
        }

        private void FontSizeSlider_LostFocus(object sender, RoutedEventArgs e)
        {
            FontSizeSlider.Visibility = Visibility.Collapsed;
        }
    }
}