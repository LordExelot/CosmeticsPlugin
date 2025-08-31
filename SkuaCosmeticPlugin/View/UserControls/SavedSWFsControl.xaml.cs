using Newtonsoft.Json;
using Skua.Core.Models.Items;
using System.Collections;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Skua.Core.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Net.Sockets;
using static Skua_CosmeticPlugin.CosmeticsMainWindow;

namespace Skua_CosmeticPlugin.View.UserControls
{
    /// <summary>
    /// Interaction logic for SavedSWFsControl.xaml
    /// </summary>
    public partial class SavedSWFsControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static SavedSWFsControl Instance { get; } = new();

        private List<TreeViewItem> _allSets = new();
        private string _searchFilter = string.Empty;
        private string[]? PlayerAPIData;

        public SavedSWFsControl()
        {
            InitializeComponent();
            DataContext = this;

            _allSets = MakeList();
            FilterSets();
            Rune.Title.Text = "Ground Rune";
        }

        private static List<TreeViewItem> MakeList()
        {
            try
            {
                if (!File.Exists(System.IO.Path.Combine(DirectoryPath, "Sets.json")))
                    return new();

                List<Set>? sets = JsonConvert.DeserializeObject<List<Set>>(File.ReadAllText(System.IO.Path.Combine(DirectoryPath, "Sets.json")));
                if (sets == null)
                    return new();

                List<TreeViewItem> toReturn = new();

                foreach (var set in sets.OrderBy(x => x.Index))
                {
                    var setItems = new List<TreeViewItem>();
                    foreach (var item in set.Items)
                    {
                        bool manual = item.ID == 0;
                        bool offHand = item.offHand == true;
                        setItems.Add(new TreeViewItem()
                        {
                            Header = $"({item.Category})\t{item.name}" +
                                    (manual || offHand ? "\n" : String.Empty) +
                                    (manual ? "[Manual]" : String.Empty) +
                                    (manual && offHand ? "\t" : String.Empty) +
                                    (offHand ? "[Offhand]" : String.Empty),
                            ItemsSource =
                            (
                                manual ?
                                    new List<TreeViewItem>()
                                    {
                                        new TreeViewItem() { Header = "Name\t" + item.name, Padding = itemDataPadding },
                                        new TreeViewItem() { Header = "sFile\t" + item.Path, Padding = itemDataPadding },
                                        new TreeViewItem() { Header = "sLink\t" + item.Link, Padding = itemDataPadding },
                                        new TreeViewItem() { Header = "category\t" + item.CategoryString, Padding = itemDataPadding }
                                    }
                                     :
                                    new List<TreeViewItem>()
                                    {
                                        new TreeViewItem() { Header = "Name\t" + item.name, Padding = itemDataPadding },
                                        new TreeViewItem() { Header = "ID\t" + item.ID, Padding = itemDataPadding },
                                        new TreeViewItem() { Header = "sFile\t" + item.Path, Padding = itemDataPadding },
                                        new TreeViewItem() { Header = "sLink\t" + item.Link, Padding = itemDataPadding },
                                        new TreeViewItem() { Header = "category\t" + item.CategoryString, Padding = itemDataPadding }
                                    }

                            )
                        });
                    }

                    toReturn.Add(new TreeViewItem()
                    {
                        Header = set.Name,
                        ItemsSource = setItems
                    });
                }

                // Add new set option
                toReturn.Add(new TreeViewItem()
                {
                    Header = "Add new set",
                });

                return toReturn;
            }
            catch
            {
                Bot.ShowMessageBox("Invalid JSON in \"Skua/plugins/options/Cosmetics Plugin/Sets.json\"", "Error");
                return new();
            }
        }

        private static readonly Thickness itemDataPadding = new(0, 1, 0, 1);

        // Currently equipped items
        private SWF? _equippedHelm;
        public SWF EquippedHelm
        {
            get { return _equippedHelm ??= GetEquippedItemAPI(ItemCategory.Helm); }
            set { _equippedHelm = value; }
        }

        private SWF? _equippedArmor;
        public SWF EquippedArmor
        {
            get { return _equippedArmor ??= GetEquippedItemAPI(ItemCategory.Armor); }
            set { _equippedArmor = value; }
        }

        private SWF? _equippedCape;
        public SWF EquippedCape
        {
            get { return _equippedCape ??= GetEquippedItemAPI(ItemCategory.Cape); }
            set { _equippedCape = value; }
        }

        private SWF? _equippedWeapon1;
        public SWF EquippedWeapon1
        {
            get { return _equippedWeapon1 ??= GetEquippedItemAPI(ItemCategory.Sword); }
            set { _equippedWeapon1 = value; }
        }

        private SWF? _equippedWeapon2;
        public SWF EquippedWeapon2
        {
            get { return _equippedWeapon2 ??= GetEquippedItemAPI(ItemCategory.Sword); }
            set { _equippedWeapon2 = value; }
        }

        private SWF? _equippedPet;
        public SWF EquippedPet
        {
            get { return _equippedPet ??= GetEquippedItemAPI(ItemCategory.Pet); }
            set { _equippedPet = value; }
        }

        private SWF? _equippedRune;
        public SWF EquippedRune
        {
            get { return _equippedRune ??= GetEquippedItemAPI(ItemCategory.Misc); }
            set { _equippedRune = value; }
        }

