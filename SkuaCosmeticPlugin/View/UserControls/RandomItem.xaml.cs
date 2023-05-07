using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using static Skua_CosmeticPlugin.CosmeticsMainWindow;

namespace Skua_CosmeticPlugin.View.UserControls
{
    /// <summary>
    /// Interaction logic for RandomItem.xaml
    /// </summary>
    [INotifyPropertyChanged]
    public partial class RandomItem : UserControl
    {
        public RandomItem()
        {
            DataContext = this;
            InitializeComponent();
        }

        private void CategoryName_Click(object sender, RoutedEventArgs e)
        {
            RandCtrl.GetRandomItem(this);
        }

        public SWF? randomItem = null;

        private void LinkBackButton_Click(object sender, RoutedEventArgs e)
        {
            if (randomItem == null)
                return;

            Main.SelectedTab = LoadCtrl;
            DataCtrl.FavoritesShown = false;
            //LoadCtrl.DataSheet.setMenuToAll();
            DataCtrl.SearchBar.Text = randomItem.ID.ToString();
            DataCtrl.FilterLib(DataCtrl.Menu_All);
            DataCtrl.swfDataGrid.SelectedIndex = 0;
            Dispatcher.Invoke(() => SelectedItem = randomItem);
            DebugLogger(randomItem);
            DebugLogger(SelectedItem);
            DebugLogger(SelectedItem == null);
            DataCtrl.OnSelectionChanged();
            DataCtrl.swfDataGrid.Focus();
        }

        private void FavoriteSWFButton_Click(object sender, RoutedEventArgs e)
        {
            if (randomItem == null)
                return;

            var modifiedList = FavoriteSWFs;

            if (!ItemIsFavorite)
                modifiedList.Add(randomItem);
            else
            {
                modifiedList.Remove(randomItem);
                if (DataCtrl.FavoritesShown)
                    DataCtrl.FilterLib();
            }

            ItemIsFavorite = !ItemIsFavorite;
            FavoriteSWFs = modifiedList;
        }

        private bool _itemIsFavorite;
        public bool ItemIsFavorite
        {
            get { return _itemIsFavorite; }
            set { SetProperty(ref _itemIsFavorite, value); }
        }
    }
}