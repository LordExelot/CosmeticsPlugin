using CommunityToolkit.Mvvm.DependencyInjection;
using Newtonsoft.Json;
//using Google.Apis.Auth.OAuth2;
//using Google.Apis.Services;
//using Google.Apis.Sheets.v4;
using Skua.Core.Interfaces;
using Skua.Core.Models.Items;
using Skua.Core.Models.Servers;
using Skua.Core.Scripts;
using System.Dynamic;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using static Skua_CosmeticPlugin.CosmeticsMainWindow;
//using GoogleData = Google.Apis.Sheets.v4.Data;
using System.Windows.Markup;

namespace Skua_CosmeticPlugin.View.UserControls
{
    /// <summary>
    /// Interaction logic for AddSWFControl.xaml
    /// </summary>
    public partial class AddSWFControl : UserControl
    {
        public static AddSWFControl Instance { get; } = new();
        public AddSWFControl()
        {
            InitializeComponent();
            Loaded += AddSWFControl_Loaded;
        }

        private void AddSWFControl_Loaded(object? sender, EventArgs e)
        {
            SWFCountText.SetBinding(TextBlock.TextProperty, new Binding("Text") { Source = OptionsCtrl.StatTotalCount });
            ScansCountText.SetBinding(TextBlock.TextProperty, new Binding("Text") { Source = OptionsCtrl.StatScanCount });
            ContributionCountText.SetBinding(TextBlock.TextProperty, new Binding("Text") { Source = OptionsCtrl.StatContributeCount });
            Loaded -= AddSWFControl_Loaded;
        }

        private void UploadSWFButton_Click(object sender, RoutedEventArgs e)
        {
            if (_BogActive)
            {
                if (!bogmode && !Bot.Player.LoggedIn)
                {
                    Logger("ERROR", "Bogalj-Mode failed: You are not logged in.");
                    return;
                }

                bogmode = !bogmode;
                if (bogmode)
                {
                    Bot.Wait.ForTrue(() => bogCTS == null, 1000, 200);
                    UploadSWFButton.Content = "Stop Bogalj-Mode | Count: " + bogCount;
                    BogMode();
                }
                else
                {
                    Task.Run(() =>
                    {
                        Bot.Wait.ForTrue(() => bogCTS == null, 1000, 200);
                        Dispatcher.Invoke(() => UploadSWFButton.Content = "Start Bogalj-Mode");
                    });
                    CancelBogMode();
                }
            }
            else
            {
                if (!Bot.Player.LoggedIn)
                {
                    Logger("ERROR", "Adding SWFs failed: You are not logged in.");
                    return;
                }
                if (isPosting)
                    return;

                List<DataType> par = new();

                //if (PostPlayerTarget.IsChecked == true)
                //    par.Add(DataType.PlayerTarget);
                addParam(PostPlayersCheck, PostPlayersButton, DataType.Player);
                addParam(PostInvCheck, PostInvButton, DataType.Inventory);
                addParam(PostDropsCheck, PostDropsButton, DataType.Drops);
                addParam(PostShopCheck, PostShopButton, DataType.Shop);
                addParam(PostQuestCheck, PostQuestButton, DataType.Quest);
                addParam(PostBankCheck, PostBankButton, DataType.Bank);

                if (par.Count == 0)
                    return;

                doingMultiple = true;
                isPosting = true;

                DoPost(par: par.ToArray());

                void addParam(CheckBox checkBox, Button button, DataType type)
                {
                    if (checkBox.IsChecked == true)
                    {
                        runningButtons.Add(button, button.Content.ToString()!);
                        button.Content = "Queued";
                        par.Add(type);
                    }
                }
            }
        }
        private Dictionary<Button, string> runningButtons = new();
        private bool doingMultiple = false; 

        private void PostInvButton_Click(object sender, RoutedEventArgs e) => PostButton(DataType.Inventory);
        private void PostBankButton_Click(object sender, RoutedEventArgs e) => PostButton(DataType.Bank);
        private void PostShopButton_Click(object sender, RoutedEventArgs e) => PostButton(DataType.Shop);
        private void PostQuestButton_Click(object sender, RoutedEventArgs e) => PostButton(DataType.Quest);
        private void PostDropsButton_Click(object sender, RoutedEventArgs e) => PostButton(DataType.Drops);
        private void PostPlayersButton_Click(object sender, RoutedEventArgs e) => PostButton(DataType.Player);

