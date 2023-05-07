using Skua.WPF;
using System.Windows;
using static Skua_CosmeticPlugin.CosmeticsMainWindow;

namespace Skua_CosmeticPlugin.View.UserControls
{
    /// <summary>
    /// Interaction logic for ManualLoadWindow.xaml
    /// </summary>
    public partial class ManualLoadWindow : CustomWindow
    {
        public static ManualLoadWindow Instance { get; } = new();
        public ManualLoadWindow()
        {
            InitializeComponent();
        }

        private void LoadManualButton_Click(object sender, RoutedEventArgs e)
        {
            string? category = (ComboBoxCategory.SelectedIndex) switch
            {
                0 => "Helm",
                1 => "Armor",
                2 => "Cape",
                3 or 4 => "Sword",
                5 => "Dagger",
                6 => "Pet",
                7 => "Misc",
                _ => null
            };
            string? itemGroup = (ComboBoxCategory.SelectedIndex) switch
            {
                0 => "he",
                1 => "ar",
                2 => "ba",
                3 or 4 or 5 => "Weapon",
                6 => "pe",
                7 => "mi",
                _ => null
            };
            if (category != null && itemGroup != null)
                LoadCtrl.LoadSWF(new(String.Empty, 0, TextBox_sFile.Text.Trim(), category, itemGroup, TextBox_sLink.Text.Trim(), String.Empty), ComboBoxCategory.SelectedIndex == 4);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}
