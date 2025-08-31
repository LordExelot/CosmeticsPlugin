using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Skua.Core.Interfaces;
using Skua.Core.Messaging;
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
    public class Loader : ISkuaPlugin, IRecipient<ItemDroppedMessage>, IRecipient<MapChangedMessage>, IRecipient<ExtensionPacketMessage>, IRecipient<ScriptStoppingMessage>
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
            try
            {
                Bot = provider.GetRequiredService<IScriptInterface>();
                this.helper = helper;

                helper.AddMenuButton(Name, () =>
                {
                    try
                    {
                        CosmeticsMainWindow.Instance?.Show();
                        CosmeticsMainWindow.Instance?.BringIntoView();
                        CosmeticsMainWindow.Instance?.Activate();
                    }
                    catch (Exception ex)
                    {
                        Bot?.Log($"Error showing {Name} window: {ex.Message}");
                        Bot?.Log($"Stack trace: {ex.StackTrace}");
                    }
                });

                Logger("Plugin", "Load method started");

                Task.Run(async () =>
                {
                    try
                    {
                        ThemeService = new ThemeService(provider.GetRequiredService<ThemeUserSettingsService>());
                        var themeService = provider.GetRequiredService<IThemeService>();
                        themeService.ThemeChanged += ThemeChanged;
                        themeService.SchemeChanged += SchemeChanged;

                        await PreInitSettings.Load();
                        EnableMessengers();
                        Logger("Plugin", "Initialization completed");
                    }
                    catch (Exception ex)
                    {
                        Bot?.Log($"Error during async initialization: {ex.Message}");
                        Bot?.Log($"Stack trace: {ex.StackTrace}");
                    }
                });

                Logger("Plugin", "Loaded successfully");
            }
            catch (Exception ex)
            {
                var bot = provider?.GetService<IScriptInterface>();
                bot?.Log($"Critical error loading {Name}: {ex.Message}");
                bot?.Log($"Stack trace: {ex.StackTrace}");
                throw; // Re-throw to let Skua handle the error
            }
        }

        public void Unload()
        {
            // Unregister from messengers
            StrongReferenceMessenger.Default.Unregister<ItemDroppedMessage, int>(this, (int)MessageChannels.GameEvents);
            StrongReferenceMessenger.Default.Unregister<MapChangedMessage, int>(this, (int)MessageChannels.GameEvents);
            StrongReferenceMessenger.Default.Unregister<ExtensionPacketMessage, int>(this, (int)MessageChannels.GameEvents);
            StrongReferenceMessenger.Default.Unregister<ScriptStoppingMessage, int>(this, (int)MessageChannels.ScriptStatus);
            
            helper!.RemoveMenuButton(Name);
            Logger("Plugin", "Unloaded");
        }
        #endregion
        #region Passive Parsing
        private void EnableMessengers()
        {
            if (!PreInitSettings.Instance.PasParseDrop &&
                !PreInitSettings.Instance.PasParseShop &&
                !PreInitSettings.Instance.PasParseQuest &&
                !PreInitSettings.Instance.PasParsePlayer &&
                !PreInitSettings.Instance.PasParseMap)
                return;

            if (!_messengersInit)
            {
                Application.Current.Dispatcher.Invoke(() => CosmeticsMainWindow.Instance.Hide());
                (manager ??= Ioc.Default.GetRequiredService<IScriptManager>()).PropertyChanged += Manager_PropertyChanged;
            }

            // Register for relevant messages based on settings
            if (PreInitSettings.Instance.PasParseDrop)
                StrongReferenceMessenger.Default.Register<Loader, ItemDroppedMessage, int>(this, (int)MessageChannels.GameEvents, (recipient, message) => recipient.OnItemDropped(message));
            
            if (PreInitSettings.Instance.PasParseMap)
                StrongReferenceMessenger.Default.Register<Loader, MapChangedMessage, int>(this, (int)MessageChannels.GameEvents, (recipient, message) => recipient.OnMapChanged(message));
            
            if (PreInitSettings.Instance.PasParseShop || PreInitSettings.Instance.PasParseQuest || PreInitSettings.Instance.PasParsePlayer)
                StrongReferenceMessenger.Default.Register<Loader, ExtensionPacketMessage, int>(this, (int)MessageChannels.GameEvents, (recipient, message) => recipient.OnExtensionPacket(message));
            
            StrongReferenceMessenger.Default.Register<Loader, ScriptStoppingMessage, int>(this, (int)MessageChannels.ScriptStatus, (recipient, message) => recipient.OnScriptStopping(message));

            _messengersInit = true;
        }

        public void Receive(MapChangedMessage message)
        {
            OnMapChanged(message);
        }

        private void OnMapChanged(MapChangedMessage message)
        {
            AddCtrl.PassiveParseMapPlayers(message.Map);
            DebugLogger("pasParse Map " + message.Map);
        }

        private void Manager_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ScriptRunning" && sender != null && ((ScriptManager)sender).ScriptRunning)
                typeof(ScriptEvent).GetMethod("UnregisterGameEvents", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(Bot!.Events, null);
        }

        private bool _messengersInit = false;
        private IScriptManager? manager;

        public void Receive(ScriptStoppingMessage message)
        {
            OnScriptStopping(message);
        }

        private void OnScriptStopping(ScriptStoppingMessage message)
        {
            Task.Run(async () =>
            {
                await Task.Delay(7500);
                EnableMessengers();
            });
        }

        public void Receive(ExtensionPacketMessage message)
        {
            OnExtensionPacket(message);
        }

        private void OnExtensionPacket(ExtensionPacketMessage message)
        {
            dynamic packet = message.Packet;
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

        public void Receive(ItemDroppedMessage message)
        {
            OnItemDropped(message);
        }

        private void OnItemDropped(ItemDroppedMessage message)
        {
            AddCtrl.PassiveParseItemBase(message.Item);
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