        private void PostButton(DataType type)
        {
            if (!Bot.Player.LoggedIn)
            {
                Logger("ERROR", "Scanning SWFs failed: You are not logged in.");
                return;
            }
            if (isPosting)
                return;
            isPosting = true;

            DoPost(type);
        }

        public void SilentPost(params DataType[] types)
        {
            if (!Bot.Player.LoggedIn || isPosting)
                return;
            isPosting = true;
            silentPost = true;

            DoPost(types);
        }
        private bool silentPost = false;
        public void PassiveParseItemBase(params ItemBase[] items)
        {
            Task.Run(() =>
            {
                Bot.Wait.ForTrue(() => !isPosting, 20);
                isPosting = true;
                silentPost = true;

                if (CurrentSWFs == null)
                    Bot.Wait.ForTrue(() => CurrentSWFs != null, 20);
                if (CurrentSWFs == null || items == null)
                    return;

                List<SWF>? list = new();
                ProcessItems(items.ToList(), ref list);
                UploadSWFs(list);

                silentPost = false;
                isPosting = false;
            });
        }

        public void PassiveParseMapPlayers(string map)
        {
            if (bogmode || bogCTS != null)
                return;

            Task.Run(() =>
            {
                Bot!.Wait.ForMapLoad(map);
                if (Bot.Map.PlayerNames == null || !Bot.Map.PlayerNames.Any())
                    return;

                Bot.Wait.ForTrue(() => !isPosting, 20);
                isPosting = true;
                silentPost = true;

                if (CurrentSWFs == null)
                    Bot.Wait.ForTrue(() => CurrentSWFs != null, 20);
                if (CurrentSWFs == null)
                    return;

                List<SWF>? list = new();
                ParseMapPlayers(ref list);
                UploadSWFs(list);

                silentPost = false;
                isPosting = false;
            });
        }

        private void DoPost(params DataType[] par)
        {
            foreach (var uiElement in ToggleElements)
                uiElement.IsEnabled = false;
            UploadSWFButton.IsEnabled = false;

            buttonProcessingText = "Scanning";
            Task.Run(() =>
            {
                string lastProcText = String.Empty;
                while (isPosting)
                {
                    lastProcText = buttonProcessingText;
                    Dispatcher.Invoke(() => UploadSWFButton.Content = buttonProcessingText);
                    for (int i = 0; i < 3; i++)
                    {
                        for (int t = 0; t < 60; t++)
                        {
                            Thread.Sleep(10);
                            if (!isPosting || lastProcText != buttonProcessingText)
                                break;
                        }
                        if (!isPosting || lastProcText != buttonProcessingText)
                            break;
                        Dispatcher.Invoke(() => UploadSWFButton.Content += ".");
                    }
                    for (int t = 0; t < 60; t++)
                    {
                        Thread.Sleep(10);
                        if (!isPosting || lastProcText != buttonProcessingText)
                            break;
                    }
                };
                Dispatcher.Invoke(() => UploadSWFButton.Content = "Scan Selected Sources for SWFs");
            });

            Task.Run(() =>
            {
                ToggleAntiAFK(true);

                PostData(par);

                ToggleAntiAFK(false);

                Dispatcher.Invoke(() =>
                {
                    foreach (var uiElement in ToggleElements)
                        uiElement.IsEnabled = true;
                    UploadSWFButton.IsEnabled = true;
                    foreach (var kvp in runningButtons)
                        kvp.Key.Content = kvp.Value;
                    runningButtons = new();
                });
                isPosting = false;
                silentPost = false;
                doingMultiple = false;
            });
        }
        public bool isPosting = false;
        private void ManageStatus(Button button, string buttonText)
        {
            if (bogmode || bogCTS != null)
                return;
            statusCTS = new();
            Task.Run(() =>
            {
                Dispatcher.Invoke(() => button.SetBinding(Button.ContentProperty, new Binding("Content") { Source = UploadSWFButton }));
                while (!statusCTS.IsCancellationRequested) { }
                if (doingMultiple)
                {
                    Dispatcher.Invoke(() =>
                    {
                        BindingOperations.ClearBinding(button, Button.ContentProperty);
                        button.Content = "Complete";
                    });
                }
                else
                {
                    Dispatcher.Invoke(() => button.Content = buttonText);
                }
                statusCTS = null;
            });
        }
        private CancellationTokenSource? statusCTS = null;
        private string buttonProcessingText = "Scanning";