        private SWF GetEquippedItemAPI(ItemCategory cat)
        {
            PlayerAPIData ??= AddSWFControl.getAPIData(Bot.Player.Username).Result;
            if (PlayerAPIData == null)
                return new();

            string itemPath = (cat) switch
            {
                ItemCategory.Armor => AddSWFControl.ParseAPIData(PlayerAPIData, "strArmorFile"),
                ItemCategory.Cape => AddSWFControl.ParseAPIData(PlayerAPIData, "strCapeFile"),
                ItemCategory.Helm => AddSWFControl.ParseAPIData(PlayerAPIData, "strHelmFile"),
                ItemCategory.Misc => AddSWFControl.ParseAPIData(PlayerAPIData, "strMiscFile"),
                ItemCategory.Pet => AddSWFControl.ParseAPIData(PlayerAPIData, "strPetFile"),
                _ => AddSWFControl.ParseAPIData(PlayerAPIData, "strWeaponFile"),
            };

            string itemName = (cat) switch
            {
                ItemCategory.Armor => AddSWFControl.ParseAPIData(PlayerAPIData, "strArmorName"),
                ItemCategory.Cape => AddSWFControl.ParseAPIData(PlayerAPIData, "strCapeName"),
                ItemCategory.Helm => AddSWFControl.ParseAPIData(PlayerAPIData, "strHelmName"),
                ItemCategory.Misc => AddSWFControl.ParseAPIData(PlayerAPIData, "strMiscName"),
                ItemCategory.Pet => AddSWFControl.ParseAPIData(PlayerAPIData, "strPetName"),
                _ => AddSWFControl.ParseAPIData(PlayerAPIData, "strWeaponName"),
            };

            var items = Bot.Inventory.Items.FindAll(x => x.Name.ToLower().Trim() == itemName.ToLower().Trim() && x.FileLink == itemPath);
            if (items == null || !items.Any())
                return new();

            if (CurrentSWFs != null && items.Select(x => new SWF(x)).Except(CurrentSWFs).Any())
                AddCtrl.SilentPost(AddSWFControl.DataType.Inventory);

            return items.Select(x => new SWF(x)).First(x => CurrentSWFs!.Contains(x));
        }

        private void SavedSetsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {

                if (e.NewValue is TreeViewItem item)
                {

                    if (item.Header.ToString() == "Add new set")
                    {
                        SelectedSet = null;
                        SetNameTextBox.Text = string.Empty;
                        UpdateSetItemControlsForAddNewSet();
                        return;
                    }

                    var selectedSet = GetSetFromTreeViewSelection();
                    if (selectedSet != null)
                    {
                        SelectedSet = selectedSet;
                        SetNameTextBox.Text = selectedSet.Name;
                        Logger("Sets", $"Selected set '{selectedSet.Name}' - showing set preview in SetItem controls");
                    }
                    else
                    {
                        Logger("WARNING", "Could not determine selected set from TreeView - GetSetFromTreeViewSelection returned null");
                        SelectedSet = null;
                        SetNameTextBox.Text = string.Empty;
                    }
                }
                else
                {
                    SelectedSet = null;
                    SetNameTextBox.Text = string.Empty;
                    Logger("Sets", "No set selected - SetItem controls show currently loaded items");
                }

            }
            catch (Exception ex)
            {
                Logger("ERROR", $"TreeView selection changed error: {ex.Message}");
                Logger("ERROR", $"Stack trace: {ex.StackTrace}");
                SelectedSet = null;
                SetNameTextBox.Text = string.Empty;
            }
        }


        private Set? GetSetFromTreeViewSelection()
        {
            try
            {
                var selectedItem = SavedSetsTreeView.SelectedItem as TreeViewItem;

                if (selectedItem == null)
                {
                    return null;
                }

                // Skip "Add new set" item
                if (selectedItem.Header.ToString() == "Add new set")
                {
                    return null;
                }

                bool isTopLevelSet = false;

                if (SavedSetsTreeView.ItemsSource != null)
                {
                    var itemsSource = SavedSetsTreeView.ItemsSource as IEnumerable<TreeViewItem>;
                    if (itemsSource != null)
                    {
                        isTopLevelSet = itemsSource.Contains(selectedItem);

                        if (!isTopLevelSet && selectedItem.Items.Count > 0)
                        {
                            isTopLevelSet = true;
                        }
                    }
                }

                if (isTopLevelSet)
                {
                    try
                    {
                        var set = new Set(selectedItem);
                        return set;
                    }
                    catch (Exception setEx)
                    {
                        Logger("ERROR", $"GetSetFromTreeViewSelection - failed to create Set from TreeViewItem: {setEx.Message}");
                        return null;
                    }
                }

                // Find parent set for child items
                TreeViewItem? parentSet = null;
                DependencyObject? parent = VisualTreeHelper.GetParent(selectedItem);

                while (parent != null && parent is not TreeView)
                {
                    if (parent is TreeViewItem tvi)
                    {
                        if (SavedSetsTreeView.ItemsSource != null)
                        {
                            var itemsSource = SavedSetsTreeView.ItemsSource as IEnumerable<TreeViewItem>;
                            if (itemsSource?.Contains(tvi) == true)
                            {
                                parentSet = tvi;
                                break;
                            }
                        }
                    }
                    parent = VisualTreeHelper.GetParent(parent);
                }

                if (parentSet != null)
                {
                    try
                    {
                        var set = new Set(parentSet);
                        return set;
                    }
                    catch (Exception setEx)
                    {
                        Logger("ERROR", $"GetSetFromTreeViewSelection - failed to create Set from parent TreeViewItem: {setEx.Message}");
                        return null;
                    }
                }

                Logger("WARNING", "GetSetFromTreeViewSelection - could not find parent set for selected TreeView item");
                return null;
            }
            catch (Exception ex)
            {
                Logger("ERROR", $"GetSetFromTreeViewSelection - exception: {ex.Message}");
                return null;
            }
        }

        private Set? _selectedSet;
        public Set? SelectedSet
        {
            get { return _selectedSet; }
            set
            {
                _selectedSet = value;
                OnPropertyChanged();

                // Notify all Display properties
                OnPropertyChanged(nameof(DisplayHelm));
                OnPropertyChanged(nameof(DisplayArmor));
                OnPropertyChanged(nameof(DisplayCape));
                OnPropertyChanged(nameof(DisplayWeapon));
                OnPropertyChanged(nameof(DisplayPet));
                OnPropertyChanged(nameof(DisplayRune));
            }
        }

        // Properties for currently loaded items
        private SetItem? _currentHelm;
        public SetItem? CurrentHelm
        {
            get { return _currentHelm; }
            set
            {
                _currentHelm = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayHelm));
            }
        }

        private SetItem? _currentArmor;
        public SetItem? CurrentArmor
        {
            get { return _currentArmor; }
            set
            {
                _currentArmor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayArmor));
            }
        }

        private SetItem? _currentCape;
        public SetItem? CurrentCape
        {
            get { return _currentCape; }
            set
            {
                _currentCape = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayCape));
            }
        }

        private SetItem? _currentWeapon;
        public SetItem? CurrentWeapon
        {
            get { return _currentWeapon; }
            set
            {
                _currentWeapon = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayWeapon));
            }
        }

        private SetItem? _currentPet;
        public SetItem? CurrentPet
        {
            get { return _currentPet; }
            set
            {
                _currentPet = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayPet));
            }
        }

        private SetItem? _currentRune;
        public SetItem? CurrentRune
        {
            get { return _currentRune; }
            set
            {
                _currentRune = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayRune));
            }
        }

        // Display properties that return either selected set items or current items
        public SetItem? DisplayHelm => CurrentHelm;
        public SetItem? DisplayArmor => CurrentArmor;
        public SetItem? DisplayCape => CurrentCape;
        public SetItem? DisplayWeapon => CurrentWeapon;
        public SetItem? DisplayPet => CurrentPet;
        public SetItem? DisplayRune => CurrentRune;


