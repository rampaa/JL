using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;

namespace JapaneseLookup
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
                PopupWindow.Instance.cardTextBox.Text = parsedWord;
                // TODO: ...lookup(parsedWord);
                // TODO: Show result.
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
    }
}