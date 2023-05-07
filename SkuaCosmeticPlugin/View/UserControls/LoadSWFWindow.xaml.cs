using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Skua.Core.Interfaces;
using Skua.Core.Models;
using Skua.Core.Models.Items;
using Skua.Core.Models.Monsters;
using Skua.Core.Utils;
using Skua.Core.ViewModels;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using static Skua_CosmeticPlugin.CosmeticsMainWindow;

namespace Skua_CosmeticPlugin.View.UserControls
{
    /// <summary>
    /// Interaction logic for LoadSWFWindow.xaml
    /// </summary>
    [INotifyPropertyChanged]
    public partial class LoadSWFWindow : UserControl
    {
        public static LoadSWFWindow Instance { get; } = new();
        public LoadSWFWindow()
        {
            DataContext = this;
            InitializeComponent();
            //FavoriteSWFButton.SetBinding(Button.ForegroundProperty, new Binding("Foreground") { Source = Main.ThemeText });
        }

        public void UpdateDataMenu()
        {
            SelectedItem_Name.Text = SelectedItem!.Name;
            SelectedItem_Category.Text = SelectedItem!.DisplayCategory;
            SelectedItem_Path.Text = SelectedItem!.Path;
            SelectedItem_File.Text = SelectedItem!.Link;

            //BindingOperations.ClearBinding(FavoriteSWFButton, Button.ForegroundProperty);
            //FavoriteSWFButton.SetBinding(Button.ForegroundProperty, ItemIsFavorite ? 
            //    new Binding("Background") { Source = Main.ThemeButton } :
            //    new Binding("Foreground") { Source = Main.ThemeText });
        }

        #region Web

