using Newtonsoft.Json;
using Skua.Core.Models.Items;
using System.Collections;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Skua.Core.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Net.Sockets;
using static Skua_CosmeticPlugin.CosmeticsMainWindow;

namespace Skua_CosmeticPlugin.View.UserControls
{
    /// <summary>
    /// Interaction logic for SavedSWFsControl.xaml
    /// </summary>
    public partial class SavedSWFsControl : UserControl
    {
        public static SavedSWFsControl Instance { get; } = new();
        public SavedSWFsControl()
        {
            InitializeComponent();
            DataContext = this;

            SavedSetsTreeView.ItemsSource = MakeList();
            Rune.Title.Text = "Ground Rune";
            Rune.DivisionLine.Height = new(0);

            Helm.DataContext = SelectedSet?.Helm;
            Armor.DataContext = SelectedSet?.Armor;
            Cape.DataContext = SelectedSet?.Cape;
            Weapon.DataContext = SelectedSet?.Weapon;
            Pet.DataContext = SelectedSet?.Pet;
            Rune.DataContext = SelectedSet?.Rune;

            //Loaded += SavedSWFsControl_Loaded;
        }

        private void SavedSWFsControl_Loaded(object sender, RoutedEventArgs e)
        {
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
                            // Set items
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
                                    // Item Data
                                    new TreeViewItem() { Header = "Name\t" + item.name, Padding = itemDataPadding },
                                    new TreeViewItem() { Header = "sFile\t" + item.Path, Padding = itemDataPadding },
                                    new TreeViewItem() { Header = "sLink\t" + item.Link, Padding = itemDataPadding }
                                    } :
                                    new List<TreeViewItem>()
                                    {
                                    // Item Data
                                    new TreeViewItem() { Header = "Name\t" + item.name, Padding = itemDataPadding },
                                    new TreeViewItem() { Header = "ID\t" + item.ID, Padding = itemDataPadding },
                                    new TreeViewItem() { Header = "sFile\t" + item.Path, Padding = itemDataPadding },
                                    new TreeViewItem() { Header = "sLink\t" + item.Link, Padding = itemDataPadding }
                                    }
                            )
                        });
                    }

                    toReturn.Add(new TreeViewItem()
                    {
                        // Main set object
                        Header = set.Name,
                        ItemsSource = setItems
                    });
                }

                // New set option
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
        private string[]? PlayerAPIData;

        private void SavedSetsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem item)
            {
                // Fetching the top most treeview item to get the title set
                DependencyObject? parent = VisualTreeHelper.GetParent(item);
                TreeViewItem? toSet = SavedSetsTreeView.SelectedItem as TreeViewItem;

                while (parent is not TreeView)
                {
                    parent = VisualTreeHelper.GetParent(parent);
                    if (parent is TreeViewItem)
                        toSet = parent as TreeViewItem;
                }

                // Setting the selected item as the found set
                SelectedSet = new(toSet!);
            }
        }

        private Set? _selectedSet;
        public Set? SelectedSet
        {
            get
            {
                return _selectedSet;
            }
            set
            {
                _selectedSet = value;
                Helm = value?.Items.Find(x => x.Category == ItemCategory.Helm);
            }
        }

