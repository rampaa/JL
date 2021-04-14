using System.Windows;

namespace JapaneseLookup.GUI
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