        private void BogButton_Click(object sender, RoutedEventArgs e)
        {
            _BogActivateCount++;
            if (_BogActivateCount >= 5 && UploadSWFButton.IsEnabled)
            {
                _BogActive = !_BogActive;
                UploadSWFButton.Content = _BogActive ? "Start Bogalj-Mode" : "Scan Selected Sources for SWFs";
                foreach (var uiElement in ToggleElements)
                    uiElement.IsEnabled = !_BogActive;
            }
        }
        private int _BogActivateCount = 0;
        private bool _BogActive = false;

        private void PostData(params DataType[] type)
        {
            DebugLogger();
            if (CurrentSWFs == null)
                Bot.Wait.ForTrue(() => CurrentSWFs != null, 20);
            if (CurrentSWFs == null)
                return;

            DebugLogger();
            if (type.All(x => x == DataType.Shop) && !Bot.Shops.LoadedCache.Any())
            {
                if (!silentPost)
                    Task.Run(() => Bot.ShowMessageBox("You selected to upload shop data, but no shop data is found.\nPlease load a shop and try again.", "Cosmetics Plugin"));
                return;
            }
            if (type.All(x => x == DataType.Quest) && !Bot.Quests.Tree.Any())
            {
                if (!silentPost)
                    Task.Run(() => Bot.ShowMessageBox("You selected to upload quest item data, but no quest data is found.\nPlease load a quest and try again.", "Cosmetics Plugin"));
                return;
            }

            // Parsing sources
            var list = new List<SWF>();
            foreach (var _type in type)
            {
                switch (_type)
                {
                    case DataType.Inventory:
                        {
                            ManageStatus(PostInvButton, "Scan your Inventory");
                            if (!silentPost)
                                Logger("Parsing", "Inventory Items");

                            ProcessItems(Bot.Inventory.Items.Concat(Bot.TempInv.Items), ref list);

                            StopStatus();
                        }
                        break;

                    case DataType.Bank:
                        {
                            ManageStatus(PostBankButton, "Scan your Bank");
                            if (!silentPost) 
                                Logger("Parsing", "Bank Items");

                            Bot.Bank.Open();
                            Bot.Bank.Loaded = true;
                            Bot.Wait.ForTrue(() => Bot.Bank.Items.Any(), 10);

                            ProcessItems(Bot.Bank.Items, ref list);

                            StopStatus();
                        }
                        break;

                    case DataType.Shop:
                        {
                            if (!Bot.Shops.LoadedCache.Any())
                            {
                                PostShopButton.Content = "Scan the Loaded Shops";
                                break;
                            }

                            ManageStatus(PostShopButton, "Scan the Loaded Shops");

                            var shops = Bot.Shops.LoadedCache.OrderBy(x => x.ID);
                            ProcessItems(shops.Select(s => s.Items).SelectMany(x => x).Concat(shops.Select(s => s.Items.Select(i => i.Requirements)).SelectMany(x => x).SelectMany(x => x)), ref list);

                            if (!silentPost)
                                Logger("Parsing", "Shop Items\n\t" + String.Join("\n\t", shops.Select(q => $"[{q.ID}]\t{q.Name}")));
                            StopStatus();
                        }
                        break;

                    case DataType.Quest:
                        {
                            if (!Bot.Quests.Tree.Any())
                            {
                                PostQuestButton.Content = "Scan All Loaded Quests";
                                break;
                            }

                            ManageStatus(PostQuestButton, "Scan All Loaded Quests");

                            var quests = Bot.Quests.Tree.OrderBy(x => x.ID);
                            ProcessItems(quests.Select(q => q.Rewards.Concat(q.Requirements).Concat(q.AcceptRequirements)).SelectMany(x => x), ref list);

                            if (!silentPost)
                                Logger("Parsing", "Quest Items\n\t" + String.Join("\n\t", quests.Select(q => $"[{q.ID}]\t{q.Name}")));
                            StopStatus();
                        }
                        break;

                    case DataType.Player:
                        {
                            if (Bot.Map.PlayerNames == null || !Bot.Map.PlayerNames.Any())
                            {
                                PostPlayersButton.Content = "Scan the Other Players";
                                return;
                            }

                            ManageStatus(PostPlayersButton, "Scan the Other Players");

                            ParseMapPlayers(ref list);

                            if (!bogmode && bogCTS == null)
                                StopStatus();
                            DebugLogger();
                        }
                        break;

                    case DataType.Drops:
                        {
                            if (Bot.Drops.CurrentDropInfos == null || !Bot.Drops.CurrentDropInfos.Any())
                            {
                                PostPlayersButton.Content = "Scan the Current Drops";
                                break;
                            }

                            DebugLogger();
                            ManageStatus(PostPlayersButton, "Scan the Current Drops");
                            if (!silentPost)
                                Logger("Parsing", "Current Drops");

                            ProcessItems(Bot.Drops.CurrentDropInfos, ref list);

                            StopStatus();
                        }
                        break;
                }
            }

            if (list.Count > 0)
                buttonProcessingText = "Uploading";
            // Posting Data
            DebugLogger();
            UploadSWFs(list);
            DebugLogger();

            // User encouragement string + easter eggs
            string exca = (doneInFull ? list.Count : counter) switch
            {
                0 => "We had these items already. Thank you regardless.",
                1 => "Every item counts!",
                7 => "That's a lucky number, or so I am told. I am a computer, I don't believe in those sorts of things",
                13 => "13... Yikes\t\tBacks away slowly",
                24 or 25 => "You know what is funnier than 24?\t\t\t25!",
                69 => "\t\t\t\t\t\t\t...Nice!",
                42 => "The Answer to the Ultimate Question of Life, the Universe and Everything",
                115 => "Zombie noises.",
                221 => "Nice one, Sherlock.",
                314 => "Hmmmm, pie. Now I'm hungry.",
                343 => "Halo theme-song intensifies!",
                350 => "Dammit monster, I ain't giving you no damn Tree Fiddy",
                420 => "420 BLAZE IT!",
                404 => "ERROR: Joke not found.",
                1101 => "Nobody will ever see this anyway, why did I even write it",
                42069 or 69420 => "You won at life.",
                8008 or 58008 or 80085 or 5318008 or 8008135 => "Hue hue hue, boobies",
                >= 9000 => "IT'S OVER NINE-THOUSAND!!",
                >= 500 => "Make sure to post a screenshot in the discord for bragging rights.",
                >= 400 => "I'm at a loss, you're great",
                >= 250 => "Contributions like this one make the 2000+ I sunk into this plugin worth it.",
                >= 150 => "Holy fuck that is a lot!",
                >= 100 => "Who dah boss, that's right, you dah boss!",
                >= 75 => "You... You are amazing...",
                >= 50 => "Damn, well done",
                >= 25 => "Thank you sooooooo much!",
                >= 10 => "You're helping a lot!",
                _ => "Thank you for contributing!",
            };
            DebugLogger();

            // If there was no failure
            if (doneInFull)
            {
                DebugLogger();
                if (!bogmode && bogCTS == null)
                {
                    if (!silentPost)
                    {
                        Logger("Posting Complete", $"You added {list.Count} new cosmetics to the SWF database! " + exca);
                        Task.Run(() => Bot.ShowMessageBox($"You added {list.Count} new cosmetics to the SWF database!\n" + exca, "Cosmetics Plugin"));
                    }
                }
                else if (list.Count > 0)
                {
                    bogCount += list.Count;
                    Dispatcher.Invoke(() => UploadSWFButton.Content = "Stop Bogalj-Mode | Count: " + bogCount);
                }
                DebugLogger();
            }
            // There was an error
            else
            {
                if (!bogmode && bogCTS == null)
                {
                    if (!silentPost)
                    {
                        Logger("Posting Complete", $"You added {counter - 1} out of {list.Count} new cosmetics to the SWF database! " + exca +
                                        " Please try again in a minute.");
                        Task.Run(() =>
                        {
                            Bot.ShowMessageBox($"You added {counter - 1} out of {list.Count} new cosmetics to the SWF database!\n" + exca +
                                                "\n\nPlease try again in a minute.", "Cosmetics Plugin");
                        });
                    }
                }
                else
                {
                    bogCount += counter == 0 ? 0 : counter - 1;
                    Dispatcher.Invoke(() => UploadSWFButton.Content = "Stop Bogalj-Mode | Count: " + bogCount);
                }
            }

            DebugLogger();

            void StopStatus()
            {
                statusCTS!.Cancel();
                Bot.Wait.ForTrue(() => statusCTS == null, 1000, 200);
            }

        }
        private bool doneInFull = true;
        private int counter = 1;
        private void ProcessItems(IEnumerable<ItemBase> items, ref List<SWF>? list)
        {
            if (items == null)
                return;
            var s = items.Where(x => categoryWhiteList.Contains(x.Category)).Select(x => new SWF(x));
            list?.AddRange(s.Except(CurrentSWFs!).Except(list));
            SavedCtrl.UpdateScannedItems(s);
        }

