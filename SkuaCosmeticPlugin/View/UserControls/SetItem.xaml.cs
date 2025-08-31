using Newtonsoft.Json;
using Skua.Core.Models.Items;
using System.Windows.Controls;
using System.Windows;
using static Skua_CosmeticPlugin.CosmeticsMainWindow;

namespace Skua_CosmeticPlugin.View.UserControls
{
    /// <summary>
    /// Interaction logic for SetItem.xaml
    /// </summary>
    public partial class SetItem : UserControl
    {
        public SetItem()
        {
            InitializeComponent();
            // DataContext will be set by parent control via binding
            
            // Initialize properties to avoid nullability warnings
            name = string.Empty;
            Path = string.Empty;
            Link = string.Empty;
            CategoryString = string.Empty;
        }

        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty(nameof(name))]
        public string name { get; set; }

        [JsonProperty("sFile")]
        public string Path { get; set; }

        [JsonProperty("sLink")]
        public string Link { get; set; }

        [JsonProperty("category")]
        public string CategoryString { get; set; }
        private ItemCategory? _category = null;
        [JsonIgnore]
        public ItemCategory Category
        {
            get
            {
                return _category ??= Enum.TryParse(CategoryString, true, out ItemCategory result) ? result : ItemCategory.Unknown;
            }
        }
        //[JsonProperty("offHand")]
        public bool? offHand;

        private void LinkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var setItem = DataContext as SetItem;
                if (setItem == null || setItem.ID == 0)
                    return;

                var swfItem = CurrentSWFs?.FirstOrDefault(x => x.ID == setItem.ID);
                if (swfItem == null)
                    return;

                // Switch to Load Items tab
                Main.SelectedTabItem = Main.TabItems.FirstOrDefault(t => t.Content == LoadCtrl);
                DataCtrl.FavoritesShown = false;
                DataCtrl.SearchBar.Text = swfItem.ID.ToString();
                DataCtrl.FilterLib(DataCtrl.Menu_All);
                DataCtrl.swfDataGrid.SelectedItem = swfItem;
                DataCtrl.swfDataGrid.ScrollIntoView(swfItem);
                Dispatcher.Invoke(() => SelectedItem = swfItem);
                DataCtrl.OnSelectionChanged();
                DataCtrl.swfDataGrid.Focus();

                Logger("SetItem", $"Navigated to item: {setItem.name} (ID: {setItem.ID})");
            }
            catch (Exception ex)
            {
                Logger("ERROR", $"Failed to navigate to item: {ex.Message}");
            }
        }




        //public SetItem(SWF item, bool? OffHand = null)
        //{
        //    ID = item.ID;
        //    Name = item.Name;
        //    Path = item.Path;
        //    Link = item.Link;
        //    CategoryString = item.CategoryString;
        //    _category = item.Category;
        //    offHand = OffHand;
        //}
    }
}
