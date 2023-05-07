using Newtonsoft.Json;
using Skua.Core.Models.Items;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm;
using System.Windows.Media;
using System.Windows.Threading;
using static Skua_CosmeticPlugin.CosmeticsMainWindow;
using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using Skua.Core.Interfaces;
using System.Net.Sockets;

namespace Skua_CosmeticPlugin.View.UserControls
{
    [INotifyPropertyChanged]
    public partial class DataSheet : UserControl
    {
        public static DataSheet? Instance { get; set; }
        public DataSheet()
        {
            Instance = this;
            DataContext = this;
            InitializeComponent();

            lastItem = Menu_All;
            if (File.Exists(Path.Combine(DirectoryPath, "LocalDatabaseCache.json")))
            {
                ApplyFilter(ref lastItem, JsonConvert.DeserializeObject<List<SWF>>(File.ReadAllText(Path.Combine(DirectoryPath, "LocalDatabaseCache.json"))), true);
            }
            else swfDataGrid.ItemsSource = new List<SWF>() { new("Loading...", 0, String.Empty, "Loading...", String.Empty, String.Empty, String.Empty) };

            RefreshDataGrid();
        }

        #region DataGrid
        private static async Task<bool> FetchSWFs(bool onlyUpdate)
        {
            string? sheet = null;
            var toReturn = new List<SWF>();
            if (!onlyUpdate)
            {
                await Task.Run(async () =>
                {
                    for (int a = 0; a < 10; a++)
                    {
                        try
                        {
                            sheet = await WebC.GetStringAsync(Tokens.GoogleSheetsJsonApiURL);
                            break;
                        }
                        catch (SocketException sEx)
                        {
                            _ = sEx;
                        }
                        catch (Exception ex)
                        {
                            Logger("Fetching SWFs", "Failed: Crash detected during GET-Request >\n" + ex);
                            break;
                        }
                    }
                });
                if (sheet == null)
                {
                    Logger("Fetching SWFs", "Failed: GET-Request returned null");
                    return false;
                }

                toReturn = JsonConvert.DeserializeObject<List<SWF>>(sheet);
                if (toReturn == null)
                {
                    Logger("Fetching SWFs", "Failed: Deserialization of JSON retured null");
                    return false;
                }
            }
            CurrentSWFs = (onlyUpdate ? CurrentSWFs! : toReturn).OrderBy(x => x.ID).ToList();
            swfFilterHelms = CurrentSWFs.Where(x => x.Category == ItemCategory.Helm).ToList();
            swfFilterArmor = CurrentSWFs.Where(x => x.Category == ItemCategory.Armor || x.Category == ItemCategory.Class).ToList();
            swfFilterCapes = CurrentSWFs.Where(x => x.Category == ItemCategory.Cape).ToList();
            swfFilterWeapons = CurrentSWFs.Where(x => x.ItemGroup == "Weapon").ToList();
            swfFilterPets = CurrentSWFs.Where(x => x.Category == ItemCategory.Pet).ToList();
            swfFilterRunes = CurrentSWFs.Where(x => x.Category == ItemCategory.Misc).ToList();

            OptionsCtrl.StatHelmCount.Text = $"{swfFilterHelms.Count} [{SavedSWFsControl.GetPercentage(swfFilterHelms)}%]";
            OptionsCtrl.StatArmorCount.Text = $"{swfFilterArmor.Count} [{SavedSWFsControl.GetPercentage(swfFilterArmor)}%]";
            OptionsCtrl.StatCapeCount.Text = $"{swfFilterCapes.Count} [{SavedSWFsControl.GetPercentage(swfFilterCapes)}%]";
            OptionsCtrl.StatWeaponCount.Text = $"{swfFilterWeapons.Count} [{SavedSWFsControl.GetPercentage(swfFilterWeapons)}%]";
            OptionsCtrl.StatPetCount.Text = $"{swfFilterPets.Count} [{SavedSWFsControl.GetPercentage(swfFilterPets)}%]";
            OptionsCtrl.StatRuneCount.Text = $"{swfFilterRunes.Count} [{SavedSWFsControl.GetPercentage(swfFilterRunes)}%]";

            OptionsCtrl.StatScanCount.Text = $"{SavedCtrl.ScannedItems.Count} [{SavedSWFsControl.GetPercentage(SavedCtrl.ScannedItems)}%]";
            OptionsCtrl.StatContributeCount.Text = $"{SavedCtrl.ContributedItems.Count} [{SavedSWFsControl.GetPercentage(SavedCtrl.ContributedItems)}%]";
            OptionsCtrl.StatFavoriteCount.Text = $"{FavoriteSWFs.Count}";

            if (!onlyUpdate && !SettingsCtrl.CacheCB)
                Logger("Fetching SWFs", toReturn.Count > 0 ? "Done" : "Failed: toReturn.Count is 0 or less");
            return toReturn.Count > 0;
        }
        public void RefreshDataGrid(bool onlyUpdate = false)
        {
            Dispatcher.Invoke(async () =>
            {
                if (!RefreshDataButton.IsEnabled)
                    return;
                RefreshDataButton.IsEnabled = false;

                await FetchSWFs(onlyUpdate);
                OptionsCtrl.UpdateDBCache();
                FilterLib();
                swfDataGrid.Columns[0].SortDirection = ListSortDirection.Ascending;

                OptionsCtrl.StatTotalCount.Text = CurrentSWFs!.Count.ToString();

                RefreshDataButton.IsEnabled = true;
            });
        }