        public void ParseMapPlayers(ref List<SWF>? list)
        {
            if (Bot.Map.PlayerNames == null || !Bot.Map.PlayerNames.Any())
                return;

            DebugLogger();

            foreach (string playerName in Bot.Map.PlayerNames)
            {
                // Skip user
                if (playerName.ToLower() == Bot.Player.Username.ToLower())
                    continue;

                // Safety measure, as things could fail
                if (Bot.Map.TryGetPlayer(playerName, out var player))
                {
                    // Only log if bog mode is not active
                    if (!bogmode && bogCTS == null && !silentPost)
                    {
                        Logger("Parsing", $"Player Items: [{player!.EntID}] {playerName}");
                    }

                    // Fetch the avatar data
                    var dyn = JsonConvert.DeserializeObject<dynamic?>(Bot.Flash.Call<string>("getAvatar", player!.EntID)!);
                    // As this too, can fail, add failsave
                    if (dyn == null)
                        continue;

                    // Key is itemGroup, the rest is the data
                    Dictionary<string, dynamic> dict = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(JsonConvert.SerializeObject(dyn.eqp));

                    // Fetch missin data with GET-req on API
                    string[]? apiFull = getAPIData(playerName).Result;
                    if (apiFull == null)
                        continue;

                    foreach (KeyValuePair<string, dynamic> kvp in dict)
                    {
                        // Some items appear to have weird values, so this detects and logs them when needed
                        try
                        {
                            // Making sure weird input wont break it
                            int _itemID = 0;
                            if (itemGroupWhitelist.Contains(kvp.Key) && kvp.Value.ItemID != null && Int32.TryParse(kvp.Value.ItemID.ToString(), out _itemID))
                            {
                                // Basic checks to make sure the database doesn't contain the items yet
                                if (!list!.Any(y => y.Path == kvp.Value.sFile.ToString() && y.ID == _itemID) &&
                                    !CurrentSWFs!.Any(o => o.Path == kvp.Value.sFile.ToString() && o.ID == _itemID))
                                {
                                    // Fetch missing data from GET-req on API (Name)
                                    var api = ParseAPI(apiFull, kvp.Key);
                                    if (api == null || api!.Name == null)
                                        continue;

                                    // Skipping classes cuz API data mismatch and skipping datapoints that cant make a matching set based on the sFile
                                    if (kvp.Key != "ar" && (api!.sFile == null || api!.sFile.ToString() != kvp.Value.sFile.ToString()))
                                        continue;

                                    string name = api!.Name.ToString();
                                    list?.Add(new(name, _itemID, kvp.Value.sFile.ToString(), getCategory(kvp), kvp.Key, kvp.Value.sLink.ToString(), kvp.Value.sMeta == null ? "" : kvp.Value.sMeta.ToString())); ;
                                }
                                SavedCtrl.UpdateScannedItems(_itemID, kvp.Value.sFile.ToString());
                            }
                        }
                        catch (Exception e)
                        {
                            Logger("Parsing", $"Failed > {JsonConvert.ToString(kvp.Value)}:\n{e}");
                        }
                    }
                }
            }
            DebugLogger();

            string getCategory(KeyValuePair<string, dynamic> item)
            {
                switch (item.Key)
                {
                    case "ar":
                    case "co":
                        return "Armor";
                    case "ba":
                        return "Cape";
                    case "he":
                        return "Helm";
                    case "mi":
                        return "Misc";
                    case "pe":
                        return "Pet";
                    case "Weapon":
                        return item.Value.sType.ToString();

                    default:
                        Bot.ShowMessageBox("Missing itemgroup in getCategory: " + item.Key, "Cosmetics Plugin");
                        Bot.Stop(true);
                        return null!;
                }
            }

            dynamic ParseAPI(string[] api, string key)
            {
                dynamic toReturn = new ExpandoObject();
                switch (key)
                {
                    case "ar":
                        toReturn.Name = ParseAPIData(api, "strClassName");
                        break;
                    case "ba":
                        toReturn.sFile = ParseAPIData(api, "strCapeFile");
                        toReturn.Name = ParseAPIData(api, "strCapeName");
                        break;
                    case "co":
                        toReturn.sFile = ParseAPIData(api, "strClassFile");
                        toReturn.Name = ParseAPIData(api, "strArmorName");
                        break;
                    case "he":
                        toReturn.sFile = ParseAPIData(api, "strHelmFile");
                        toReturn.Name = ParseAPIData(api, "strHelmName");
                        break;
                    case "mi":
                        toReturn.sFile = ParseAPIData(api, "strMiscFile");
                        toReturn.Name = ParseAPIData(api, "strMiscName");
                        break;
                    case "pe":
                        toReturn.sFile = ParseAPIData(api, "strPetFile");
                        toReturn.Name = ParseAPIData(api, "strPetName");
                        break;
                    case "Weapon":
                        toReturn.sFile = ParseAPIData(api, "strWeaponFile");
                        toReturn.Name = ParseAPIData(api, "strWeaponName");
                        break;

                    default:
                        Bot.ShowMessageBox("Missing itemgroup in getName: " + key, "Cosmetics Plugin");
                        return null!;
                }
                return toReturn;
            }
        }

