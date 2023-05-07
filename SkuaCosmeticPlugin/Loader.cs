using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Skua.Core.Interfaces;
using Skua.Core.Models;
using Skua.Core.Models.Items;
using Skua.Core.Scripts;
using Skua.WPF.Services;
using Skua_CosmeticPlugin;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using static Skua_CosmeticPlugin.CosmeticsMainWindow;

namespace Skua_CosmeticsPlugin
{
    public class Loader : ISkuaPlugin
    {
        public static Loader Instance { get; } = new();
        public string Name => "Cosmetics Plugin";
        public string Author => "Lord Exelot";
        public string Description => "This plugin will allow you to visually load items onto your character without having them.";

        public List<IOption>? Options { get; } = new();
        private IPluginHelper? helper = null;

        #region (Un)Load
        public IScriptInterface? Bot { get; private set; }
        public void Load(IServiceProvider provider, IPluginHelper helper)
        {
            Bot = provider.GetRequiredService<IScriptInterface>();
            this.helper = helper;

            helper.AddMenuButton(Name, () =>
            {
                CosmeticsMainWindow.Instance.Show();
                CosmeticsMainWindow.Instance.BringIntoView();
                CosmeticsMainWindow.Instance.Activate();
            });

            Logger("Plugin", "Loaded");

            Task.Run(async () =>
            {
                ThemeService = new ThemeService(provider.GetRequiredService<ThemeUserSettingsService>());
                var themeService = provider.GetRequiredService<IThemeService>();
                themeService.ThemeChanged += ThemeChanged;
                themeService.SchemeChanged += SchemeChanged;

                await PreInitSettings.Load();
                EnableEvents();
            });
        }

