using System.Windows.Controls;
using static Skua_CosmeticPlugin.CosmeticsMainWindow;

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

        private void ApplySWFButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Main.SelectedTab = LoadCtrl;
        }
        private void AddSWFButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Main.SelectedTab = AddCtrl;
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Main.SelectedTab = RandCtrl;
        }

        private void OptionsButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Main.SelectedTab = OptionsCtrl;
            OptionsCtrl.AssignGender();
        }

        private void SavedSWFButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Main.SelectedTab = SavedCtrl;
        }
    }
}