        private void OpenWikiButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem == null)
                return;
            OpenLink(wikiURL + SelectedItem.Name);
            Logger("Opening WikiDot", SelectedItem.Name);
        }
        private const string wikiURL = "http://aqwwiki.wikidot.com/";
        #endregion

        #region Loading
        public async void LoadSWF(SWF? selectedItem = null, bool offHand = false)
        {
            await Task.Run(() =>
            {
                if ((selectedItem == null && SelectedItem == null) || !loadAllowed)
                    return;

                if (!Bot.Player.LoggedIn)
                {
                    Logger("ERROR", "Item loading failed: You are not logged in.");
                    return;
                }
                if (!Bot.Map.Loaded)
                {
                    Logger("ERROR", "Item loading failed: The map is yet not loaded .");
                    return;
                }

                SWF item = selectedItem ?? SelectedItem!;

                dynamic t = new ExpandoObject();
                t.sFile = item.Path;
                t.sLink = item.Link;
                t.sType = item.Category == ItemCategory.Class ? "Armor" : item.CategoryString;
                t.sMeta = item.Meta;
                item.ItemGroup = item.ItemGroup == "ar" ? "co" : item.ItemGroup;

                Bot.Wait.ForTrue(() => Bot.Player.Loaded, 20);

                if (offHand && item.ItemGroup == "Weapon")
                {
                    Bot.Flash.SetGameObject($"world.myAvatar.pMC.pAV.objData.eqp.Weapon.sLink", t.sLink);
                    Bot.Flash.CallGameFunction("world.myAvatar.pMC.loadWeaponOff", t.sFile, t.sLink);
                }
                else
                {
                    Bot.Flash.SetGameObject($"world.myAvatar.objData.eqp[{item.ItemGroup}]", t);
                    Bot.Flash.CallGameFunction("world.myAvatar.loadMovieAtES", item.ItemGroup, t.sFile, t.sLink);
                    if (item.Category == ItemCategory.Class || item.Category == ItemCategory.Armor)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (OptionsCtrl.OptionInGameGender.SelectedIndex == 2)
                            {
                                Task.Run(() =>
                                {
                                    Bot.Wait.ForTrue(() => Bot.Player.Loaded, 20);
                                    ChangeGender('R');
                                });
                            }
                        });
                    }
                }
            });
        }
        public bool loadAllowed = true;

        private void FavoriteSWFButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem == null)
                return;
            var modifiedList = FavoriteSWFs;

            if (!ItemIsFavorite)
                modifiedList.Add(SelectedItem);
            else
            {
                modifiedList.Remove(SelectedItem);
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

        private void LoadSWFButton_Click(object sender, RoutedEventArgs e)
        {
            LoadSWF(offHand: OffhandCheckBox.IsChecked == true);
        }

        public void ChangeGender(char gender)
        {
            if (!Bot.Player.LoggedIn)
                return;

            int uid = Bot.Flash.Call<int>("UserID");
            string? _gender = Bot.Flash.Call("gender");
            if (_gender == null || uid == 0)
                return;

            gender = gender == 'R' ? (new Random().Next(0, 2) == 1 ? 'M' : 'F') : gender;
            if (gender == _gender[1])
                return;

            string packet = $"{{\"t\":\"xt\",\"b\":{{\"r\":-1,\"o\":{{\"cmd\":\"genderSwap\",\"bitSuccess\":1,\"gender\":\"{gender}\",\"intCoins\":0,\"uid\":{uid},\"strHairFileName\":\"\",\"HairID\":\"\",\"strHairName\":\"\"}}}}}}";
            Task.Run(() => Bot.Send.ClientPacket(packet, "json"));
        }
        #endregion

        private void ManualLoadButton_Click(object sender, RoutedEventArgs e)
        {
            ManualWin.Show();
            ManualWin.Activate();
        }

        #region ArtistMode

        private void DownloadSWFButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem == null)
                return;

            if (SelectedItem.Category == ItemCategory.Class || SelectedItem.Category == ItemCategory.Armor)
            {
                switch (OptionsControl.Settings.Instance.DownloadSWFGender)
                {
                    case 0: // Male
                        GetSWFFile('M');
                        break;
                    case 1: // Female
                        GetSWFFile('F');
                        break;
                    case 2: // Both
                        string filename = SelectedItem.Path.Split('/').Last();
                        GetSWFFile('M');
                        Logger("Download .SWF", filename + " [Male]");
                        GetSWFFile('F');
                        Logger("Download .SWF", filename[..^4] + " (1).swf [Female]");
                        break;
                }
            }
            else GetSWFFile();

            void GetSWFFile(char? gender = null)
            {
                if (SelectedItem == null)
                    return;

                switch (SelectedItem.Category)
                {
                    case ItemCategory.Class:
                    case ItemCategory.Armor:
                        OpenLink($"{baseURL}classes/{gender}/{SelectedItem.Path}");
                        return;

                    default:
                        OpenLink(baseURL + SelectedItem.Path);
                        return;
                }
            }
        }

        private const string baseURL = "https://game.aq.com/game/gamefiles/";

        private void DownloadAssetsButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem == null)
                return;
            Task.Run(() =>
            {
                string folderName = $"{SelectedItem.ID} - {SelectedItem.Name}";
                if (SelectedItem.ItemGroup == "Weapon")
                {
                    string category = (SelectedItem.Category) switch
                    {
                        ItemCategory.Staff => "Staves",
                        ItemCategory.HandGun => "Hand Guns",
                        _ => Char.ToUpper(SelectedItem.CategoryString[0]) + SelectedItem.CategoryString[1..] + "s"
                    };
                    DecompileSWF(SelectedItem.Path, Path.Combine("Items", "Weapons", category, folderName));
                }
                else
                {
                    switch (SelectedItem.Category)
                    {
                        case ItemCategory.Armor:
                        case ItemCategory.Class:
                            string male = "classes/M/" + SelectedItem.Path;
                            string female = "classes/F/" + SelectedItem.Path;
                            string path = Path.Combine("Items", "Armors", folderName);
                            switch (OptionsControl.Settings.Instance.DownloadSWFGender)
                            {
                                case 0: // Male
                                    DecompileSWF(male, Path.Combine(path, "Male"));
                                    break;
                                case 1: // Female
                                    DecompileSWF(female, Path.Combine(path, "Female"));
                                    break;
                                case 2: // Both
                                    DecompileSWF(male, Path.Combine(path, "Male"));
                                    DecompileSWF(female, Path.Combine(path, "Female"));
                                    break;
                            }
                            break;

                        default:
                            string category = (SelectedItem.Category) switch
                            {
                                ItemCategory.Misc => "Ground Runes",
                                _ => Char.ToUpper(SelectedItem.CategoryString[0]) + SelectedItem.CategoryString[1..] + "s"
                            };
                            DecompileSWF(SelectedItem.Path, Path.Combine("Items", category, folderName));
                            break;
                    }
                }
            });
        }

        private void MapSWFButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(Bot.Map.FilePath))
                return;
            Task.Run(() =>
            {
                OpenLink($"{baseURL}maps/{Bot.Map.FilePath}");
            });
        }

        private void MapAssetsButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(Bot.Map.FilePath))
                return;
            Task.Run(() =>
            {
                DecompileSWF("maps/" + Bot.Map.FilePath, Path.Combine("Maps", Path.Combine(Bot.Map.FilePath.Split('/')[..^1]), $"{Bot.Map.Name} - {Bot.Map.FilePath.Split('/').Last()}"));
            });
        }

        private void MonsterSWFButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                var monsters = SelectMonster();
                if (monsters == null)
                    return;

                foreach (Monster mon in monsters)
                {
                    OpenLink($"{baseURL}mon/{mon.FileName}");
                }
            });
        }

        private void MonsterAssetsButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                var monsters = SelectMonster();
                if (monsters == null)
                    return;

                foreach (Monster mon in monsters)
                {
                    DecompileSWF("mon/" + mon.FileName, Path.Combine("Monsters", mon.Race, $"{mon.ID} - {mon.Name}"));
                }
            });
        }

        private Monster[]? SelectMonster()
        {
            if (!Bot.Player.LoggedIn)
                return null;

            var monData = Bot.Monsters.MapMonsters;
            if (monData == null || monData.Count == 0)
                return null;

            List<string> monsters = new();
            monData = monData.OrderByDescending(m => monData.Count(_m => _m.ID == m.ID)).ToList();

            foreach (var mon in monData)
            {
                string data = $"{monData.Count(m => m.ID == mon.ID)}x [{mon.ID}] {mon.Name} | {mon.FileName}";
                if (!monsters.Contains(data))
                {
                    monsters.Add(data);
                }
            }

            InputDialogViewModel monDiag = new("Monsters in /" + Bot.Map.Name,
                    "Please tell us what monsters you want to parse the files of? (Names/IDs)\n\n" +
                    String.Join('\n', monsters) +
                    "\n\nDont forget to use , (comma) as a divider if you wish to use more\nthan one monster." +
                    "\nIf you wish to fetch all monsters, type 'all' (without the ' )", false);
            if (Ioc.Default.GetRequiredService<IDialogService>().ShowDialog(monDiag) != true)
                return null;

            if (String.IsNullOrEmpty(monDiag.DialogTextInput.Trim()))
            {
                Logger("SelectMonsters", "Your input was empty");
                return null;
            }

            if (monDiag.DialogTextInput.Trim().ToLower() == "all")
            {
                return Bot.Monsters.MapMonsters.ToArray();
            }
            else
            {
                List<Monster> toReturn = new();
                string[] monsterList = monDiag.DialogTextInput.Split(',', StringSplitOptions.TrimEntries);

                List<string> remainder = new();
                foreach (string s in monsterList)
                {
                    if (Int32.TryParse(s, out int monID) && Bot.Monsters.TryGetMonster(monID, out var Monster))
                        toReturn.Add(Monster!);
                    else remainder.Add(s);
                }

                List<string> remainder2 = new();
                foreach (string s in remainder)
                {
                    if (Bot.Monsters.TryGetMonster(s, out var Monster))
                        toReturn.Add(Monster!);
                    else remainder2.Add(s);
                }

                if (remainder2.Count > 0)
                    Logger("SelectMonsters", $"The plugin was unable to find the following monster{(remainder2.Count == 1 ? "" : "s")}: " + String.Join('|', remainder2));

                if (toReturn.Count == 0)
                {
                    Logger("SelectMonsters", "No monsters were found based on your input.");
                    return null;
                }

                return toReturn.ToArray();
            }
        }

        private void DownloadSWF(string fileName, string path)
        {
            fileName = fileName.EndsWith(".swf") ? fileName : fileName + ".swf";
            path = Path.Combine(_cachePath, path);

            string modName = fileName.Split('/').Last();
            Directory.CreateDirectory(path);

            Task.Run(async () =>
            {
                byte[] fileBytes = await HttpClients.GetMapClient.GetByteArrayAsync($"{baseURL}{fileName}");
                await File.WriteAllBytesAsync(Path.Combine(path, modName), fileBytes);
            }).Wait();
        }

        private bool DecompileSWF(string fileName, string path)
        {
            DownloadSWF(fileName, path);

            fileName = fileName.EndsWith(".swf") ? fileName : fileName + ".swf";
            path = Path.Combine(_cachePath, path);

            string modName = fileName.Split('/').Last();
            Directory.CreateDirectory(path);

            var decompile = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    FileName = "powershell.exe",
                    WorkingDirectory = Path.Combine(AppContext.BaseDirectory, "FFDec"),
                    ArgumentList = {
                        "/c",
                        "./ffdec.bat",
                        "-format",
                        "sprite:svg",
                        "-export",
                        "image,shape,sprite,morphshape,sound",
                        @$"""""""{path}""""""",
                        @$"""""""{Path.Combine(path, modName)}"""""""
                    }
                }
            };
            decompile.Start();
            string error = decompile.StandardError.ReadToEnd();
            decompile.WaitForExit();
            if (!string.IsNullOrEmpty(error))
            {
                string errorMsg = $"Error while decompiling the SWF: {error}";
                Trace.WriteLine(errorMsg);
                Bot.ShowMessageBox(errorMsg, "SWF Decompile Error");
            }

            deleteEmptyDIRs(path);
            static void deleteEmptyDIRs(string startDIR)
            {
                foreach (var directory in Directory.GetDirectories(startDIR))
                {
                    deleteEmptyDIRs(directory);
                    if (Directory.GetFileSystemEntries(directory).Length == 0)
                        Directory.Delete(directory, false);
                }
            }
            return Directory.Exists(path);
        }
        private readonly string _cachePath = Path.Combine(ClientFileSources.SkuaPluginsDIR, "options", "Cosmetics Plugin", "Artist Mode");
        #endregion

    }
}