        private void RefreshDataButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshDataGrid();
        }

        #endregion
        #region Filters
        private void ApplyFilter(ref MenuItem menu, IEnumerable<SWF>? list, bool restore = false)
        {
            if (isFiltering || (!restore && menu == lastItem))
                return;

            isFiltering = true;

            // Signifying selected item
            if (lastItem != menu)
            {
                // Resetting FontWeight
                lastItem!.FontWeight = FontWeights.Normal;
                if (!(lastItem!.Parent).ToString()!.StartsWith("System.Windows.Controls.Menu "))
                {
                    MenuItem? parent = ((MenuItem)lastItem!.Parent);
                    if (parent != null)
                    {
                        parent.FontWeight = FontWeights.Normal;
                    }
                }

                // Setting FontWeight
                menu.FontWeight = FontWeights.UltraBlack;
                if (menu.Name.ToString()!.Contains("_Weapons_"))
                {
                    MenuItem? parent = ((MenuItem)menu.Parent);
                    if (parent != null)
                    {
                        Menu_Weapons.FontWeight = FontWeights.UltraBlack;
                        parent.FontWeight = FontWeights.UltraBlack;
                        foreach (var _m in parent.Items)
                        {
                            MenuItem item = (MenuItem)_m;
                            if (item != menu)
                                item.FontWeight = FontWeights.Normal;
                        }
                    }
                }
                else Menu_Weapons.FontWeight = FontWeights.Normal;

                lastItem = menu;
                paginaNr = 1;
            }

            // Applying pagination
            _ = Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    // Calculating pagina data
                    numberOfSWFsPerPage = Int32.TryParse(((ComboBoxItem)NumberOfSWFsCombo.SelectedItem).Content.ToString(), out int result) ? result : (list?.ToList() ?? new List<SWF>()).Count;
                    paginaMax = Math.Max((int)Math.Ceiling((float)(list != null ? list.ToList().Count : 1) / (float)numberOfSWFsPerPage) - (FavoritesShown ? 0 : 1), 1);
                    if (paginaNr > paginaMax)
                        paginaNr = paginaMax;

                    // Generating paginas based on data
                    List<SWF>? filter =
                        paginaNr > 1 ?
                            list?.Skip(paginaNr * numberOfSWFsPerPage).Take(numberOfSWFsPerPage).ToList() :
                            list?.Take(numberOfSWFsPerPage).ToList();
                    filter =
                        filter != null && filter.Any() ?
                            filter :
                            list?.Skip((paginaNr * numberOfSWFsPerPage) - numberOfSWFsPerPage).Take(numberOfSWFsPerPage).ToList();

                    // Assigning filters but keeping sort direction
                    var sortedCol = swfDataGrid.Columns.FirstOrDefault(x => x.SortDirection != null);
                    bool sortedColAcs = sortedCol?.SortDirection == ListSortDirection.Ascending;
                    int sortedColIndex = swfDataGrid.Columns.IndexOf(sortedCol);

                    swfDataGrid.ItemsSource = sortedColIndex switch
                    {
                        1 => sortedColAcs ? filter?.OrderBy(x => x.Name) : filter?.OrderByDescending(x => x.Name),
                        2 => sortedColAcs ? filter?.OrderBy(x => x.DisplayCategory) : filter?.OrderByDescending(x => x.DisplayCategory),
                        _ => sortedColAcs ? filter?.OrderBy(x => x.ID) : filter?.OrderByDescending(x => x.ID),
                    };
                    foreach (var column in swfDataGrid.Columns)
                        column.SortDirection = null;
                    swfDataGrid.Columns[sortedColIndex].SortDirection = sortedColAcs ? ListSortDirection.Ascending : ListSortDirection.Descending;

                    // Updating pagina controls
                    paginationInfo.Content = $"Page {paginaNr} of {paginaMax}";
                    bool isLastPage = paginaNr == paginaMax;
                    btnNext.IsEnabled = !isLastPage;
                    btnLast.IsEnabled = !isLastPage;
                    bool isFirstPage = paginaNr == 1;
                    btnPrev.IsEnabled = !isFirstPage;
                    btnFirst.IsEnabled = !isFirstPage;
                });
            });
            isFiltering = false;
        }
        private bool isFiltering = false;
        private MenuItem? lastItem = null;
        public void setMenuToAll() => lastItem = Menu_All;

        public void FilterLib(object? sender = null)
        {
            bool restore = sender == null;
            MenuItem menu = restore ? lastItem! : (MenuItem)sender!;
            List<SWF>? toFilter = (menu.Name) switch
            {
                "Menu_All" => FavoritesShown ? FavoriteSWFs : CurrentSWFs,
                "Menu_Helms" => FavoritesShown ? FavoriteSWFs?.Where(x => x.Category == ItemCategory.Helm).ToList() : swfFilterHelms,
                "Menu_Armor" => FavoritesShown ? FavoriteSWFs?.Where(x => x.Category == ItemCategory.Armor || x.Category == ItemCategory.Class).ToList() : swfFilterArmor,
                "Menu_Capes" => FavoritesShown ? FavoriteSWFs?.Where(x => x.Category == ItemCategory.Cape).ToList() : swfFilterCapes,
                "Menu_Weapons_All" => FavoritesShown ? FavoriteSWFs?.Where(x => x.ItemGroup == "Weapon").ToList() : swfFilterWeapons,
                "Menu_Pets" => FavoritesShown ? FavoriteSWFs?.Where(x => x.Category == ItemCategory.Pet).ToList() : swfFilterPets,
                "Menu_GroundRunes" => FavoritesShown ? FavoriteSWFs?.Where(x => x.Category == ItemCategory.Misc).ToList() : swfFilterRunes,

                "Menu_Weapons_Melee_All" => FavoritesShown ? FavoriteSWFs?.Where(x => meleeWeapons.Contains(x.Category)).ToList() : swfFilterWeapons!.Where(x => meleeWeapons.Contains(x.Category)).ToList(),
                "Menu_Weapons_Melee_Axes" => FavoritesShown ? FavoriteSWFs?.Where(x => x.Category == ItemCategory.Axe).ToList() : swfFilterWeapons!.Where(x => x.Category == ItemCategory.Staff).ToList(),
                "Menu_Weapons_Melee_Daggers" => FavoritesShown ? FavoriteSWFs?.Where(x => x.Category == ItemCategory.Dagger).ToList() : swfFilterWeapons!.Where(x =>  x.Category == ItemCategory.Dagger).ToList(),
                "Menu_Weapons_Melee_Gauntlets" => FavoritesShown ? FavoriteSWFs?.Where(x => x.Category == ItemCategory.Gauntlet).ToList() : swfFilterWeapons!.Where(x =>  x.Category == ItemCategory.Gauntlet).ToList(),
                "Menu_Weapons_Melee_Maces" => FavoritesShown ? FavoriteSWFs?.Where(x => x.Category == ItemCategory.Mace).ToList() : swfFilterWeapons!.Where(x =>  x.Category == ItemCategory.Mace).ToList(),
                "Menu_Weapons_Melee_Polearms" => FavoritesShown ? FavoriteSWFs?.Where(x => x.Category == ItemCategory.Polearm).ToList() : swfFilterWeapons!.Where(x =>  x.Category == ItemCategory.Polearm).ToList(),
                "Menu_Weapons_Melee_Swords" => FavoritesShown ? FavoriteSWFs?.Where(x => x.Category == ItemCategory.Sword).ToList() : swfFilterWeapons!.Where(x =>  x.Category == ItemCategory.Sword).ToList(),
                "Menu_Weapons_Melee_Whips" => FavoritesShown ? FavoriteSWFs?.Where(x => x.Category == ItemCategory.Whip).ToList() : swfFilterWeapons!.Where(x => x.Category == ItemCategory.Whip).ToList(),

                "Menu_Weapons_Ranged_All" => FavoritesShown ? FavoriteSWFs?.Where(x => rangedWeapons.Contains(x.Category)).ToList() : swfFilterWeapons!.Where(x => rangedWeapons.Contains(x.Category)).ToList(),
                "Menu_Weapons_Ranged_HandGuns" => FavoritesShown ? FavoriteSWFs?.Where(x => x.Category == ItemCategory.HandGun).ToList() : swfFilterWeapons!.Where(x => x.Category == ItemCategory.HandGun).ToList(),
                "Menu_Weapons_Ranged_Bows" => FavoritesShown ? FavoriteSWFs?.Where(x => x.Category == ItemCategory.Bow).ToList() : swfFilterWeapons!.Where(x => x.Category == ItemCategory.Bow).ToList(),
                "Menu_Weapons_Ranged_Guns" =>   FavoritesShown ? FavoriteSWFs?.Where(x => x.Category == ItemCategory.Gun).ToList() : swfFilterWeapons!.Where(x => x.Category == ItemCategory.Gun).ToList(),
                "Menu_Weapons_Ranged_Rifles" => FavoritesShown ? FavoriteSWFs?.Where(x => x.Category == ItemCategory.Rifle).ToList() : swfFilterWeapons!.Where(x => x.Category == ItemCategory.Rifle).ToList(),

                "Menu_Weapons_Magic_All" => FavoritesShown ? FavoriteSWFs?.Where(x => magicWeapons.Contains(x.Category)).ToList() : swfFilterWeapons!.Where(x => magicWeapons.Contains(x.Category)).ToList(),
                "Menu_Weapons_Magic_Staves" => FavoritesShown ? FavoriteSWFs?.Where(x => x.Category == ItemCategory.Staff).ToList() : swfFilterWeapons!.Where(x => x.Category == ItemCategory.Staff).ToList(),
                "Menu_Weapons_Magic_Wands" => FavoritesShown ? FavoriteSWFs?.Where(x => x.Category == ItemCategory.Wand).ToList() : swfFilterWeapons!.Where(x => x.Category == ItemCategory.Wand).ToList(),

                _ => null
            };
            ApplyFilter(ref menu, toFilter?.Where(x => textFilter(x)), restore);

            bool textFilter(SWF x)
            {
                if (String.IsNullOrEmpty(SearchBar.Text))
                    return true;

                // ID Search
                if (Int32.TryParse(SearchBar.Text, out int result))
                    return x.ID.ToString().Contains(SearchBar.Text);

                return stringModify(x.Name).Contains(stringModify(SearchBar.Text));

                string stringModify(string s)
                {
                    string output = String.Empty;
                    foreach(char c in s.ToLower())
                        if (Char.IsLetterOrDigit(c))
                            output += c;
                    return output;
                }
            }
        }

        private void ToggleFavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            FavoritesShown = !FavoritesShown;
            paginaNr = 1;
            FilterLib();
        }

        private bool _favoritesShown = false;
        public bool FavoritesShown
        {
            get { return _favoritesShown; }
            set { SetProperty(ref _favoritesShown, value); }
        }

        private readonly ItemCategory[] meleeWeapons =
        {
            ItemCategory.Axe,
            ItemCategory.Dagger,
            ItemCategory.Gauntlet,
            ItemCategory.Mace,
            ItemCategory.Polearm,
            ItemCategory.Sword,
            ItemCategory.Whip,
        };

        private readonly ItemCategory[] rangedWeapons =
        {
            ItemCategory.Bow,
            ItemCategory.Gun,
            ItemCategory.HandGun,
            ItemCategory.Rifle,
        };

        private readonly ItemCategory[] magicWeapons =
        {
            ItemCategory.Staff,
            ItemCategory.Wand,
        };

        private void ActivateFilter(object sender, RoutedEventArgs e)
        {
            FilterLib(sender);
        }
        private void SearchBarClear_Click(object sender, RoutedEventArgs e)
        {
            SearchBar.Clear();
            SearchBar.Focus();
        }
        private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isSearching)
                return;
            isSearching = true;
            FilterLib();
            isSearching = false;
        }
        private bool isSearching = false;
        #endregion

        private void swfDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedItem = (SWF)swfDataGrid.SelectedItem;
            OnSelectionChanged();
        }

        public void OnSelectionChanged()
        {
            if (SelectedItem == null)
                return;
            LoadCtrl.UpdateDataMenu();
        }

        private void DataGridCell_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            LoadCtrl.LoadSWF(offHand: LoadCtrl.OffhandCheckBox.IsChecked == true);
        }

        #region pagination
        private int paginaNr = 1;
        private int paginaMax;
        private int numberOfSWFsPerPage = 10;
        private enum PagingMode { First = 1, Next = 2, Previous = 3, Last = 4 };

        private void btnFirst_Click(object sender, System.EventArgs e) => Navigate(PagingMode.First);
        private void btnNext_Click(object sender, System.EventArgs e) => Navigate(PagingMode.Next);
        private void btnPrev_Click(object sender, System.EventArgs e) => Navigate(PagingMode.Previous);
        private void btnLast_Click(object sender, System.EventArgs e) => Navigate(PagingMode.Last);
        private void NumberOfSWFsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) 
        {
            if (CurrentSWFs == null)
                return;
            paginaNr = 1;
            FilterLib();
            OptionsControl.Settings.Instance.PaginaSWFsShown = (string)((ComboBoxItem)NumberOfSWFsCombo.SelectedItem).Content;
        }

        private void Navigate(PagingMode mode)
        {
            switch (mode)
            {
                case PagingMode.Next:
                    paginaNr++;
                    break;

                case PagingMode.Previous:
                    paginaNr -= 1;
                    break;

                case PagingMode.First:
                    paginaNr = 1;
                    break;

                case PagingMode.Last:
                    paginaNr = paginaMax;
                    break;
            }
            FilterLib();
        }
        #endregion

        public void NumberOfSWFsCombo_Loaded(object sender, RoutedEventArgs e)
        {
            OptionsCtrl.NumberOfSWFsCombo_Loaded();
        }
    }
}