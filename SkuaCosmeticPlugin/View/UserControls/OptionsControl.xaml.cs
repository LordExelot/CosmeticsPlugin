using Newtonsoft.Json;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using static Skua_CosmeticPlugin.CosmeticsMainWindow;

namespace Skua_CosmeticPlugin.View.UserControls
{
    /// <summary>
    /// Interaction logic for OptionsControl.xaml
    /// </summary>
    public partial class OptionsControl : UserControl
    {
        public static OptionsControl Instance { get; } = new();
        public OptionsControl()
        {
            InitializeComponent();

            AssignGender();
            Settings.Instance.Load();
            OptionInGameGender.SelectionChanged += OptionInGameGender_SelectionChanged;
        }

        private void ToggleDevOptions_Click(object sender, RoutedEventArgs e)
        {
            toggleDevClick++;
            if (toggleDevClick >= 5)
            {
                Settings.Instance.DeveloperOptionsVisible = !Settings.Instance.DeveloperOptionsVisible;
                _toggleDevOption();
            }
        }
        private void _toggleDevOption()
        {
            devOptions.Height = CosmeticsMainWindow.ShowGridIfTrue(Settings.Instance.DeveloperOptionsVisible);
            toggleDevClick = 0;
        }
        private int toggleDevClick = 0;

        private void OptionInGameGender_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadCtrl.ChangeGender((OptionInGameGender.SelectedIndex) switch
            {
                0 => 'M',
                1 => 'F',
                _ => 'R'
            });
        }
        public void AssignGender()
        {
            if (OptionInGameGender.SelectedIndex >= 0 || !Bot.Player.LoggedIn)
                return;

            OptionInGameGender.SelectedIndex = Bot.Flash.Call("gender") == "\"F\"" ? 1 : 0;
        }

        #region settings-class and events
        public class Settings
        {
            private static Settings? _instance;
            public static Settings Instance
            {
                get
                {
                    return _instance ??= new Settings();
                }
                set
                {
                    _instance = value;
                }
            }

            private int _downloadSWFGender = 2;
            public int DownloadSWFGender
            {
                get
                {
                    return _downloadSWFGender;
                }
                set
                {
                    _downloadSWFGender = value;
                    if (_loaded)
                        Save();
                }
            }

            private bool _debugLogger = false;
            public bool DebugLogger
            {
                get
                {
                    return _debugLogger;
                }
                set
                {
                    _debugLogger = value;
                    if (canSave())
                        Save();
                }
            }

            private bool _developerOptionsVisibile = false;
            public bool DeveloperOptionsVisible
            {
                get
                {
                    return _developerOptionsVisibile;
                }
                set
                {
                    _developerOptionsVisibile = value;
                    if (_loaded)
                        Save();
                }
            }

            private bool _artistMode = false;
            public bool ArtistMode
            {
                get
                {
                    return _artistMode;
                }
                set
                {
                    _artistMode = value;
                    if (canSave())
                        Save();
                }
            }
            private bool _artistModeShowAssetsButton = false;
            public bool ArtistModeShowAssetsButton
            {
                get
                {
                    return _artistModeShowAssetsButton;
                }
                set
                {
                    _artistModeShowAssetsButton = value;
                    if (canSave())
                        Save();
                }
            }

            private bool _showManualButton = false;
            public bool ShowManualButton
            {
                get
                {
                    return _showManualButton;
                }
                set
                {
                    _showManualButton = value;
                    if (canSave())
                        Save();
                }
            }

            private string _paginaSWFsShown = "10";
            public string PaginaSWFsShown
            {
                get
                {
                    return _paginaSWFsShown;
                }
                set
                {
                    _paginaSWFsShown = value;
                    if (canSave())
                        Save();
                }
            }

            private bool _cacheCB = false;
            public bool CacheCB
            {
                get
                {
                    return _cacheCB;
                }
                set
                {
                    _cacheCB = value;
                    if (canSave())
                        Save();
                }
            }

            private bool _pasParseShop = false;
            public bool PasParseShop
            {
                get
                {
                    return _pasParseShop;
                }
                set
                {
                    _pasParseShop = value;
                    if (canSave())
                        Save();
                }
            }

            private bool _pasParseQuest = false;
            public bool PasParseQuest
            {
                get
                {
                    return _pasParseQuest;
                }
                set
                {
                    _pasParseQuest = value;
                    if (canSave())
                        Save();
                }
            }

