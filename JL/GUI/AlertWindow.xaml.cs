using System;
using System.Windows;
using System.Windows.Media;
using JL.Utilities;

namespace JL.GUI
{
    public partial class AlertWindow : Window
    {
        public AlertWindow()
        {
            InitializeComponent();
        }

        public void DisplayAlert(AlertLevel alertLevel, string message)
        {
            AlertBorder.BorderBrush = alertLevel switch
            {
                AlertLevel.Error => Brushes.Red,
                AlertLevel.Warning => Brushes.Orange,
                AlertLevel.Information => Brushes.White,
                AlertLevel.Success => Brushes.Green,
                _ => throw new ArgumentOutOfRangeException(nameof(alertLevel), alertLevel, null)
            };
            AlertWindowTextBlock.Text = message;
        }
    }
}
