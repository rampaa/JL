using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using JapaneseLookup.Anki;

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

        public PopupWindow()
        {
            InitializeComponent();
            MaxWidth = int.Parse(ConfigurationManager.AppSettings.Get("PopupMaxWidth") ??
                                 throw new InvalidOperationException());
            MaxHeight = int.Parse(ConfigurationManager.AppSettings.Get("PopupMaxHeight") ??
                                  throw new InvalidOperationException());
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
            string context = null;
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
                if (child.Name == "context")
                {
                    context = child.Text;
                }

                Enum.TryParse(child.Name, out LookupResult result);
                switch (result)
                {
                    case LookupResult.FoundSpelling:
                        foundSpelling = child.Text;
                        break;
                    case LookupResult.Readings:
                        readings = (string) child.Tag;
                        break;
                    // case "context":
                    //     context = child.Text;
                    //     break;
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
            foreach (TextBlock child in bottom.Children)
            {
                Enum.TryParse(child.Name, out LookupResult result);
                switch (result)
                {
                    case LookupResult.Definitions:
                        definitions += child.Text;
                        break;
                    case LookupResult.StrokeCount:
                        strokeCount += child.Text;
                        break;
                    case LookupResult.Grade:
                        grade += child.Text;
                        break;
                    case LookupResult.Composition:
                        composition += child.Text;
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

            // TODO: find a better solution for this that avoids adding an element to Instance.StackPanel
            var mediaElement = new MediaElement { Source = uri, Volume = 1, Visibility = Visibility.Collapsed };
            Instance.StackPanel.Children.Add(mediaElement);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.M:
                {
                    MainWindow.MiningMode = true;
                    PopUpScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                    // TODO: Tell the user that they are in mining mode
                    Instance.Activate();
                    Instance.Focus();

                    break;
                }

                case Key.P:
                {
                    string foundSpelling = null;
                    string reading = null;

                    var innerStackPanel = (StackPanel) StackPanel.Children[_playAudioIndex];
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

                    break;
                }

                case Key.Escape:
                {
                    if (MainWindow.MiningMode)
                    {
                        MainWindow.MiningMode = false;
                        PopUpScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                        Hide();
                    }

                    break;
                }

                case Key.K:
                {
                    ConfigManager.KanjiMode = !ConfigManager.KanjiMode;
                    break;
                }

                case Key.L:
                {
                    MainWindowUtilities.ShowPreferencesWindow();
                    break;
                }

                case Key.N:
                {
                    MainWindowUtilities.ShowAddNameWindow();
                    break;
                }

                case Key.W:
                {
                    MainWindowUtilities.ShowAddWordWindow();
                    break;
                }

                case Key.S:
                {
                    MainWindowUtilities.SearchWithBrowser();
                    break;
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }
}