        public static async Task<string[]?> getAPIData(string playerName)
        {
            string? _data = null;

            await Task.Run(async () =>
            {
                try
                {
                    _data = await WebC.GetStringAsync(
                        "https://game.aq.com/game/" +
                        "api/charpage/fvars?id=" +
                        playerName.ToLower().Replace(" ", "%20")
                    );
                }
                catch { }
            });
            if (_data == null)
                return null;

            return _data.ToString().Split('&', StringSplitOptions.RemoveEmptyEntries);
        }
        public static string ParseAPIData(string[] apiData, string line)
        {
            string toReturn = apiData.First(x => x.StartsWith(line)).Split('=').Last() ?? "";
            if (!toReturn.Contains('%'))
                return toReturn;

            string toReturn2 = string.Empty;
            string encoded = string.Empty;
            bool foundChar = false;
            bool decoding = false;
            foreach (char c in toReturn)
            {
                if (c == '%' || (char.IsDigit(c) && foundChar))
                {
                    foundChar = true;
                    if (c != '%')
                    {
                        decoding = true;
                        encoded += c;
                    }
                }
                else
                {
                    foundChar = false;
                    if (decoding)
                    {
                        toReturn2 += Char.ConvertFromUtf32(Convert.ToInt32(encoded, 16));
                        encoded = string.Empty;
                        decoding = false;
                    }
                    toReturn2 += c;
                }
            }
            return toReturn2;
        }