        public void Unload()
        {
            helper!.RemoveMenuButton(Name);
            Logger("Plugin", "Unloaded");
        }
        #endregion
        #region Passive Parsing
        private void EnableEvents()
        {
            if (!PreInitSettings.Instance.PasParseDrop &&
                !PreInitSettings.Instance.PasParseShop &&
                !PreInitSettings.Instance.PasParseQuest &&
                !PreInitSettings.Instance.PasParsePlayer &&
                !PreInitSettings.Instance.PasParseMap)
                return;

            if (!_eventsInit)
            {
                Application.Current.Dispatcher.Invoke(() => CosmeticsMainWindow.Instance.Hide());
                (manager ??= Ioc.Default.GetRequiredService<IScriptManager>()).PropertyChanged += Manager_PropertyChanged;
            }
            typeof(ScriptEvent).GetMethod("RegisterGameEvents", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(Bot!.Events, null);

            if (PreInitSettings.Instance.PasParseDrop)
                Bot!.Events.ItemDropped += Events_ItemDropped;
            if (PreInitSettings.Instance.PasParseMap)
                Bot!.Events.MapChanged += Events_MapChanged;
            if (PreInitSettings.Instance.PasParseShop || PreInitSettings.Instance.PasParseQuest || PreInitSettings.Instance.PasParsePlayer)
                Bot!.Events.ExtensionPacketReceived += Events_ExtensionPacketReceived;
            Bot!.Events.ScriptStopping += Events_ScriptStopping;

            _eventsInit = true;
        }

        private void Events_MapChanged(string map)
        {
            AddCtrl.PassiveParseMapPlayers(map);
            DebugLogger("pasParse Map " + map);
        }

        private void Manager_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ScriptRunning" && sender != null && ((ScriptManager)sender).ScriptRunning)
                typeof(ScriptEvent).GetMethod("UnregisterGameEvents", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(Bot!.Events, null);
        }

        private bool _eventsInit = false;
        private IScriptManager? manager;

        private bool Events_ScriptStopping(Exception? exception)
        {
            Task.Run(() =>
            {
                Task.Delay(7500);
                EnableEvents();
            });
            return true;
        }

        private void Events_ExtensionPacketReceived(dynamic packet)
        {
            string type = packet["params"].type;
            dynamic data = packet["params"].dataObj;
            if (type is not null and "json")
            {
                string cmd = data.cmd.ToString();
                switch (cmd)
                {
                    case "loadShop":
                        if (PreInitSettings.Instance.PasParseShop)
                        {
                            int ID = Int32.Parse(data.shopinfo.ShopID.ToString());
                            Bot!.Wait.ForTrue(() => Bot.Shops.ID == ID, 20);

                            AddCtrl.PassiveParseItemBase(Bot!.Shops.Items.OrderBy(x => x.ID).ToArray());
                            DebugLogger("pasParse Shop");
                        }
                        break;

                    case "getQuests":
                        if (PreInitSettings.Instance.PasParseQuest)
                        {
                            Dictionary<int, dynamic> _packet = JsonConvert.DeserializeObject<Dictionary<int, dynamic>>(data.quests.ToString());
                            IEnumerable<int> IDs = _packet.Keys;
                            Bot!.Wait.ForTrue(() => IDs.All(q => Bot!.Quests.Tree.Any(x => x.ID == q)), 20);

                            AddCtrl.PassiveParseItemBase(
                                Bot!.Quests.Tree.FindAll(q => IDs.Contains(q.ID))
                                .Select(q =>
                                    q.Rewards.Concat(
                                    q.Requirements).Concat(
                                    q.AcceptRequirements))
                                .SelectMany(x => x)
                                .ToArray());
                            DebugLogger("pasParse Quest");
                        }
                        break;
                }
            }
        }

        private void Events_ItemDropped(ItemBase item, bool addedToInv, int quantityNow)
        {
            AddCtrl.PassiveParseItemBase(item);
            DebugLogger("pasParse Drop");
        }
        #endregion
        #region Themes
        public static ThemeService? ThemeService;
        public static void ThemeChanged(object? theme)
        {
            ThemeService!.SetCurrentTheme(theme);
        }
        public static void SchemeChanged(ColorScheme scheme, object? color)
        {
            ThemeService!.ChangeCustomColor(color);
            ThemeService.ChangeScheme(scheme);
        }
        #endregion
        #region Settings
        private class PreInitSettings
        {
            private static PreInitSettings? _instance;
            public static PreInitSettings Instance
            {
                get
                {
                    return _instance ??= new PreInitSettings();
                }
                set
                {
                    _instance = value;
                }
            }


            private bool _pasParseShop = false;
            public bool PasParseShop
            {
                get { return _pasParseShop; }
                set { _pasParseShop = value; }
            }

            private bool _pasParseQuest = false;
            public bool PasParseQuest
            {
                get { return _pasParseQuest; }
                set { _pasParseQuest = value; }
            }

            private bool _pasParseDrop = false;
            public bool PasParseDrop
            {
                get { return _pasParseDrop; }
                set { _pasParseDrop = value; }
            }

            private bool _pasParseMap = false;
            public bool PasParseMap
            {
                get { return _pasParseMap; }
                set { _pasParseMap = value; }
            }

            private bool _pasParsePlayer = false;
            public bool PasParsePlayer
            {
                get { return _pasParsePlayer; }
                set { _pasParsePlayer = value; }
            }

            private bool _debugLogger = false;
            public bool DebugLogger
            {
                get { return _debugLogger; }
                set { _debugLogger = value; }
            }

            public static async Task Load()
            {
                await Task.Run(() =>
                {
                    if (File.Exists(settingsPath))
                        Instance = (PreInitSettings)new JsonSerializer().Deserialize(File.OpenText(settingsPath), typeof(PreInitSettings))!;
                });
            }
        }
        private readonly static string settingsPath = Path.Combine(DirectoryPath, "PluginSettings.json");
        #endregion
        #region Logging
        public void DebugLogger(string? extra = null, [CallerMemberName] string? caller = null, [CallerLineNumber] int line = 0)
        {
            lastDebugLog = $"[{DateTime.Now:HH:mm:ss:fff}] <Debug Logger~> ({caller})  Line {line}{(extra == null ? String.Empty : " > " + extra)}";
            if (PreInitSettings.Instance.DebugLogger)
                Bot!.Log(lastDebugLog);
        }
        public void DebugLogger(object? extra, [CallerMemberName] string? caller = null, [CallerLineNumber] int line = 0)
        {
            lastDebugLog = $"[{DateTime.Now:HH:mm:ss:fff}] <Debug Logger~> ({caller})  Line {line}{(extra == null ? String.Empty : " > " + extra)}";
            if (PreInitSettings.Instance.DebugLogger)
                Bot!.Log(lastDebugLog);
        }
        #endregion
    }
}