            private bool _pasParseDrop = false;
            public bool PasParseDrop
            {
                get
                {
                    return _pasParseDrop;
                }
                set
                {
                    _pasParseDrop = value;
                    if (canSave())
                        Save();
                }
            }

            private bool _pasParseMap = false;
            public bool PasParseMap
            {
                get
                {
                    return _pasParseMap;
                }
                set
                {
                    _pasParseMap = value;
                    if (canSave())
                        Save();
                }
            }

            private bool _pasParsePlayer = false;
            public bool PasParsePlayer
            {
                get
                {
                    return _pasParsePlayer;
                }
                set
                {
                    _pasParsePlayer = value;
                    if (canSave())
                        Save();
                }
            }

            private bool canSave()
            {
                return
                    Instance._loaded &&
                    OptionsControl.Instance._optionDownloadGender_loaded &&
                    OptionsControl.Instance._manualLoadCheck_loaded &&
                    OptionsControl.Instance._artistModeCheck_loaded &&
                    OptionsControl.Instance._debugLoggerCheck_loaded &&
                    OptionsControl.Instance._artistModeShowAssetsCheck_loaded &&
                    OptionsControl.Instance._numberOfSWFsCombo_Loaded &&
                    OptionsControl.Instance._cacheDBCheck_Loaded &&
                    OptionsControl.Instance._passiveParsePlayerCheck_Loaded &&
                    OptionsControl.Instance._passiveParseMapCheck_Loaded &&
                    OptionsControl.Instance._passiveParseDropCheck_Loaded &&
                    OptionsControl.Instance._passiveParseQuestCheck_Loaded &&
                    OptionsControl.Instance._passiveParseShopCheck_Loaded;
            }

            private static readonly string FilePath = Path.Combine(DirectoryPath, "PluginSettings.json");

            public async static void Save([CallerMemberName] string? caller = null)
            {
                await Task.Run(() =>
                {
                    Exception? ex = null;
                    Directory.CreateDirectory(DirectoryPath);
                    for (int i = 0; i < 10; i++)
                    {
                        try
                        {
                            File.WriteAllText(FilePath, JsonConvert.SerializeObject(Instance, Formatting.Indented));
                            return;
                        }
                        catch (Exception e)
                        {
                            ex ??= e;
                        }
                    }
                    Logger("Options", $"Saving {caller} Failed: {(ex == null ? "Timed Out" : ex.ToString())}");
                });
            }

            public async void Load()
            {
                await Task.Run(() =>
                {
                    if (File.Exists(FilePath))
                        Instance = (Settings)new JsonSerializer().Deserialize(File.OpenText(FilePath), typeof(Settings))!;
                    Instance._loaded = true;
                });

                OptionsControl.Instance._toggleDevOption();

                LoadSWFWindow.Instance.VisibleRowOptionManual.Height = CosmeticsMainWindow.ShowGridIfTrue(Settings.Instance.ShowManualButton);
                OptionsControl.Instance.artistModeRow.Height = CosmeticsMainWindow.ShowGridIfTrue(Settings.Instance.ArtistMode);
                LoadSWFWindow.Instance.VisibleRowOptionArtist.Height = CosmeticsMainWindow.ShowGridIfTrue(Settings.Instance.ArtistMode);
                LoadSWFWindow.Instance.VisibileColOptionAssets.Width = Settings.Instance.ArtistModeShowAssetsButton ? new(1, GridUnitType.Star) : new(0, GridUnitType.Pixel);
            }
            private bool _loaded = false;
        }