        public async void UploadSWFs(List<SWF>? list)
        {
            if (list == null || !list.Any()) 
                return;

            DebugLogger();
            if (false)
            {
                DebugLogger();
                await GoogleSheetsAppend(list);
                DebugLogger();
            }
            else
            {
                doneInFull = true;
                counter = 1;

                foreach (var item in list)
                {
                    try
                    {
                        if (!WebC.PostAsync(
                            Tokens.GoogleFormURL,
                            new FormUrlEncodedContent(
                                new Dictionary<string, string>
                                {
                                    {"entry.201636508", item.Name},
                                    {"entry.1836617871", item.ID.ToString()},
                                    {"entry.934285392", item.Path},
                                    {"entry.125292851", item.CategoryString},
                                    {"entry.508744168", item.ItemGroup},
                                    {"entry.156380687", item.Link},
                                    {"entry.1712469548", item.Meta},
                                }
                            )).Result.IsSuccessStatusCode)
                        {
                            doneInFull = false;
                            if (!silentPost)
                                Logger("Posting Failed", "Google API rate-limit reached");
                            break;
                        }
                    }
                    catch
                    {
                        doneInFull = false;
                        if (!silentPost)
                            Logger("Posting Failed", "Google API rate-limit reached");
                        break;
                    }

                    SavedCtrl.UpdateContributedItems(item.ID, item.Path);
                    if (!silentPost)
                        Logger("Posting SWF", $"[{(counter.ToString().Length == 1 && list.Count.ToString().Length != 1 ? "0" : String.Empty)}{counter++}/{list.Count}] {item.Name}");
                }

                // Make sure that the datagrid shows the new items.
                CurrentSWFs?.AddRange(list);
                DataCtrl.RefreshDataGrid(true);
            }
        }