#nullable disable
        public partial class Set
        {
            [JsonProperty("index")]
            public int Index { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("items")]
            public List<SetItemData> Items { get; set; }

#nullable enable
            // UI helper properties - not serialized
            [JsonIgnore]
            public SetItem? Helm
            {
                get
                {
                    var itemData = Items.Find(x => x.Category == ItemCategory.Helm);
                    return itemData != null ? ConvertToSetItem(itemData) : null;
                }
                set
                {
                    Items.RemoveAll(x => x.Category == ItemCategory.Helm);
                    if (value != null)
                        Items.Add(ConvertToSetItemData(value));
                }
            }

            [JsonIgnore]
            public SetItem? Armor
            {
                get
                {
                    var itemData = Items.Find(x => x.Category == ItemCategory.Armor || x.Category == ItemCategory.Class);
                    return itemData != null ? ConvertToSetItem(itemData) : null;
                }
                set
                {
                    Items.RemoveAll(x => x.Category == ItemCategory.Armor || x.Category == ItemCategory.Class);
                    if (value != null)
                        Items.Add(ConvertToSetItemData(value));
                }
            }

            [JsonIgnore]
            public SetItem? Cape
            {
                get
                {
                    var itemData = Items.Find(x => x.Category == ItemCategory.Cape);
                    return itemData != null ? ConvertToSetItem(itemData) : null;
                }
                set
                {
                    Items.RemoveAll(x => x.Category == ItemCategory.Cape);
                    if (value != null)
                        Items.Add(ConvertToSetItemData(value));
                }
            }

            [JsonIgnore]
            public SetItem? Weapon
            {
                get
                {
                    var itemData = Items.Find(x => WeaponCategories.Contains(x.Category));
                    return itemData != null ? ConvertToSetItem(itemData) : null;
                }
                set
                {
                    Items.RemoveAll(x => WeaponCategories.Contains(x.Category));
                    if (value != null)
                        Items.Add(ConvertToSetItemData(value));
                }
            }

            [JsonIgnore]
            public SetItem? Pet
            {
                get
                {
                    var itemData = Items.Find(x => x.Category == ItemCategory.Pet);
                    return itemData != null ? ConvertToSetItem(itemData) : null;
                }
                set
                {
                    Items.RemoveAll(x => x.Category == ItemCategory.Pet);
                    if (value != null)
                        Items.Add(ConvertToSetItemData(value));
                }
            }

            [JsonIgnore]
            public SetItem? Rune
            {
                get
                {
                    var itemData = Items.Find(x => x.Category == ItemCategory.Misc);
                    return itemData != null ? ConvertToSetItem(itemData) : null;
                }
                set
                {
                    Items.RemoveAll(x => x.Category == ItemCategory.Misc);
                    if (value != null)
                        Items.Add(ConvertToSetItemData(value));
                }
            }

            private SetItem ConvertToSetItem(SetItemData itemData)
            {
                return new SetItem
                {
                    ID = itemData.ID,
                    name = itemData.name,
                    Path = itemData.Path,
                    Link = itemData.Link,
                    CategoryString = itemData.CategoryString,
                    offHand = itemData.offHand
                };
            }

            private SetItemData ConvertToSetItemData(SetItem setItem)
            {
                return new SetItemData
                {
                    ID = setItem.ID,
                    name = setItem.name,
                    Path = setItem.Path,
                    Link = setItem.Link,
                    CategoryString = setItem.CategoryString,
                    offHand = setItem.offHand
                };
            }

            public Set() { }

            public Set(TreeViewItem tree)
            {
                // Get the index
                int index = -1;
                if (Instance.SavedSetsTreeView.ItemsSource != null)
                {
                    var itemsSource = Instance.SavedSetsTreeView.ItemsSource as IList<TreeViewItem>;
                    if (itemsSource != null)
                    {
                        index = itemsSource.IndexOf(tree);
                    }
                }
                else
                {
                    index = Instance.SavedSetsTreeView.Items.IndexOf(tree);
                }

                Index = index >= 0 ? index : 0;
                Name = tree.Header.ToString();
                Items = new();

                // Convert TreeViewItem back to SetItem via dictionary
                foreach (TreeViewItem t1 in tree.Items)
                {
                    Dictionary<string, string> toConvert = new();
                    foreach (TreeViewItem t2 in t1.Items)
                    {
                        // Use t2.Header instead of ToString() for reliability
                        string[] s = t2.Header.ToString().Split('\t');
                        if (s.Length == 2)
                        {
                            string key = s[0] == "Name" ? "name" : s[0];
                            toConvert[key] = s[1];
                        }
                    }
                    Items.Add(JsonConvert.DeserializeObject<SetItemData>(JsonConvert.SerializeObject(toConvert)));
                }
            }

            public override string ToString()
            {
                return $"[{Index}] {Name}\n - {Items.Select(x => x.name).Join("\n - ")}";
            }
        }

        #region SavedItem

        public class SavedItem
        {
            [JsonProperty(nameof(ID))]
            public int ID { get; set; }

            [JsonProperty(nameof(Path))]
            public string Path { get; set; } = string.Empty;

            [JsonConstructor]
            public SavedItem(int ID, string path)
            {
                this.ID = ID;
                Path = path;
            }

            public SavedItem(SWF item)
            {
                ID = item.ID;
                Path = item.Path;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(ID, Path);
            }
        }