        #region changed events
        private void OptionDownloadGender_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_optionDownloadGender_loaded)
                return;
            Settings.Instance.DownloadSWFGender = OptionDownloadGender.SelectedIndex;
        }

        private void ManualLoadCheck_Event(object sender, RoutedEventArgs e)
        {
            if (!_manualLoadCheck_loaded)
                return;
            Settings.Instance.ShowManualButton = ManualLoadCheck.IsChecked == true;
            LoadCtrl.VisibleRowOptionManual.Height = CosmeticsMainWindow.ShowGridIfTrue(Settings.Instance.ShowManualButton);
        }

        private void ArtistModeCheck_Event(object sender, RoutedEventArgs e)
        {
            if (!_artistModeCheck_loaded)
                return;
            Settings.Instance.ArtistMode = ArtistModeCheck.IsChecked == true;
            artistModeRow.Height = CosmeticsMainWindow.ShowGridIfTrue(Settings.Instance.ArtistMode);
            LoadCtrl.VisibleRowOptionArtist.Height = CosmeticsMainWindow.ShowGridIfTrue(Settings.Instance.ArtistMode);
        }

        private void ArtistModeShowAssetsCheck_Event(object sender, RoutedEventArgs e)
        {
            if (!_artistModeShowAssetsCheck_loaded)
                return;
            Settings.Instance.ArtistModeShowAssetsButton = ArtistModeShowAssetsCheck.IsChecked == true;
            LoadSWFWindow.Instance.VisibileColOptionAssets.Width = Settings.Instance.ArtistModeShowAssetsButton ? new(1, GridUnitType.Star) : new(0, GridUnitType.Pixel);
        }

        private void DebugLoggerCheck_Event(object sender, RoutedEventArgs e)
        {
            if (!_debugLoggerCheck_loaded)
                return;
            Settings.Instance.DebugLogger = DebugLoggerCheck.IsChecked == true;
            if (Settings.Instance.DebugLogger)
                Bot.Log(lastDebugLog);
        }

        private void CacheDBCheck_Event(object sender, RoutedEventArgs e)
        {
            if (!_artistModeCheck_loaded)
                return;
            Settings.Instance.CacheCB = CacheDBCheck.IsChecked == true;

            if (Settings.Instance.CacheCB)
                UpdateDBCache();
            else File.Delete(Path.Combine(DirectoryPath, "LocalDatabaseCache.json"));
        }

        private void PassiveParseShopCheck_Event(object sender, RoutedEventArgs e)
        {
            if (!_passiveParseShopCheck_Loaded)
                return;
            Settings.Instance.PasParseShop = PassiveParseShopCheck.IsChecked == true;
        }

        private void PassiveParseQuestCheck_Event(object sender, RoutedEventArgs e)
        {
            if (!_passiveParseQuestCheck_Loaded)
                return;
            Settings.Instance.PasParseQuest = PassiveParseQuestCheck.IsChecked == true;
        }

        private void PassiveParseDropCheck_Event(object sender, RoutedEventArgs e)
        {
            if (!_passiveParseDropCheck_Loaded)
                return;
            Settings.Instance.PasParseDrop = PassiveParseDropCheck.IsChecked == true;
        }

        private void PassiveParseMapCheck_Event(object sender, RoutedEventArgs e)
        {
            if (!_passiveParseMapCheck_Loaded)
                return;
            Settings.Instance.PasParseMap = PassiveParseMapCheck.IsChecked == true;
        }

        private void PassiveParsePlayerCheck_Event(object sender, RoutedEventArgs e)
        {
            if (!_passiveParsePlayerCheck_Loaded)
                return;
            Settings.Instance.PasParsePlayer = PassiveParsePlayerCheck.IsChecked == true;
        }

        #endregion
        #region loaded events
        private bool _optionDownloadGender_loaded = false;
        private void OptionDownloadGender_Loaded(object sender, RoutedEventArgs e)
        {
            OptionDownloadGender.SelectedIndex = Settings.Instance.DownloadSWFGender;
            OptionDownloadGender.Loaded -= new(OptionDownloadGender_Loaded);
            _optionDownloadGender_loaded = true;
        }

        private bool _manualLoadCheck_loaded = false;
        private void ManualLoadCheck_Loaded(object sender, RoutedEventArgs e)
        {
            ManualLoadCheck.IsChecked = Settings.Instance.ShowManualButton;
            ManualLoadCheck.Loaded -= new(ManualLoadCheck_Loaded);
            _manualLoadCheck_loaded = true;
        }

        private bool _artistModeCheck_loaded = false;
        private void ArtistModeCheck_Loaded(object sender, RoutedEventArgs e)
        {
            ArtistModeCheck.IsChecked = Settings.Instance.ArtistMode;
            ArtistModeCheck.Loaded -= new(ArtistModeCheck_Loaded);
            _artistModeCheck_loaded = true;
        }

        private bool _debugLoggerCheck_loaded = false;
        private void DebugLoggerCheck_Loaded(object sender, RoutedEventArgs e)
        {
            DebugLoggerCheck.IsChecked = Settings.Instance.DebugLogger;
            DebugLoggerCheck.Loaded -= new(DebugLoggerCheck_Loaded);
            _debugLoggerCheck_loaded = true;
        }

        private bool _artistModeShowAssetsCheck_loaded = false;
        private void ArtistModeShowAssetsCheck_Loaded(object sender, RoutedEventArgs e)
        {
            ArtistModeShowAssetsCheck.IsChecked = Settings.Instance.ArtistModeShowAssetsButton;
            ArtistModeShowAssetsCheck.Loaded -= new(ArtistModeShowAssetsCheck_Loaded);
            _artistModeShowAssetsCheck_loaded = true;
            LoadSWFWindow.Instance.VisibileColOptionAssets.Width = Settings.Instance.ArtistModeShowAssetsButton ? new(1, GridUnitType.Star) : new(0, GridUnitType.Pixel);
        }

        public bool _numberOfSWFsCombo_Loaded = false;

        public void NumberOfSWFsCombo_Loaded()
        {
            DataCtrl.NumberOfSWFsCombo.SelectedItem = new ComboBoxItem() { Content = Settings.Instance.PaginaSWFsShown };
            DataCtrl.NumberOfSWFsCombo.Loaded -= new(DataCtrl.NumberOfSWFsCombo_Loaded);
            _numberOfSWFsCombo_Loaded = true;
        }

        public bool _cacheDBCheck_Loaded = false;
        public void CacheDBCheck_Loaded(object sender, RoutedEventArgs e)
        {
            CacheDBCheck.IsChecked = SettingsCtrl.CacheCB = File.Exists(Path.Combine(DirectoryPath, "LocalDatabaseCache.json"));
            CacheDBCheck.Loaded -= new(CacheDBCheck_Loaded);
            _cacheDBCheck_Loaded = true;
        }

        public bool _passiveParsePlayerCheck_Loaded = false;
        private void PassiveParsePlayerCheck_Loaded(object sender, RoutedEventArgs e)
        {
            PassiveParsePlayerCheck.IsChecked = Settings.Instance.PasParsePlayer;
            PassiveParsePlayerCheck.Loaded -= new(PassiveParsePlayerCheck_Loaded);
            _passiveParsePlayerCheck_Loaded = true;
        }

        public bool _passiveParseMapCheck_Loaded = false;
        private void PassiveParseMapCheck_Loaded(object sender, RoutedEventArgs e)
        {
            PassiveParseMapCheck.IsChecked = Settings.Instance.PasParseMap;
            PassiveParseMapCheck.Loaded -= new(PassiveParseMapCheck_Loaded);
            _passiveParseMapCheck_Loaded = true;
        }

        public bool _passiveParseDropCheck_Loaded = false;
        private void PassiveParseDropCheck_Loaded(object sender, RoutedEventArgs e)
        {
            PassiveParseDropCheck.IsChecked = Settings.Instance.PasParseDrop;
            PassiveParseDropCheck.Loaded -= new(PassiveParseDropCheck_Loaded);
            _passiveParseDropCheck_Loaded = true;
        }

        public bool _passiveParseQuestCheck_Loaded = false;
        private void PassiveParseQuestCheck_Loaded(object sender, RoutedEventArgs e)
        {
            PassiveParseQuestCheck.IsChecked = Settings.Instance.PasParseQuest;
            PassiveParseQuestCheck.Loaded -= new(PassiveParseQuestCheck_Loaded);
            _passiveParseQuestCheck_Loaded = true;
        }

        public bool _passiveParseShopCheck_Loaded = false;
        private void PassiveParseShopCheck_Loaded(object sender, RoutedEventArgs e)
        {
            PassiveParseShopCheck.IsChecked = Settings.Instance.PasParseShop;
            PassiveParseShopCheck.Loaded -= new(PassiveParseShopCheck_Loaded);
            _passiveParseShopCheck_Loaded = true;
        }
        #endregion

        public void UpdateDBCache()
        {
            if (!SettingsCtrl.CacheCB)
                return;

            DebugLogger();
            Task.Run(() =>
            {
                Directory.CreateDirectory(DirectoryPath);
                Exception? ex = null;
                DebugLogger();
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        File.WriteAllText(Path.Combine(DirectoryPath, "LocalDatabaseCache.json"), JsonConvert.SerializeObject(CurrentSWFs, Formatting.Indented));
                        DebugLogger();
                        return;
                    }
                    catch (Exception e)
                    {
                        ex ??= e;
                    }
                }
                Logger("Options", $"Caching Database Failed: {(ex == null ? "Timed Out" : ex.ToString())}");
            });
        }
        #endregion
    }
}