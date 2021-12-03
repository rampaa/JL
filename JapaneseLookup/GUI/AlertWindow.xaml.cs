using System;
using System.Windows;
using System.Windows.Media;
using JapaneseLookup.Utilities;

namespace JapaneseLookup.GUI
{
    public partial class AlertWindow : Window
    {
        public AlertWindow()
        {
            InitializeComponent();
        }

        public void DisplayAlert(AlertLevel alertLevel, string message)
        {
            AlertWindowTextBox.BorderBrush= alertLevel switch
            {
                AlertLevel.Error => Brushes.Red,
                AlertLevel.Warning => Brushes.Orange,
                AlertLevel.Information => Brushes.White,
                _ => throw new ArgumentOutOfRangeException(nameof(alertLevel), alertLevel, null)
            };
            AlertWindowTextBox.Text = message;
        }
    }
}