#nullable enable
        /// <summary>
        /// Data model class for JSON serialization of set items
        /// </summary>
        public class SetItemData
        {
            [JsonProperty("id")]
            public int ID { get; set; }

            [JsonProperty("name")]
            public string name { get; set; } = string.Empty;

            [JsonProperty("sFile")]
            public string Path { get; set; } = string.Empty;

            [JsonProperty("sLink")]
            public string Link { get; set; } = string.Empty;

            [JsonProperty("category")]
            public string CategoryString { get; set; } = string.Empty;

            [JsonProperty("offHand")]
            public bool? offHand { get; set; }

            private ItemCategory? _category = null;
            [JsonIgnore]
            public ItemCategory Category
            {
                get
                {
                    return _category ??= Enum.TryParse(CategoryString, true, out ItemCategory result) ? result : ItemCategory.Unknown;
                }
            }

            public SetItemData() { }

            public SetItemData(SWF item, bool? isOffHand = null)
            {
                ID = item.ID;
                name = item.Name;
                Path = item.Path;
                Link = item.Link;
                CategoryString = item.CategoryString;
                _category = item.Category;
                offHand = isOffHand;
            }
        }

        private List<SavedItem>? _ScannedItems;
        private List<SavedItem>? _ContributedItems;
        private static readonly string ScannedItemsPath = System.IO.Path.Combine(DirectoryPath, "ScannedItems.json");
        private static readonly string ContributedItemsPath = System.IO.Path.Combine(DirectoryPath, "ContributedItems.json");

        public List<SavedItem> ScannedItems
        {
            get
            {
                return _ScannedItems ??= File.Exists(ScannedItemsPath) ?
                    JsonConvert.DeserializeObject<List<SavedItem>>(JsonConvert.SerializeObject(JsonConvert.DeserializeObject<dynamic[]>(File.ReadAllText(ScannedItemsPath))))! :
                    new();
            }
            set
            {
                _ScannedItems = value;
                Directory.CreateDirectory(DirectoryPath);
                File.WriteAllText(ScannedItemsPath, JsonConvert.SerializeObject(value, Formatting.Indented));
                OptionsCtrl.StatScanCount.Text = $"{value.Count} [{GetPercentage(value)}%]";
            }
        }

        public void UpdateScannedItems(IEnumerable<SWF> swfs)
            => Dispatcher.Invoke(() => ScannedItems = ScannedItems.Union(swfs.Select(x => new SavedItem(x))).ToList());

        public void UpdateScannedItems(int ID, string path)
            => Dispatcher.Invoke(() => ScannedItems = ScannedItems.Union(Enumerable.Repeat<SavedItem>(new(ID, path), 1)).ToList());

        public List<SavedItem> ContributedItems
        {
            get
            {
                return _ContributedItems ??= File.Exists(ContributedItemsPath) ?
                    JsonConvert.DeserializeObject<List<SavedItem>>(JsonConvert.SerializeObject(JsonConvert.DeserializeObject<dynamic[]>(File.ReadAllText(ContributedItemsPath))))! :
                    new();
            }
            set
            {
                _ContributedItems = value;
                Directory.CreateDirectory(DirectoryPath);
                File.WriteAllText(ContributedItemsPath, JsonConvert.SerializeObject(value, Formatting.Indented));
                OptionsCtrl.StatContributeCount.Text = $"{value.Count} [{GetPercentage(value)}%]";
            }
        }

        public void UpdateContributedItems(IEnumerable<SWF> swfs)
            => Dispatcher.Invoke(() => ContributedItems = ContributedItems.Union(swfs.Select(x => new SavedItem(x))).ToList());

        public void UpdateContributedItems(int ID, string path)
            => Dispatcher.Invoke(() => ContributedItems = ContributedItems.Union(Enumerable.Repeat<SavedItem>(new(ID, path), 1)).ToList());

        public static double GetPercentage(ICollection list)
        {
            return Math.Round((float)list.Count / (float)CurrentSWFs!.Count * 1000) / 10;
        }

        #endregion

        private void FilterSets()
        {
            var filteredSets = _allSets.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(_searchFilter))
            {
                filteredSets = filteredSets.Where(x =>
                    x.Header.ToString().Contains(_searchFilter, StringComparison.OrdinalIgnoreCase));
            }

            SavedSetsTreeView.ItemsSource = filteredSets.ToList();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                _searchFilter = textBox.Text;
                FilterSets();
            }
        }

        private void AddSetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string setName = SetNameTextBox.Text.Trim();
                if (string.IsNullOrEmpty(setName))
                {
                    Bot.ShowMessageBox("Please enter a set name", "Add Set");
                    return;
                }

                LoadCurrentlyEquippedItems();

                var newSet = new Set
                {
                    Name = setName,
                    Index = GetNextSetIndex(),
                    Items = new List<SetItemData>()
                };

                Logger("Sets", $"Current items state: Helm={CurrentHelm?.name}, Armor={CurrentArmor?.name}, Cape={CurrentCape?.name}, Weapon={CurrentWeapon?.name}, Pet={CurrentPet?.name}, Rune={CurrentRune?.name}");

                // Add items in order: helmet, armor, cape, weapon, pet, rune
                AddEquippedItemToSet(newSet, ItemCategory.Helm);
                AddEquippedItemToSet(newSet, ItemCategory.Armor);
                AddEquippedItemToSet(newSet, ItemCategory.Cape);

                // Add first available weapon
                foreach (var weaponCategory in WeaponCategories)
                {
                    AddEquippedItemToSet(newSet, weaponCategory);
                    break;
                }

                AddEquippedItemToSet(newSet, ItemCategory.Pet);
                AddEquippedItemToSet(newSet, ItemCategory.Misc); // Rune

                if (newSet.Items.Count == 0)
                {
                    Bot.ShowMessageBox("No items found to save in the set.\n\nTo create a set:\n1. Go to the Load Items tab\n2. Load some items to your character\n3. Return to the Sets tab\n4. Click 'Add new set' and enter a name", "No Items to Save");
                    return;
                }

                SaveSetToFile(newSet);

                Logger("Sets", $"Added new set: {setName} with {newSet.Items.Count} items");
                Bot.ShowMessageBox($"Successfully created set '{setName}' with {newSet.Items.Count} items!", "Set Created");
                SetNameTextBox.Text = string.Empty;

                // Refresh the sets list
                _allSets = MakeList();
                FilterSets();
            }
            catch (Exception ex)
            {
                Logger("ERROR", $"Failed to add set: {ex.Message}");
                Bot.ShowMessageBox($"Failed to create set: {ex.Message}", "Error");
            }
        }

        private void AddEquippedItemToSet(Set set, ItemCategory category)
        {
            try
            {
                SetItem? equippedItem = category switch
                {
                    ItemCategory.Helm => CurrentHelm,
                    ItemCategory.Armor or ItemCategory.Class => CurrentArmor,
                    ItemCategory.Cape => CurrentCape,
                    ItemCategory.Pet => CurrentPet,
                    ItemCategory.Misc => CurrentRune,
                    _ when WeaponCategories.Contains(category) => CurrentWeapon,
                    _ => null
                };

                if (equippedItem != null)
                {
                    var itemData = new SetItemData
                    {
                        ID = equippedItem.ID,
                        name = equippedItem.name,
                        Path = equippedItem.Path,
                        Link = equippedItem.Link,
                        CategoryString = equippedItem.CategoryString,
                        offHand = equippedItem.offHand
                    };

                    set.Items.Add(itemData);
                    Logger("Sets", $"Added {category} item to set {set.Name}: {equippedItem.name}");
                }
                else
                {
                    Logger("Sets", $"No {category} item available to add to set {set.Name}");
                }
            }
            catch (Exception ex)
            {
                Logger("ERROR", $"Failed to add {category} item to set: {ex.Message}");
            }
        }

        private void PinSetButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSet == null)
            {
                Bot.ShowMessageBox("Please select a set to pin", "Pin Set");
                return;
            }

            try
            {
                // Load existing sets
                var setsPath = System.IO.Path.Combine(DirectoryPath, "Sets.json");
                var existingSets = new List<Set>();
                if (File.Exists(setsPath))
                {
                    var existingJson = File.ReadAllText(setsPath);
                    existingSets = JsonConvert.DeserializeObject<List<Set>>(existingJson) ?? new List<Set>();
                }

                // Find and remove the selected set
                var toPin = existingSets.FirstOrDefault(s => s.Name == SelectedSet.Name);
                if (toPin != null)
                {
                    existingSets.Remove(toPin);
                    // Insert at the top
                    existingSets.Insert(0, toPin);

                    // Update indices (optional, if you use Index for ordering)
                    for (int i = 0; i < existingSets.Count; i++)
                        existingSets[i].Index = i;

                    // Save updated list
                    File.WriteAllText(setsPath, JsonConvert.SerializeObject(existingSets, Formatting.Indented));
                    Logger("Sets", $"Pinned set: {SelectedSet.Name}");

                    // Refresh UI
                    _allSets = MakeList();
                    FilterSets();
                }
                else
                {
                    Bot.ShowMessageBox("Set not found in file.", "Pin Set");
                }
            }
            catch (Exception ex)
            {
                Logger("ERROR", $"Failed to pin set: {ex.Message}");
                Bot.ShowMessageBox($"Failed to pin set: {ex.Message}", "Error");
            }
        }


        private void DeleteSetButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSet == null)
            {
                Bot.ShowMessageBox("Please select a set to delete", "Delete Set");
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete the set '{SelectedSet.Name}'?",
                "Delete Set", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                // Load existing sets
                var setsPath = System.IO.Path.Combine(DirectoryPath, "Sets.json");
                var existingSets = new List<Set>();
                if (File.Exists(setsPath))
                {
                    var existingJson = File.ReadAllText(setsPath);
                    existingSets = JsonConvert.DeserializeObject<List<Set>>(existingJson) ?? new List<Set>();
                }

                // Remove the selected set
                var toRemove = existingSets.FirstOrDefault(s => s.Name == SelectedSet.Name);
                if (toRemove != null)
                {
                    existingSets.Remove(toRemove);

                    // Save updated list
                    File.WriteAllText(setsPath, JsonConvert.SerializeObject(existingSets, Formatting.Indented));
                    Logger("Sets", $"Deleted set: {SelectedSet.Name}");

                    // Refresh UI
                    _allSets = MakeList();
                    FilterSets();
                    SelectedSet = null;
                    SetNameTextBox.Text = string.Empty;
                }
                else
                {
                    Bot.ShowMessageBox("Set not found in file.", "Delete Set");
                }
            }
            catch (Exception ex)
            {
                Logger("ERROR", $"Failed to delete set: {ex.Message}");
                Bot.ShowMessageBox($"Failed to delete set: {ex.Message}", "Error");
            }
        }


        private void UpdateSetItemControlsForAddNewSet()
        {
            Helm.Title.Text = "Helm";
            Armor.Title.Text = "Armor";
            Cape.Title.Text = "Cape";
            Weapon.Title.Text = "Weapon";
            Pet.Title.Text = "Pet";
            Rune.Title.Text = "Ground Rune";

            SelectedSet = null;
            LoadCurrentlyEquippedItems();

            Logger("Sets", "Updated SetItem controls for 'Add new set' - showing currently loaded items");
        }

        private void LoadCurrentlyEquippedItems()
        {
            // No need to scan inventory; just ensure UI is bound to current SetItem properties.
            // If you want to force a UI refresh, you can call OnPropertyChanged for each property:
            OnPropertyChanged(nameof(CurrentHelm));
            OnPropertyChanged(nameof(CurrentArmor));
            OnPropertyChanged(nameof(CurrentCape));
            OnPropertyChanged(nameof(CurrentWeapon));
            OnPropertyChanged(nameof(CurrentPet));
            OnPropertyChanged(nameof(CurrentRune));
            Logger("Sets", "Refreshed SetItem controls to currently loaded items.");
        }


        private void UpdateCurrentItemFromSelectedItem(ItemCategory category)
        {
            try
            {
                var loadedItem = GetCurrentlyLoadedItem(category);

                switch (category)
                {
                    case ItemCategory.Helm:
                        CurrentHelm = loadedItem;
                        break;
                    case ItemCategory.Armor:
                    case ItemCategory.Class:
                        CurrentArmor = loadedItem;
                        break;
                    case ItemCategory.Cape:
                        CurrentCape = loadedItem;
                        break;
                    case ItemCategory.Pet:
                        CurrentPet = loadedItem;
                        break;
                    case ItemCategory.Misc:
                        CurrentRune = loadedItem;
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger("ERROR", $"Failed to update current {category} item: {ex.Message}");
            }
        }

        private SetItem? GetCurrentlyLoadedItem(ItemCategory category)
        {
            // Just return the current SetItem property for the category
            return category switch
            {
                ItemCategory.Helm => CurrentHelm,
                ItemCategory.Armor or ItemCategory.Class => CurrentArmor,
                ItemCategory.Cape => CurrentCape,
                ItemCategory.Pet => CurrentPet,
                ItemCategory.Misc => CurrentRune,
                _ when WeaponCategories.Contains(category) => CurrentWeapon,
                _ => null
            };
        }

        public void RefreshCurrentlyLoadedItems()
        {
            if (SelectedSet == null)
            {
                LoadCurrentlyEquippedItems();
            }
        }

        public void UpdateLoadedItem(SWF loadedItem)
        {
            try
            {

                var setItem = new SetItem
                {
                    ID = loadedItem.ID,
                    name = loadedItem.Name,
                    Path = loadedItem.Path,
                    Link = loadedItem.Link,
                    CategoryString = loadedItem.Category.ToString()
                };

                switch (loadedItem.Category)
                {
                    case ItemCategory.Helm:
                        CurrentHelm = setItem;
                        Logger("Sets", $"Updated current Helm: {loadedItem.Name}");
                        break;
                    case ItemCategory.Armor:
                    case ItemCategory.Class:
                        CurrentArmor = setItem;
                        Logger("Sets", $"Updated current Armor: {loadedItem.Name}");
                        break;
                    case ItemCategory.Cape:
                        CurrentCape = setItem;
                        Logger("Sets", $"Updated current Cape: {loadedItem.Name}");
                        break;
                    case ItemCategory.Pet:
                        CurrentPet = setItem;
                        Logger("Sets", $"Updated current Pet: {loadedItem.Name}");
                        break;
                    case ItemCategory.Misc:
                        CurrentRune = setItem;
                        Logger("Sets", $"Updated current Rune: {loadedItem.Name}");
                        break;
                    default:
                        if (WeaponCategories.Contains(loadedItem.Category))
                        {
                            CurrentWeapon = setItem;
                            Logger("Sets", $"Updated current Weapon: {loadedItem.Name}");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger("ERROR", $"Failed to update loaded item in Sets tab: {ex.Message}");
            }
        }


        private void SetUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelectedSet == null)
                {
                    Bot.ShowMessageBox("Please select a set to update", "Update Set");
                    return;
                }

                string setName = SelectedSet.Name;
                Logger("Sets", $"Updating set '{setName}' with currently loaded items");

                var updatedSet = new Set
                {
                    Name = setName,
                    Index = SelectedSet.Index,
                    Items = new List<SetItemData>()
                };

                // Add items in order
                AddEquippedItemToSet(updatedSet, ItemCategory.Helm);
                AddEquippedItemToSet(updatedSet, ItemCategory.Armor);
                AddEquippedItemToSet(updatedSet, ItemCategory.Cape);

                foreach (var weaponCategory in WeaponCategories)
                {
                    AddEquippedItemToSet(updatedSet, weaponCategory);
                    break;
                }

                AddEquippedItemToSet(updatedSet, ItemCategory.Pet);
                AddEquippedItemToSet(updatedSet, ItemCategory.Misc);

                UpdateSetInFile(updatedSet);

                SelectedSet = updatedSet;

                Logger("Sets", $"Updated set '{setName}' with {updatedSet.Items.Count} items");
                Bot.ShowMessageBox($"Set '{setName}' updated with currently loaded items ({updatedSet.Items.Count} items)", "Updated");

                _allSets = MakeList();
                FilterSets();
            }
            catch (Exception ex)
            {
                Logger("ERROR", $"Failed to update set: {ex.Message}");
            }
        }

        private async void SetLoadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check if a child cosmetic is selected
                var selectedItem = SavedSetsTreeView.SelectedItem as TreeViewItem;
                SetItemData? singleItemData = null;

                // If selectedItem is not a top-level set, try to extract SetItemData
                if (selectedItem != null)
                {
                    // Top-level set: Header matches a set name and has children
                    bool isTopLevelSet = selectedItem.Items.Count > 0;
                    if (!isTopLevelSet)
                    {
                        singleItemData = GetSetItemDataFromTreeViewItem(selectedItem);
                    }
                }

                if (singleItemData != null)
                {
                    // Load only the selected cosmetic
                    if (!Bot.Player.LoggedIn)
                    {
                        Bot.ShowMessageBox("You must be logged in to load a cosmetic.", "Load Cosmetic");
                        return;
                    }
                    if (!Bot.Map.Loaded)
                    {
                        Bot.ShowMessageBox("Please wait for the map to load before loading a cosmetic.", "Load Cosmetic");
                        return;
                    }
                    SetLoadButton.IsEnabled = false;
                    LoadItemToCharacterFromData(singleItemData);
                    SetLoadButton.IsEnabled = true;
                    Bot.ShowMessageBox($"Loaded cosmetic '{singleItemData.name}' to character.", "Load Cosmetic");
                    return;
                }

                // Otherwise, load the whole set as before
                if (SelectedSet == null)
                {
                    Bot.ShowMessageBox("Please select a set to load", "Load Set");
                    return;
                }

                if (!Bot.Player.LoggedIn)
                {
                    Bot.ShowMessageBox("You must be logged in to load a set.", "Load Set");
                    return;
                }
                if (!Bot.Map.Loaded)
                {
                    Bot.ShowMessageBox("Please wait for the map to load before loading a set.", "Load Set");
                    return;
                }

                SetLoadButton.IsEnabled = false;
                int loadedCount = 0;
                foreach (var item in SelectedSet.Items)
                {
                    try
                    {
                        if (item.ID > 0)
                        {
                            LoadItemToCharacterFromData(item);
                            loadedCount++;
                            await Task.Delay(100);
                        }
                        else
                        {
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger("ERROR", $"Failed to load item {item.name}: {ex.Message}");
                    }
                }
                SetLoadButton.IsEnabled = true;
                Bot.ShowMessageBox($"Loaded {loadedCount} items from set '{SelectedSet.Name}' to character", "Load Set");
            }
            catch (Exception ex)
            {
                Logger("ERROR", $"Failed to load set: {ex.Message}");
                SetLoadButton.IsEnabled = true;
            }
        }

        private void LoadItemToCharacterFromData(SetItemData item)
        {
            try
            {
                var swfItem = new SWF
                {
                    ID = item.ID,
                    Name = item.name,
                    Path = item.Path,
                    Link = item.Link,
                    CategoryString = item.CategoryString,
                    ItemGroup = GetItemGroupFromCategory(item.Category),
                    Meta = ""
                };

                Logger("Sets", $"Loading item to character: {item.name} (ID: {item.ID}, Category: {item.Category}, ItemGroup: {swfItem.ItemGroup}, Path: {item.Path})");

                LoadSWFWindow.Instance.LoadSWF(swfItem, item.offHand ?? false);
            }
            catch (Exception ex)
            {
                Logger("ERROR", $"Failed to load item to character: {ex.Message}");
                Logger("ERROR", $"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private string GetItemGroupFromCategory(ItemCategory category)
        {
            return category switch
            {
                ItemCategory.Helm => "he",
                ItemCategory.Armor => "co",
                ItemCategory.Class => "co",
                ItemCategory.Cape => "ba",
                ItemCategory.Pet => "pe",
                ItemCategory.Misc => "mi",
                _ when WeaponCategories.Contains(category) => "Weapon",
                _ => "Unknown"
            };
        }

        private void SaveSetToFile(Set newSet)
        {
            try
            {
                var setsPath = System.IO.Path.Combine(DirectoryPath, "Sets.json");
                List<Set> existingSets = new();

                Logger("Sets", $"Attempting to save set: {newSet.Name} with {newSet.Items.Count} items");
                foreach (var item in newSet.Items)
                {
                    Logger("Sets", $"  Item: {item.name} (ID: {item.ID}, Category: {item.CategoryString})");
                }

                if (File.Exists(setsPath))
                {
                    var existingJson = File.ReadAllText(setsPath);
                    existingSets = JsonConvert.DeserializeObject<List<Set>>(existingJson) ?? new List<Set>();
                }

                existingSets.Add(newSet);

                Directory.CreateDirectory(DirectoryPath);

                string jsonString;
                try
                {
                    jsonString = JsonConvert.SerializeObject(existingSets, Formatting.Indented);
                    Logger("Sets", $"JSON serialization successful. Length: {jsonString.Length}");
                }
                catch (JsonException jsonEx)
                {
                    Logger("ERROR", $"JSON serialization failed: {jsonEx.Message}");
                    throw;
                }

                File.WriteAllText(setsPath, jsonString);

                Logger("Sets", $"Saved set '{newSet.Name}' to file: {setsPath}");

                // Verify file was written correctly
                var verifyJson = File.ReadAllText(setsPath);
                var verifyParse = JsonConvert.DeserializeObject<List<Set>>(verifyJson);
                Logger("Sets", $"File verification successful. Contains {verifyParse?.Count ?? 0} sets");
            }
            catch (Exception ex)
            {
                Logger("ERROR", $"Failed to save set to file: {ex.Message}");
                Logger("ERROR", $"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private void UpdateSetInFile(Set updatedSet)
        {
            try
            {
                var setsPath = System.IO.Path.Combine(DirectoryPath, "Sets.json");
                List<Set> existingSets = new();

                if (File.Exists(setsPath))
                {
                    var existingJson = File.ReadAllText(setsPath);
                    existingSets = JsonConvert.DeserializeObject<List<Set>>(existingJson) ?? new List<Set>();
                }

                var existingSet = existingSets.FirstOrDefault(s => s.Name == updatedSet.Name);
                if (existingSet != null)
                {
                    existingSet.Items = updatedSet.Items;
                    Logger("Sets", $"Updated existing set '{updatedSet.Name}' in file");
                }
                else
                {
                    existingSets.Add(updatedSet);
                    Logger("Sets", $"Added new set '{updatedSet.Name}' to file");
                }

                Directory.CreateDirectory(DirectoryPath);
                var jsonString = JsonConvert.SerializeObject(existingSets, Formatting.Indented);
                File.WriteAllText(setsPath, jsonString);
            }
            catch (Exception ex)
            {
                Logger("ERROR", $"Failed to update set in file: {ex.Message}");
                throw;
            }
        }

        private int GetNextSetIndex()
        {
            try
            {
                var setsPath = System.IO.Path.Combine(DirectoryPath, "Sets.json");

                if (!File.Exists(setsPath))
                    return 0;

                var existingJson = File.ReadAllText(setsPath);
                var existingSets = JsonConvert.DeserializeObject<List<Set>>(existingJson) ?? new List<Set>();

                return existingSets.Count > 0 ? existingSets.Max(s => s.Index) + 1 : 0;
            }
            catch (Exception ex)
            {
                Logger("ERROR", $"Failed to get next set index: {ex.Message}");
                return 0;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            AddCtrl.SilentPost(AddSWFControl.DataType.Inventory);
            Loaded -= new RoutedEventHandler(UserControl_Loaded);
        }
        private SetItemData? GetSetItemDataFromTreeViewItem(TreeViewItem item)
        {
            try
            {
                // Parent is the set TreeViewItem
                TreeViewItem? parentSet = null;
                DependencyObject? parent = VisualTreeHelper.GetParent(item);
                while (parent != null && parent is not TreeView)
                {
                    if (parent is TreeViewItem tvi)
                    {
                        parentSet = tvi;
                        break;
                    }
                    parent = VisualTreeHelper.GetParent(parent);
                }
                if (parentSet == null)
                    return null;

                // Find the index of the child in the parent
                int childIndex = parentSet.Items.IndexOf(item);
                if (childIndex < 0)
                    return null;

                // Get the Set from the parent
                var set = new Set(parentSet);
                if (set.Items.Count > childIndex)
                    return set.Items[childIndex];
            }
            catch (Exception ex)
            {
                Logger("ERROR", $"Failed to extract SetItemData: {ex.Message}");
            }
            return null;
        }
    }
}