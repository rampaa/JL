using JapaneseLookup.Utilities;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

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

        public static MainWindow Instance { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
            ConfigManager.ApplyPreferences();
            Utils.DeserializeDicts().ConfigureAwait(false);
            ConfigManager.LoadDictionaries().ConfigureAwait(false);
            MainWindowChrome.Freeze();
            MainTextBox.IsInactiveSelectionHighlightEnabled = true;
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
                    Utils.Logger.Warning(e, "CopyFromClipboard failed");
                }
            }
        }

        private void ClipboardChanged(object sender, EventArgs e)
        {
            CopyFromClipboard();
        }

        public void MainTextBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (ConfigManager.LookupOnSelectOnly || Background.Opacity == 0 || MainTextboxContextMenu.IsVisible) return;
            FirstPopupWindow.TextBox_MouseMove(MainTextBox);
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MainTextBox_MouseLeave(object sender, MouseEventArgs e)
        {
            //OpacitySlider.Visibility = Visibility.Collapsed;
            //FontSizeSlider.Visibility = Visibility.Collapsed;

            if (FirstPopupWindow.MiningMode || ConfigManager.LookupOnSelectOnly) return;

            FirstPopupWindow.Hide();
            FirstPopupWindow.LastText = "";
        }

        private void MainTextBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                var allBacklogText = string.Join("\n", MainWindowUtilities.Backlog);
                if (MainTextBox.Text != allBacklogText)
                {
                    if (MainTextBox.GetFirstVisibleLineIndex() == 0)
                    {
                        int caretIndex = allBacklogText.Length - MainTextBox.Text.Length;
                        MainTextBox.Text =
                            "Character count: " + string.Join("", MainWindowUtilities.Backlog).Length + "\n"
                            + allBacklogText;
                        MainTextBox.Foreground = ConfigManager.MainWindowBacklogTextColor;

                        if (caretIndex >= 0)
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

            if (Background.Opacity == 0)
                Background.Opacity = OpacitySlider.Value / 100;

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

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ConfigManager.SaveBeforeClosing();
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Background.Opacity = OpacitySlider.Value / 100;
        }

        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MainTextBox.FontSize = FontSizeSlider.Value;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (Utils.KeyGestureComparer(e, ConfigManager.ShowPreferencesWindowKeyGesture))
            {
                MainWindowUtilities.ShowPreferencesWindow();
            }

            else if (Utils.KeyGestureComparer(e, ConfigManager.MousePassThroughModeKeyGesture))
            {
                Background.Opacity = 0;
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
                if (ConfigManager.Ready)
                    MainWindowUtilities.ShowAddNameWindow();
            }

            else if (Utils.KeyGestureComparer(e, ConfigManager.ShowAddWordWindowKeyGesture))
            {
                if (ConfigManager.Ready)
                    MainWindowUtilities.ShowAddWordWindow();
            }

            else if (Utils.KeyGestureComparer(e, ConfigManager.ShowManageDictionariesWindowKeyGesture))
            {
                if (ConfigManager.Ready)
                    ManageDictionariesWindow.Instance.ShowDialog();
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

        private void ShowManageDictionariesWindow(object sender, RoutedEventArgs e)
        {
            ManageDictionariesWindow.Instance.ShowDialog();
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
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

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ConfigManager.MainWindowHeight = Height;
            ConfigManager.MainWindowWidth = Width;
        }

        private void MainTextBox_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            AddNameButton.IsEnabled = ConfigManager.Ready;
            AddWordButton.IsEnabled = ConfigManager.Ready;
            ManageDictionariesButton.IsEnabled = ConfigManager.Ready;
        }

        private void MainTextBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!ConfigManager.LookupOnSelectOnly
                || Background.Opacity == 0
                || ConfigManager.InactiveLookupMode) return;

            //if (ConfigManager.RequireLookupKeyPress
            //    && !Keyboard.Modifiers.HasFlag(ConfigManager.LookupKey))
            //    return;

            FirstPopupWindow.LookupOnSelect(MainTextBox);
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            FirstPopupWindow.MiningMode = false;
            foreach (var popupWindow in Application.Current.Windows.OfType<PopupWindow>().ToList())
            {
                popupWindow.Hide();
            }
        }
    }
}