using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using JapaneseLookup.Anki;
using JapaneseLookup.Lookup;
using JapaneseLookup.Utilities;

namespace JapaneseLookup.GUI
{
    /// <summary>
    /// Interaction logic for PopupWindow.xaml
    /// </summary>
    public partial class PopupWindow : Window
    {
        private static PopupWindow _instance;
        private static int _playAudioIndex;

        private static readonly System.Windows.Interop.WindowInteropHelper InteropHelper =
            new(Application.Current.MainWindow!);

        private static readonly System.Windows.Forms.Screen ActiveScreen =
            System.Windows.Forms.Screen.FromHandle(InteropHelper.Handle);

        public static PopupWindow Instance
        {
            get { return _instance ??= new PopupWindow(); }
        }

        public ObservableCollection<StackPanel> ResultStackPanels { get; } = new();

        public PopupWindow()
        {
            InitializeComponent();
            // TODO: do we really need these two lines when we set them up in the ConfigManager?
            MaxWidth = int.Parse(ConfigurationManager.AppSettings.Get("PopupMaxWidth") ??
                                 throw new InvalidOperationException());
            MaxHeight = int.Parse(ConfigurationManager.AppSettings.Get("PopupMaxHeight") ??
                                  throw new InvalidOperationException());

            StackPanel.ItemsSource = ResultStackPanels;
        }

        public void UpdatePosition(Point cursorPosition)
        {
            var needsFlipX = ConfigManager.PopupFlipX && cursorPosition.X + Width > ActiveScreen.Bounds.Width;
            var needsFlipY = ConfigManager.PopupFlipY && cursorPosition.Y + Height > ActiveScreen.Bounds.Height;

            double newLeft;
            double newTop;

            if (needsFlipX)
            {
                // flip Leftwards while preventing -OOB
                newLeft = cursorPosition.X - Width - ConfigManager.PopupXOffset * 2;
                if (newLeft < 0) newLeft = 0;
            }
            else
            {
                // no flip
                newLeft = cursorPosition.X + ConfigManager.PopupXOffset;
            }

            if (needsFlipY)
            {
                // flip Upwards while preventing -OOB
                newTop = cursorPosition.Y - Height - ConfigManager.PopupYOffset * 2;
                if (newTop < 0) newTop = 0;
            }
            else
            {
                // no flip
                newTop = cursorPosition.Y + ConfigManager.PopupYOffset;
            }

            // push if +OOB
            if (newLeft + Width > ActiveScreen.Bounds.Width)
            {
                newLeft = ActiveScreen.Bounds.Width - Width;
            }

            if (newTop + Height > ActiveScreen.Bounds.Height)
            {
                newTop = ActiveScreen.Bounds.Height - Height;
            }

            Left = newLeft;
            Top = newTop;
        }

        public static void FoundSpelling_MouseEnter(object sender, MouseEventArgs e)
        {
            var textBlock = (TextBlock) sender;
            _playAudioIndex = (int) textBlock.Tag;
        }

        public static void FoundSpelling_MouseLeave(object sender, MouseEventArgs e)
        {
            _playAudioIndex = 0;
        }

        public static async void FoundSpelling_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            MainWindow.MiningMode = false;
            Instance.Hide();

            string foundSpelling = null;
            string readings = null;
            string definitions = "";
            string context = PopupWindowUtilities.FindSentence(MainWindow.CurrentText, MainWindow.CurrentCharPosition);
            string foundForm = null;
            string edictID = null;
            var timeLocal = DateTime.Now.ToString("s", CultureInfo.InvariantCulture);
            string alternativeSpellings = null;
            string frequency = null;
            string strokeCount = null;
            string grade = null;
            string composition = null;

