using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Skua.Core.Interfaces;
using Skua.Core.Models;
using Skua.Core.Models.Items;
using Skua.WPF;
using Skua_CosmeticPlugin.View.UserControls;
using Skua_CosmeticPlugin.Models;
using Skua_CosmeticsPlugin;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Skua_CosmeticPlugin
{
    /// <summary>
    /// Interaction logic for CosmeticsMainWindow.xaml
    /// </summary>
    [INotifyPropertyChanged]
    public sealed partial class CosmeticsMainWindow : CustomWindow
    {
        #region Declirations
        private static CosmeticsMainWindow? _instance;
        public static CosmeticsMainWindow Instance
        {
            get
            {
                return _instance ??= new();
            }
        }
        public static IScriptInterface Bot => IScriptInterface.Instance;
        public static Loader Loader => Loader.Instance;
        public static LoadSWFWindow LoadCtrl => LoadSWFWindow.Instance;
        public static CosmeticsMainWindow Main => CosmeticsMainWindow.Instance;
        public static AddSWFControl AddCtrl => AddSWFControl.Instance;
        public static DataSheet DataCtrl => DataSheet.Instance!;
        public static RandomControl RandCtrl => RandomControl.Instance;
        public static OptionsControl OptionsCtrl => OptionsControl.Instance;
        public static OptionsControl.Settings SettingsCtrl => OptionsControl.Settings.Instance;
        public static SavedSWFsControl SavedCtrl => SavedSWFsControl.Instance;
        public static MenuBar MenuCtrl => MenuBar.Instance;
        public static ManualLoadWindow ManualWin => ManualLoadWindow.Instance;
        public static readonly string DirectoryPath = Path.Combine(ClientFileSources.SkuaPluginsDIR, "options", "Cosmetics Plugin");
        public CosmeticsMainWindow()
        {
            InitializeComponent();
            InitializeTabs();
            DataContext = this;
            WebC.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
        }

        private void InitializeTabs()
        {
            TabItems = new ObservableCollection<Models.TabItem>
            {
                new Models.TabItem("Load Items", LoadCtrl),
                new Models.TabItem("Add Items", AddCtrl),
                new Models.TabItem("Randomizer", RandCtrl),
                new Models.TabItem("Sets", SavedCtrl),
                new Models.TabItem("Options", OptionsCtrl)
            };
            SelectedTabItem = TabItems[0];
        }

        #endregion

        private ObservableCollection<Models.TabItem> _tabItems = new();
        public ObservableCollection<Models.TabItem> TabItems
        {
            get { return _tabItems; }
            set { SetProperty(ref _tabItems, value); }
        }

        private Models.TabItem? _selectedTabItem;
        public Models.TabItem? SelectedTabItem
        {
            get { return _selectedTabItem; }
            set 
            { 
                SetProperty(ref _selectedTabItem, value);
                if (value != null)
                {
                    SelectedTab = value.Content;
                    // Handle special case for Options tab
                    if (value.Content == OptionsCtrl)
                    {
                        OptionsCtrl.AssignGender();
                    }
                }
            }
        }

        //[ObservableProperty]
        private FrameworkElement? _selectedTab;
        public FrameworkElement SelectedTab
        {
            get { return _selectedTab!; }
            set { SetProperty(ref _selectedTab, value); }
        }

        #region SWFs
        public static List<SWF>? CurrentSWFs = null;
        private static SWF? _selectedItem;
        public static SWF? SelectedItem
        {
            get
            {
                return _selectedItem;
            }
            set
            {
                _selectedItem = value;

                LoadCtrl.VisibleRowSelectedWeapon.Height = CosmeticsMainWindow.ShowGridIfTrue(SelectedItem!.ItemGroup == "Weapon");
                LoadCtrl.ItemIsFavorite =
                    SelectedItem != null && FavoriteSWFs != null &&
                    FavoriteSWFs.Any(x => x.ID == SelectedItem.ID && x.Path == SelectedItem.Path);
            }
        }

        public static List<SWF>? swfFilterHelms = null;
        public static List<SWF>? swfFilterArmor = null;
        public static List<SWF>? swfFilterCapes = null;
        public static List<SWF>? swfFilterWeapons = null;
        public static List<SWF>? swfFilterPets = null;
        public static List<SWF>? swfFilterRunes = null;

        public static List<SWF> FavoriteSWFs
        {
            get
            {
                _FavoriteSWFs ??= File.Exists(FavoritesPath) ?
                    JsonConvert.DeserializeObject<List<SWF>>(JsonConvert.SerializeObject(JsonConvert.DeserializeObject<dynamic[]>(File.ReadAllText(FavoritesPath))))! :
                    new();
                return _FavoriteSWFs;
            }
            set
            {
                _FavoriteSWFs = value.OrderBy(x => x.ID).ToList();
                OptionsCtrl.StatFavoriteCount.Text = $"{value.Count}";
                Directory.CreateDirectory(DirectoryPath);
                File.WriteAllText(FavoritesPath, JsonConvert.SerializeObject(_FavoriteSWFs, Formatting.Indented));
            }
        }
        private static List<SWF>? _FavoriteSWFs;
        private static string FavoritesPath = Path.Combine(DirectoryPath, "FavoritedItems.json");
        public class SWF
        {
            [JsonProperty("id")]
            public int ID { get; set; }
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("path")]
            public string Path { get; set; }
            [JsonProperty("link")]
            public string Link { get; set; }

            [JsonProperty("itemGroup")]
            public string ItemGroup { get; set; }

            [JsonProperty("category")]
            public string CategoryString { get; set; }
            private ItemCategory? _category = null;
            [JsonIgnore]
            public ItemCategory Category
            {
                get
                {
                    if (_category is not null)
                        return (ItemCategory)_category;

                    return (ItemCategory)(_category = Enum.TryParse(CategoryString, true, out ItemCategory result) ? result : ItemCategory.Unknown);
                }
            }
            private string? _displayCategory = null;
            public string DisplayCategory
            {
                get
                {
                    if (_displayCategory is not null)
                        return _displayCategory;

                    return _displayCategory = ItemGroup != "Weapon" ? (Category == ItemCategory.Misc ? "Ground Rune" : CategoryString) : (Category == ItemCategory.HandGun ? "Weapon (Hand Gun)" : $"Weapon ({CategoryString})");
                }
            }

            [JsonProperty("meta")]
            public string Meta { get; set; }

            public SWF(string name, int id, string path, string category, string itemGroup, string link, string meta)
            {
                Name = name;
                ID = id;
                Path = path;
                CategoryString = category;
                ItemGroup = itemGroup;
                Link = link;
                Meta = meta;
            }

            public SWF(ItemBase item)
            {
                Name = item.Name;
                ID = item.ID;
                Path = item.FileLink;
                CategoryString = item.CategoryString;
                ItemGroup = item.ItemGroup;
                Link = item.FileName;
                Meta = item.Meta;
            }

            public SWF()
            {
                Name = String.Empty;
                ID = 0;
                Path = String.Empty;
                CategoryString = String.Empty;
                ItemGroup = String.Empty;
                Link = String.Empty;
                Meta = String.Empty;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(ID, Name, Path);
            }

            public override bool Equals(object? obj)
            {
                // Null checks
                if (obj == null)
                    return this == null;
                else if (this == null)
                    return false;

                // Type check
                if (obj is not SWF _obj)
                    return false;

                return (this.ID == _obj.ID) && (this.Name == _obj.Name) && (this.Path == _obj.Path);
            }

            public override string ToString()
            {
                return $"{Name}\t[{ID}]\n{DisplayCategory}\n{Path}";
            }
        }
        #endregion

        #region Logging

        public static void Logger(string caller, string message)
            => Bot.Log($"[{DateTime.Now:HH:mm:ss}] <Cosmetics Plugin> ({caller})  {message}");
        public bool dlEnabled = false;
        public static void DebugLogger(string? extra = null, [CallerMemberName] string? caller = null, [CallerLineNumber] int line = 0)
        {
            lastDebugLog = $"[{DateTime.Now:HH:mm:ss:fff}] <Debug Logger> ({caller})  Line {line}{(extra == null ? String.Empty : " > " + extra)}";
            if (OptionsControl.Settings.Instance.DebugLogger)
                Bot.Log(lastDebugLog);
        }
        public static void DebugLogger(object? extra, [CallerMemberName] string? caller = null, [CallerLineNumber] int line = 0)
        {
            lastDebugLog = $"[{DateTime.Now:HH:mm:ss:fff}] <Debug Logger> ({caller})  Line {line}{(extra == null ? String.Empty : " > " + extra)}";
            if (OptionsControl.Settings.Instance.DebugLogger)
                Bot.Log(lastDebugLog);
        }
        private static string? _lastDebugLog;
        public static string lastDebugLog
        {
            get { return _lastDebugLog ?? "lastDebugLog == NULL"; }
            set { _lastDebugLog = value; }
        }

        #endregion
        #region Utility
        public static HttpClient WebC = new();

        public static string RemoveQuote(string input)
        {
            if (input.StartsWith('"'))
                input = input[1..];
            if (input.EndsWith('"'))
                input = input[..^1];
            return input;
        }

        public static void OpenLink(string? link)
        {
            if (link == null)
                return;
            Process.Start("explorer", link);
        }

        public static GridLength ShowGridIfTrue(bool _bool) => new(0, _bool ? GridUnitType.Auto : GridUnitType.Pixel);

        #endregion

        public readonly static ItemCategory[] WeaponCategories =
        {
            ItemCategory.Axe,
            ItemCategory.Bow,
            ItemCategory.Dagger,
            ItemCategory.Gauntlet,
            ItemCategory.Gun,
            ItemCategory.HandGun,
            ItemCategory.Mace,
            ItemCategory.Polearm,
            ItemCategory.Rifle,
            ItemCategory.Staff,
            ItemCategory.Sword,
            ItemCategory.Wand,
            ItemCategory.Whip,
        };
    }
}