#nullable disable
        [ObservableObject]
        public partial class Set
        {
            [JsonProperty("index")]
            public int Index { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("items")]
            public List<SetItem> Items { get; set; }
            //{ 
            //    get
            //    {
            //        return new[] { Helm, Armor, Cape, Weapon, Pet, Rune }.Where(x => x != null).ToList();
            //    }
            //}

#nullable enable
            public SetItem? Helm
            {
                get
                {
                    return Items.Find(x => x.Category == ItemCategory.Helm);
                }
                set
                {
                    Items.RemoveAll(x => x.Category == ItemCategory.Helm);
                    Items.Add(value);
                    OnPropertyChanged(nameof(Helm));
                }
            }
            public SetItem? Armor
            {
                get
                {
                    return Items.Find(x => x.Category == ItemCategory.Armor || x.Category == ItemCategory.Class);
                }
                set
                {
                    Items.RemoveAll(x => x.Category == ItemCategory.Armor || x.Category == ItemCategory.Class);
                    Items.Add(value);
                }
            }
            public SetItem? Cape
            {
                get
                {
                    return Items.Find(x => x.Category == ItemCategory.Cape);
                }
                set
                {
                    Items.RemoveAll(x => x.Category == ItemCategory.Cape);
                    Items.Add(value);
                }
            }
            public SetItem? Weapon
            {
                get
                {
                    return Items.Find(x => WeaponCategories.Contains(x.Category));
                }
                set
                {
                    Items.RemoveAll(x => WeaponCategories.Contains(x.Category));
                    Items.Add(value);
                }
            }
            public SetItem? Pet
            {
                get
                {
                    return Items.Find(x => x.Category == ItemCategory.Pet);
                }
                set
                {
                    Items.RemoveAll(x => x.Category == ItemCategory.Pet);
                    Items.Add(value);
                }
            }
            public SetItem? Rune
            {
                get
                {
                    return Items.Find(x => x.Category == ItemCategory.Misc);
                }
                set
                {
                    Items.RemoveAll(x => x.Category == ItemCategory.Misc);
                    Items.Add(value);
                }
            }

            public Set() { }
            public Set(TreeViewItem tree)
            {
                //string toSerialize = "{";
                //toSerialize += $"\"index\":{SavedSWFsControl.Instance.SavedSetsTreeView.Items.IndexOf(tree)}";
                //toSerialize += $"\"name\":{tree.Header}";

                //foreach (TreeViewItem t1 in tree.Items)
                //{
                //    toSerialize += $"\"{t1.Header.ToString()!.Split(')').First()[1..].ToLower()}\":";
                //    Dictionary<string, string> toConvert = new();
                //    foreach (TreeViewItem t2 in t1.Items)
                //    {
                //        string[] s = t2.ToString()[44..^14].Split('\t');
                //        toConvert.Add(s[0] == "Name" ? "name" : s[0], s[1]);
                //    }

                //    toSerialize += JsonConvert.SerializeObject(toConvert)));
                //}

                Index = SavedSWFsControl.Instance.SavedSetsTreeView.Items.IndexOf(tree);
                Name = tree.Header.ToString();
                Items = new();

                // Converting the treeviewitem back into a setItem via dictionary
                foreach (TreeViewItem t1 in tree.Items)
                {
                    Dictionary<string, string> toConvert = new();
                    foreach (TreeViewItem t2 in t1.Items)
                    {
                        string[] s = t2.ToString()[44..^14].Split('\t');
                        toConvert.Add(s[0] == "Name" ? "name" : s[0], s[1]);
                    }
                    Items.Add(JsonConvert.DeserializeObject<SetItem>(JsonConvert.SerializeObject(toConvert)));
                }
                Bot.Log(Items.Count().ToString());
                Bot.Log(Items.Any(x => x.Category == ItemCategory.Helm).ToString());
                //Items.Find(x => x.Category == ItemCategory.Helm);
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
                this.ID = item.ID;
                Path = item.Path;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(ID, Path);
            }

#nullable enable
            public override bool Equals(object? obj)
            {
                // Null checks
                if (obj == null)
                    return this == null;
                else if (this == null)
                    return false;

                // Type check
                if (obj is not SavedItem _obj)
                    return false;

                return (this.ID == _obj.ID) && (this.Path == _obj.Path);
            }
        }

        private List<SavedItem>? _ScannedItems;
        private List<SavedItem>? _ContributedItems;
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
        private static readonly string ScannedItemsPath = System.IO.Path.Combine(DirectoryPath, "ScannedItems.json");

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
        private static readonly string ContributedItemsPath = System.IO.Path.Combine(DirectoryPath, "ContributedItems.json");

        public void UpdateContributedItems(IEnumerable<SWF> swfs)
            => Dispatcher.Invoke(() => ContributedItems = ContributedItems.Union(swfs.Select(x => new SavedItem(x))).ToList());
        public void UpdateContributedItems(int ID, string path)
            => Dispatcher.Invoke(() => ContributedItems = ContributedItems.Union(Enumerable.Repeat<SavedItem>(new(ID, path), 1)).ToList());

        public static double GetPercentage(ICollection list)
        {
            return Math.Round((float)list.Count / (float)CurrentSWFs!.Count * 1000) / 10;
        }


        #endregion

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            AddCtrl.SilentPost(AddSWFControl.DataType.Inventory);
            Loaded -= new RoutedEventHandler(UserControl_Loaded);
        }
    }
}