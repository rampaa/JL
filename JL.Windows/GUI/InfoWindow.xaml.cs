using System.Windows;

namespace JL.Windows.GUI
{
    /// <summary>
    /// Interaction logic for AbbreviationWindow.xaml
    /// </summary>
    public partial class InfonWindow : Window
    {
        public InfonWindow()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }
}