        private static async Task GoogleSheetsAppend(List<SWF>? list)
        {
            //await Task.Run(() =>
            //{
            //    DebugLogger();
            //    SheetsService sheetsService = new(new BaseClientService.Initializer
            //    {
            //        HttpClientInitializer = GetCredential(),
            //        ApplicationName = "Google-SheetsSample/0.1",
            //    });
            //    DebugLogger();

            //    string spreadsheetId = Tokens.GoogleSheetsID;
            //    DebugLogger();

            //    GoogleData.ValueRange requestBody = new();
            //    requestBody.Values.Add(new List<Object>() { "Test1", "Test2", "Test3" });
            //    DebugLogger();

            //    var request = sheetsService.Spreadsheets.Values.Append(requestBody, spreadsheetId, "Data!A2:G");
            //    request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
            //    request.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;
            //    DebugLogger();

            //    var response = request.Execute();
            //    DebugLogger();

            //    Logger("GoogleSheets", JsonConvert.SerializeObject(response));
            //    DebugLogger();

            //    static UserCredential GetCredential()
            //    {
            //        // TODO: Change placeholder below to generate authentication credentials. See:
            //        // https://developers.google.com/sheets/quickstart/dotnet#step_3_set_up_the_sample
            //        //
            //        // Authorize using one of the following scopes:
            //        //     "https://www.googleapis.com/auth/drive"
            //        //     "https://www.googleapis.com/auth/drive.file"
            //        //     "https://www.googleapis.com/auth/spreadsheets"
            //        return null;
            //    }
            //});
        }

        private void BogMode()
        {
            if (!_BogActive)
                return;

            Bot.Stop(true);
            Bot.Auto.Stop();
            Bot.Options.SafeTimings = false;
            Bot.Options.ReloginTryDelay = 1000;
            Bot.Options.AutoRelogin = false;
            bool isMember = Bot.Player.IsMember;

            bogCTS = new();
            _ = Task.Run(async () =>
            {
                manager ??= Ioc.Default.GetRequiredService<IScriptManager>();
                Bot.Wait.ForTrue(() => !Ioc.Default.GetRequiredService<IScriptManager>().ScriptRunning, 30);
                manager.PropertyChanged += ScriptManager_PropertyChanged;

                Logger("Bogalj-Mode", "Initiated");
                if (Bot.Map.Name.ToLower() != "battleon")
                    PostData(DataType.Player);
                int s = 0;
                int rotation = 2;

                while (!bogCTS.IsCancellationRequested)
                {
                    DebugLogger();
                    int battleonCount = 0, yulgarCount = 0;
                    int battleonNr = 1, yulgarNr = 1;
                    DebugLogger();

                    for (int i = 1; i < 4; i++)
                    {
                        foreach (string map in new[] { "battleon", "yulgar" })
                        {
                            Bot.Map.JoinIgnore($"{map}-{i}");
                            await Task.Delay(1000);
                            Bot.Wait.ForMapLoad(map);
                            int playerCount = Bot.Map.PlayerCount;
                            switch (map)
                            {
                                case "battleon":
                                    battleonCount = playerCount;
                                    battleonNr = Int32.Parse(Bot.Map.FullName.Split('-').Last());
                                    break;
                                case "yulgar":
                                    yulgarCount = playerCount;
                                    yulgarNr = Int32.Parse(Bot.Map.FullName.Split('-').Last());
                                    break;
                            }
                            if (playerCount > 1)
                                PostData(DataType.Player);
                            if (bogCTS.IsCancellationRequested)
                                break;
                        }
                        if (bogCTS.IsCancellationRequested || (battleonCount < 7 && yulgarCount < 17 && battleonNr == 1 && yulgarNr == 1))
                            break;
                    }
                    if (bogCTS.IsCancellationRequested)
                        break;
                    DebugLogger();

                    var server = await GetServer();
                    if (Bot.Servers.LastIP == server.IP)
                        continue;
                    DebugLogger();

                    if (!Bot.Servers.EnsureRelogin(server.Name))
                    {
                        DebugLogger();
                        if (bogCTS.IsCancellationRequested)
                            break;
                        DebugLogger();
                        server = await GetServer();
                        DebugLogger();
                        if (!Bot.Servers.EnsureRelogin(server.Name))
                        {
                            DebugLogger();
                            if (bogCTS.IsCancellationRequested)
                                break;
                            DebugLogger();
                            Logger("Bogalj-Mode", "Relogin failed, Bogalj-Mode has been deactivated");
                            DebugLogger();
                            bogCTS.Cancel();
                            DebugLogger();
                            break;
                        }
                        DebugLogger();
                    }
                    DebugLogger();
                    Logger("Bogalj-Mode", "Changed server: " + server.Name);
                    DebugLogger();
                }
                bogmode = false;
                bogCTS = null;

                async Task<Server> GetServer()
                {
                    var servers = (await Bot.Servers.GetServers(true)).ToArray();
                    if (s == servers.Length)
                    {
                        s = 0;
                        Logger("Bogalj-Mode", "Starting Server Rotation " + rotation++);
                    }
                    var server = servers[s++];

                    if (bogInc)
                        return server;
                    bogInc = true;

                    while ((server.PlayerCount >= 1500 || server.Upgrade) && !isMember || server.Name == "Class Test Realm")
                        server = await GetServer();
                    bogInc = false;
                    return server;
                }
            });
        }

