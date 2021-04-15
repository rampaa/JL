using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using JapaneseLookup.Anki;
using JapaneseLookup.Parsers;
using JapaneseLookup.EDICT;
using System.Collections.Generic;

namespace JapaneseLookup.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string backlog = "";
        private IParser parser = new Mecab();

        public MainWindow()
        {
            InitializeComponent();
            JMdictLoader.Loader();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var windowClipboardManager = new ClipboardManager(this);
            windowClipboardManager.ClipboardChanged += ClipboardChanged;
            //CopyFromClipboard();
        }

        private void CopyFromClipboard()
        {
            // Check for Japanese text?
            if (Clipboard.ContainsText())
            {
                try
                {
                    backlog += Clipboard.GetText();
                    mainTextBox.Text = Clipboard.GetText() + "\n";
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

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }

        private void MainTextBox_MouseMove(object sender, MouseEventArgs e)
        {
            int charPosition = mainTextBox.GetCharacterIndexFromPoint(Mouse.GetPosition(mainTextBox), false);
            if (charPosition != -1)
            {
                string parsedWord = parser.Parse(mainTextBox.Text[charPosition..]);
                // TODO: Lookafter and lookbehind.
                // TODO: Show results correctly.
                PopupWindow.Instance.cardTextBox.Text = LookUp(parsedWord);
                Point position = PointToScreen(Mouse.GetPosition(this));
                PopupWindow popUpWindow = PopupWindow.Instance;
                popUpWindow.Left = position.X;
                popUpWindow.Top = position.Y + 30;
                popUpWindow.Show();
            }
            else
            {
                PopupWindow.Instance.Hide();
            }
        }

        private void MainTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void MainTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.M:
                {
                    int charPosition = mainTextBox.GetCharacterIndexFromPoint(Mouse.GetPosition(mainTextBox), false);
                    if (charPosition == -1) return;

                    string parsedWord = parser.Parse(mainTextBox.Text[charPosition..]);
                    // Mining.Mine(parsedWord, "reading", "gloss", "context");
                    Mining.Mine("猫", "ねこ", "gloss", "context");
                    break;
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            PopupWindow.Instance.Close();
        }

        private string LookUp(string parsedWord)
        {
            string result = "";
            
            

            List<Results> temp;

            if (JMdictLoader.jMdictDictionary.TryGetValue(parsedWord, out temp))
            {
                string id = temp[0].Id;
                string def = "";
                string reading = "";
                string alternativeSpellings = "";
                foreach (string d in temp[0].Definitions)
                {
                    def += d + "\t";
                }

                foreach (string r in temp[0].Readings)
                {
                    reading += r + "\t";
                }

                foreach (string s in temp[0].AlternativeSpellings)
                {
                    alternativeSpellings += s + "\t";
                }
                result += def + "\n" + reading + "\n" + alternativeSpellings;
                
            }
            return result;
        }
    }
}