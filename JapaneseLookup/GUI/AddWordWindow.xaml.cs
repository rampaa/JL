using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace JapaneseLookup.GUI
{
    /// <summary>
    /// Interaction logic for AddWordWindow.xaml
    /// </summary>
    public partial class AddWordWindow : Window
    {
        private static AddWordWindow _instance;
        public static AddWordWindow Instance
        {
            get { return _instance ??= new AddWordWindow(); }
        }

        public AddWordWindow()
        {
            InitializeComponent();
        }
    }
}
