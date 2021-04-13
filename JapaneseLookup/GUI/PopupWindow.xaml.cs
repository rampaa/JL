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

namespace JapaneseLookup
{
    /// <summary>
    /// Interaction logic for PopupWindow.xaml
    /// </summary>
    public partial class PopupWindow : Window

    {
        private static PopupWindow instance;

        public static PopupWindow Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new PopupWindow();
                }
                return instance;
            }
        }

        public PopupWindow()
        {
            InitializeComponent();
        }
    }
}