        private void ScriptManager_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ScriptRunning" && sender != null && ((ScriptManager)sender).ScriptRunning)
                CancelBogMode();
        }

        private bool bogmode = false;
        private int bogCount = 0;
        private bool bogInc = false;
        private IScriptManager? manager;
        private CancellationTokenSource? bogCTS = null;
        private void CancelBogMode()
        {
            if (bogCTS == null)
                return;
            manager!.PropertyChanged -= ScriptManager_PropertyChanged;
            Task.Run(() =>
            {
                bogCTS?.Cancel();
                Bot.Wait.ForTrue(() => bogCTS == null, 30);
                if (bogCount > 0)
                {
                    Task.Run(() => Bot.ShowMessageBox($"Bogalj-Mode has increased the database by {bogCount} SWF{(bogCount == 1 ? String.Empty : "s")}", "Bogalj-Mode"));
                }
            });
            Dispatcher.Invoke(() => UploadSWFButton.Content = "Start Bogalj-Mode");
        }

        private UIElement[]? _toggleElements;
        private UIElement[] ToggleElements
        {
            get
            {
                return _toggleElements ??= new[]
                {
                    (UIElement)PostInvCheck, PostInvButton,
                    PostBankCheck, PostBankButton,
                    PostQuestCheck, PostQuestButton,
                    PostShopCheck, PostShopButton,
                    PostDropsCheck, PostDropsButton,
                    PostPlayersCheck, PostPlayersButton,
                    //PostPlayerTarget,
                };
            }
        }

        private static void ToggleAntiAFK(bool enable)
        {
            if (enable)
            {
                _AntiAFK();
                Bot.Events.PlayerAFK += _AntiAFK;
            }
            else Bot.Events.PlayerAFK -= _AntiAFK;

            static void _AntiAFK()
            {
                if (Bot.Player.AFK)
                    Bot.Send.Packet("%xt%zm%afk%1%false%");
            }
        }

        private readonly ItemCategory[] categoryWhiteList =
{
            ItemCategory.Sword,
            ItemCategory.Axe,
            ItemCategory.Dagger,
            ItemCategory.Gun,
            ItemCategory.HandGun,
            ItemCategory.Rifle,
            ItemCategory.Bow,
            ItemCategory.Mace,
            ItemCategory.Gauntlet,
            ItemCategory.Polearm,
            ItemCategory.Staff,
            ItemCategory.Wand,
            ItemCategory.Whip,
            ItemCategory.Helm,
            ItemCategory.Cape,
            ItemCategory.Class,
            ItemCategory.Armor,
            ItemCategory.Misc,
            ItemCategory.Pet,
        };

        private static readonly string[] itemGroupWhitelist =
        {
            "ar",
            "ba",
            "co",
            "he",
            "mi",
            "pe",
            "Weapon",
        };

        public enum DataType
        {
            Player,
            Shop,
            Quest,
            Inventory,
            Bank,
            Drops,
            PlayerTarget,
        };
    }
}
