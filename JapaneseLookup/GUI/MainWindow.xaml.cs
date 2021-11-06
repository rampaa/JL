using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using JapaneseLookup.Utilities;

namespace JapaneseLookup.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int _currentTextIndex;

        private static PopupWindow _firstPopupWindow;

        public static PopupWindow FirstPopupWindow
        {
            get { return _firstPopupWindow ??= new PopupWindow(); }
        }

        public MainWindow()
        {
            InitializeComponent();
            ConfigManager.ApplyPreferences(this);
            MainWindowUtilities.MainWindowInitializer();
            MainWindowChrome.Freeze();
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

        public async void MainTextBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (MWindow.Background.Opacity == 0 ||ConfigManager.InactiveLookupMode) return;
            await FirstPopupWindow.TextBox_MouseMove(MainTextBox);
        }

        private void MWindow_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MainTextBox_MouseLeave(object sender, MouseEventArgs e)
        {
            //OpacitySlider.Visibility = Visibility.Collapsed;
            //FontSizeSlider.Visibility = Visibility.Collapsed;

            if (FirstPopupWindow.MiningMode) return;

            FirstPopupWindow.Hide();
            FirstPopupWindow.LastText = "";
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
                        MainTextBox.Text =
                            "Character count: " + String.Join("", MainWindowUtilities.Backlog).Length + "\n"
                            + allBacklogText;
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
            if (Utils.KeyGestureComparer(e, ConfigManager.ShowPreferencesWindowKeyGesture))
            {
                MainWindowUtilities.ShowPreferencesWindow();
            }

            else if (Utils.KeyGestureComparer(e, ConfigManager.MousePassThroughModeKeyGesture))
            {
                MWindow.Background.Opacity = 0;
                Keyboard.ClearFocus();
            }

            else if (Utils.KeyGestureComparer(e, ConfigManager.KanjiModeKeyGesture))
            {
                ConfigManager.KanjiMode = !ConfigManager.KanjiMode;
                FirstPopupWindow.LastText = "";
                MainTextBox_MouseMove(null, null);
            }

            else if (Utils.KeyGestureComparer(e, ConfigManager.ShowAddNameWindowKeyGesture))
            {
                MainWindowUtilities.ShowAddNameWindow();
            }

            else if (Utils.KeyGestureComparer(e, ConfigManager.ShowAddWordWindowKeyGesture))
            {
                MainWindowUtilities.ShowAddWordWindow();
            }

            else if (Utils.KeyGestureComparer(e, ConfigManager.SearchWithBrowserKeyGesture))
            {
                MainWindowUtilities.SearchWithBrowser();
            }

            else if (Utils.KeyGestureComparer(e, ConfigManager.InactiveLookupModeKeyGesture))
            {
                ConfigManager.InactiveLookupMode = !ConfigManager.InactiveLookupMode;
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
            if (Utils.KeyGestureComparer(e, ConfigManager.SteppedBacklogBackwardsKeyGesture))
            {
                if (_currentTextIndex != 0)
                {
                    _currentTextIndex--;
                    MainTextBox.Foreground = ConfigManager.MainWindowBacklogTextColor;
                }

                MainTextBox.Text = MainWindowUtilities.Backlog[_currentTextIndex];
            }
            else if (Utils.KeyGestureComparer(e, ConfigManager.SteppedBacklogForwardsKeyGesture))
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

        private void MWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ConfigManager.MainWindowHeight = Height;
            ConfigManager.MainWindowWidth = Width;
        }
    }
}