            var textBlock = (TextBlock) sender;
            var top = (WrapPanel) textBlock.Parent;
            foreach (TextBlock child in top.Children)
            {
                Enum.TryParse(child.Name, out LookupResult result);
                switch (result)
                {
                    case LookupResult.FoundSpelling:
                        foundSpelling = child.Text;
                        break;
                    case LookupResult.Readings:
                        readings = (string) child.Tag;
                        break;
                    case LookupResult.FoundForm:
                        foundForm = child.Text;
                        break;
                    case LookupResult.EdictID:
                        edictID = child.Text;
                        break;
                    case LookupResult.AlternativeSpellings:
                        alternativeSpellings = (string) child.Tag;
                        break;
                    case LookupResult.Frequency:
                        frequency = child.Text;
                        break;
                    case LookupResult.OnReadings:
                        readings += child.Text + " ";
                        break;
                    case LookupResult.KunReadings:
                        readings += child.Text + " ";
                        break;
                    case LookupResult.Nanori:
                        readings += child.Text + " ";
                        break;
                }
            }

            var innerStackPanel = (StackPanel) top.Parent;
            var bottom = (StackPanel) innerStackPanel.Children[1];
            foreach (object child in bottom.Children)
            {
                if (child is not TextBlock)
                    continue;

                textBlock = (TextBlock) child;

                Enum.TryParse(textBlock.Name, out LookupResult result);
                switch (result)
                {
                    case LookupResult.Definitions:
                        definitions += textBlock.Text;
                        break;
                    case LookupResult.StrokeCount:
                        strokeCount += textBlock.Text;
                        break;
                    case LookupResult.Grade:
                        grade += textBlock.Text;
                        break;
                    case LookupResult.Composition:
                        composition += textBlock.Text;
                        break;
                }
            }

            await Mining.Mine(
                foundSpelling,
                readings,
                definitions,
                context,
                foundForm,
                edictID,
                timeLocal,
                alternativeSpellings,
                frequency,
                strokeCount,
                grade,
                composition
            );
        }

        private static void PlayAudio(string foundSpelling, string reading)
        {
            Debug.WriteLine("Attempting to play audio: " + foundSpelling + " " + reading);

            if (reading == "") reading = foundSpelling;

            Uri uri = new(
                "http://assets.languagepod101.com/dictionary/japanese/audiomp3.php?kanji=" +
                foundSpelling +
                "&kana=" +
                reading
            );

            // var sound = AnkiConnect.GetAudio("猫", "ねこ").Result;

            // TODO: find a better solution for this that has less latency and prevents the noaudio clip from playing
            var mediaElement = new MediaElement { Source = uri, Volume = 1, Visibility = Visibility.Collapsed };
            var mainWindow = Application.Current.Windows.OfType<MainWindow>().First();
            mainWindow.MainGrid.Children.Add(mediaElement);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            
            if (Utils.KeyGestureComparer(e, ConfigManager.MiningModeKeyGesture))
            {
                MainWindow.MiningMode = true;
                PopUpScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                // TODO: Tell the user that they are in mining mode
                Instance.Activate();
                Instance.Focus();

                Instance.ResultStackPanels.Clear();
                PopupWindowUtilities.DisplayResults(true);
            }
            
            else if (Utils.KeyGestureComparer(e, ConfigManager.PlayAudioKeyGesture))
            {
                string foundSpelling = null;
                string reading = null;

                var innerStackPanel = (StackPanel) StackPanel.Items[_playAudioIndex];
                var top = (WrapPanel) innerStackPanel.Children[0];

                foreach (TextBlock child in top.Children)
                {
                    Enum.TryParse(child.Name, out LookupResult result);
                    switch (result)
                    {
                        case LookupResult.FoundSpelling:
                            foundSpelling = child.Text;
                            break;
                        case LookupResult.Readings:
                            reading = ((string) child.Tag).Split(",")[0];
                            break;
                    }
                }

                PlayAudio(foundSpelling, reading);
            }

            else if (e.Key == Key.Escape)
            {
                if (MainWindow.MiningMode)
                {
                    MainWindow.MiningMode = false;
                    PopUpScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    Hide();
                }
            }

            else if (Utils.KeyGestureComparer(e, ConfigManager.KanjiModeKeyGesture))
            {
                ConfigManager.KanjiMode = !ConfigManager.KanjiMode;
                MainWindow.LastWord = "";
                Application.Current.Windows.OfType<MainWindow>().First().MainTextBox_MouseMove(null, null);
            }

            else if (Utils.KeyGestureComparer(e, ConfigManager.ShowPreferencesWindowKeyGesture))
            {
                MainWindowUtilities.ShowPreferencesWindow();
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
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }
}