using System.Windows.Controls;

namespace Skua_CosmeticPlugin.View.UserControls
{
    /// <summary>
    /// Interaction logic for MenuBar.xaml
    /// </summary>
    public partial class MenuBar : UserControl
    {
        public static MenuBar Instance { get; } = new();

        public MenuBar()
        {
            InitializeComponent();
        }
    }
}
