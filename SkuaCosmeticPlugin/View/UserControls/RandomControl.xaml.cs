using System.Windows;
using System.Windows.Controls;
using static Skua_CosmeticPlugin.CosmeticsMainWindow;

namespace Skua_CosmeticPlugin.View.UserControls
{
    /// <summary>
    /// Interaction logic for RandomControl.xaml
    /// </summary>
    public partial class RandomControl : UserControl
    {
        public static RandomControl Instance { get; } = new();
        public RandomControl()
        {
            InitializeComponent();
            DataContext = this;

            RandomHelm.CategoryName.Content = "Random Helm";
            RandomArmor.CategoryName.Content = "Random Armor";
            RandomCape.CategoryName.Content = "Random Cape";
            RandomWeapon1.CategoryName.Content = "Random Weapon";
            RandomWeapon2.CategoryName.Content = "Random Offhand";
            RandomWeapon2.SetCheckBox.IsEnabled = false;
            RandomWeapon2.SetCheckBox.IsChecked = false;
            RandomPet.CategoryName.Content = "Random Pet";
            RandomRune.CategoryName.Content = "Random Ground Rune";
        }

        public void GetRandomItem(RandomItem type)
        {
            if (CurrentSWFs?.Count == 0)
                return;

            SWF? _randomItem = (type.Name) switch
            {
                "RandomHelm" => SelectRandomItem(swfFilterHelms!),
                "RandomArmor" => SelectRandomItem(swfFilterArmor!),
                "RandomCape" => SelectRandomItem(swfFilterCapes!),
                "RandomWeapon1" or "RandomWeapon2" => SelectRandomItem(swfFilterWeapons!),
                "RandomPet" => SelectRandomItem(swfFilterPets!),
                "RandomRune" => SelectRandomItem(swfFilterRunes!),
                _ => null
            };
            if (_randomItem == null)
                return;

            type.randomItem = _randomItem;
            type.ItemName.Text = $"[{_randomItem.ID}] {_randomItem.Name}";
            type.ItemPath.Text = _randomItem.Path;

            if (type.Name == "RandomWeapon1")
            {
                RandomWeapon2.ItemName.Text = String.Empty;
                RandomWeapon2.ItemPath.Text = String.Empty;
            }

            type.ItemIsFavorite = FavoriteSWFs != null && FavoriteSWFs.Any(x => x.ID == _randomItem.ID && x.Path == _randomItem.Path);

            LoadCtrl.LoadSWF(_randomItem, type.Name == "RandomWeapon2");
        }
        private static SWF SelectRandomItem(IEnumerable<SWF> SWFs)
        {
            return SWFs.ToArray()[new Random().Next(0, SWFs.Count())];
        }

        private void RandomizeMultiple(bool onlySelected)
        {
            if (isRandomizing)
                return;
            isRandomizing = true;

            if (!Bot.Player.LoggedIn)
            {
                Logger("ERROR", "Item loading failed: You are not logged in.");
                LoadCtrl.loadAllowed = false;
            }
            if (!Bot.Map.Loaded)
            {
                Logger("ERROR", "Item loading failed: The map is yet not loaded .");
                LoadCtrl.loadAllowed = false;
            }

            foreach (var control in controls())
            {
                if (!onlySelected || control.SetCheckBox.IsChecked == true)
                {
                    GetRandomItem(control);
                }
            }

            //if (ShowOffhandControl.IsChecked == true && RandomWeapon2.SetCheckBox.IsChecked == true)
            //{
            //    Task.Run(() =>
            //    {
            //        DebugLogger();
            //        Task.Delay(1000);
            //        DebugLogger();
            //    });

            //    GetRandomItem(RandomWeapon2);
            //}

            LoadCtrl.loadAllowed = true;
            isRandomizing = false;
        }
        private bool isRandomizing = false;

        private RandomItem[] controls() => new[]
        {
            RandomHelm,
            RandomArmor,
            RandomCape,
            RandomWeapon1,
            RandomPet,
            RandomRune
        };

        private void RandomizeAllButton_Click(object sender, RoutedEventArgs e)
        {
            RandomizeMultiple(false);
        }

        private void RandomizeSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            RandomizeMultiple(true);
        }

        private void ShowOffhandControl_Checked(object sender, RoutedEventArgs e)
        {
            VisibleOnShowOffhandControlChecked.Height = new(1, GridUnitType.Star);
        }
        private void ShowOffhandControl_Unchecked(object sender, RoutedEventArgs e)
        {
            VisibleOnShowOffhandControlChecked.Height = new(0, GridUnitType.Pixel);
        }

        private void TatoButton_Click(object sender, RoutedEventArgs e)
        {
            OpenLink("\"https://www.youtube.com/watch?v=dQw4w9WgXcQ\"");
        }
    }
}
