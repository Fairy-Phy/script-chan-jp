using Caliburn.Micro;
using Microsoft.Win32;
using script_chan2.DataTypes;
using Serilog;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;

namespace script_chan2.GUI
{
    public class SettingsViewModel : Screen, IHandle<object>, IHandle<string>
    {
        private ILogger localLog = Log.ForContext<SettingsViewModel>();

        #region Events
        public void Handle(object message)
        {
            if (message is IrcConnectedData)
            {
                NotifyOfPropertyChange(() => IrcIsConnecting);
                NotifyOfPropertyChange(() => IrcIsConnected);
                NotifyOfPropertyChange(() => IrcIsDisconnected);
                NotifyOfPropertyChange(() => PrivateIrcIsConnecting);
                NotifyOfPropertyChange(() => PrivateIrcIsConnected);
                NotifyOfPropertyChange(() => PrivateIrcIsDisconnected);
            }
        }

        public void Handle(string message)
        {
            if (message == "ConfigFileChanged")
            {
                NotifyOfPropertyChange(() => PrivateIrcVisible);
            }
        }
        #endregion

        #region Properties
        private bool dirty;
        public bool Dirty
        {
            get { return dirty; }
            set
            {
                if (value != dirty)
                {
                    dirty = value;
                    NotifyOfPropertyChange(() => Dirty);
                }
            }
        }

        public BindableCollection<string> Languages
        {
            get { return new BindableCollection<string>(new string[] { "en-US", "de-DE", "zh-TW", "ja-JP" }); }
        }

        private string lang;
        public string SelectedLanguage
        {
            get { return lang; }
            set
            {
                if (value != lang)
                {
                    lang = value;
                    NotifyOfPropertyChange(() => SelectedLanguage);
                    Dirty = true;
                }
            }
        }

        private string apiKey;
        public string ApiKey
        {
            get { return apiKey; }
            set
            {
                if (value != apiKey)
                {
                    apiKey = value;
                    NotifyOfPropertyChange(() => ApiKey);
                    Dirty = true;
                }
            }
        }

        public void OpenApiKeyPage()
        {
            localLog.Information("open api key page");
            Process.Start("https://osu.ppy.sh/p/api");
        }

        public Visibility ApiWorks
        {
            get
            {
                if (apiStatus == true)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
        }

        public Visibility ApiError
        {
            get
            {
                if (apiStatus == false)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
        }

        private bool? apiStatus;
        public bool? ApiStatus
        {
            get { return apiStatus; }
            set
            {
                if (value != apiStatus)
                {
                    apiStatus = value;
                    NotifyOfPropertyChange(() => ApiStatus);
                    NotifyOfPropertyChange(() => ApiWorks);
                    NotifyOfPropertyChange(() => ApiError);
                }
            }
        }

        private bool isCheckingApi;
        public bool IsCheckingApi
        {
            get { return isCheckingApi; }
            set
            {
                if (value != isCheckingApi)
                {
                    isCheckingApi = value;
                    NotifyOfPropertyChange(() => IsCheckingApi);
                    NotifyOfPropertyChange(() => ApiProgress);
                    NotifyOfPropertyChange(() => ApiTestEnabled);
                }
            }
        }

        public Visibility ApiProgress
        {
            get
            {
                if (IsCheckingApi)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
        }

        public bool ApiTestEnabled
        {
            get { return !IsCheckingApi; }
        }

        public async void CheckApiKey()
        {
            localLog.Information("check api key");
            IsCheckingApi = true;
            ApiStatus = null;
            ApiStatus = await OsuApi.OsuApi.CheckApiKey(apiKey);
            IsCheckingApi = false;
        }

        private string ircUsername;
        public string IrcUsername
        {
            get { return ircUsername; }
            set
            {
                if (value != ircUsername)
                {
                    ircUsername = value;
                    NotifyOfPropertyChange(() => IrcUsername);
                    Dirty = true;
                }
            }
        }

        private string ircPassword;
        public string IrcPassword
        {
            get { return ircPassword; }
            set
            {
                if (value != ircPassword)
                {
                    ircPassword = value;
                    NotifyOfPropertyChange(() => IrcPassword);
                    Dirty = true;
                }
            }
        }

        public void OpenIrcPage()
        {
            localLog.Information("open irc credentials page");
            System.Diagnostics.Process.Start("https://osu.ppy.sh/p/irc");
        }

        public Visibility IrcIsConnecting
        {
            get
            {
                if (OsuIrc.OsuIrc.ConnectionStatus == Enums.IrcStatus.Connecting)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
        }

        public Visibility IrcIsConnected
        {
            get
            {
                if (OsuIrc.OsuIrc.ConnectionStatus == Enums.IrcStatus.Connected)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
        }

        public Visibility IrcIsDisconnected
        {
            get
            {
                if (OsuIrc.OsuIrc.ConnectionStatus == Enums.IrcStatus.Disconnected)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
        }

        public Visibility PrivateIrcIsConnecting
        {
            get
            {
                if (!Settings.EnablePrivateIrc || string.IsNullOrEmpty(Settings.IrcIpPrivate))
                    return Visibility.Collapsed;
                if (OsuIrc.OsuIrc.PrivateConnectionStatus == Enums.IrcStatus.Connecting)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
        }

        public Visibility PrivateIrcIsConnected
        {
            get
            {
                if (!Settings.EnablePrivateIrc || string.IsNullOrEmpty(Settings.IrcIpPrivate))
                    return Visibility.Collapsed;
                if (OsuIrc.OsuIrc.PrivateConnectionStatus == Enums.IrcStatus.Connected)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
        }

        public Visibility PrivateIrcIsDisconnected
        {
            get
            {
                if (!Settings.EnablePrivateIrc || string.IsNullOrEmpty(Settings.IrcIpPrivate))
                    return Visibility.Collapsed;
                if (OsuIrc.OsuIrc.PrivateConnectionStatus == Enums.IrcStatus.Disconnected)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
        }

        private int ircTimeout;
        public int IrcTimeout
        {
            get { return ircTimeout; }
            set
            {
                if (value != ircTimeout)
                {
                    ircTimeout = value;
                    NotifyOfPropertyChange(() => IrcTimeout);
                    Dirty = true;
                }
            }
        }

        public Visibility PrivateIrcVisible
        {
            get
            {
                if (!string.IsNullOrEmpty(Settings.IrcIpPrivate))
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
        }

        private bool enablePrivateIrc;
        public bool EnablePrivateIrc
        {
            get { return enablePrivateIrc; }
            set
            {
                if (value != enablePrivateIrc)
                {
                    enablePrivateIrc = value;
                    NotifyOfPropertyChange(() => EnablePrivateIrc);
                    Dirty = true;
                }
            }
        }

        private int defaultTimerCommand;
        public int DefaultTimerCommand
        {
            get { return defaultTimerCommand; }
            set
            {
                if (value != defaultTimerCommand)
                {
                    defaultTimerCommand = value;
                    NotifyOfPropertyChange(() => DefaultTimerCommand);
                    Dirty = true;
                }
            }
        }

        private int defaultTimerAfterGame;
        public int DefaultTimerAfterGame
        {
            get { return defaultTimerAfterGame; }
            set
            {
                if (value != defaultTimerAfterGame)
                {
                    defaultTimerAfterGame = value;
                    NotifyOfPropertyChange(() => DefaultTimerAfterGame);
                    Dirty = true;
                }
            }
        }

        private int defaultTimerAfterPick;
        public int DefaultTimerAfterPick
        {
            get { return defaultTimerAfterPick; }
            set
            {
                if (value != defaultTimerAfterPick)
                {
                    defaultTimerAfterPick = value;
                    NotifyOfPropertyChange(() => DefaultTimerAfterPick);
                    Dirty = true;
                }
            }
        }

        private string notificationSoundFile;
        public string NotificationSoundFile
        {
            get { return notificationSoundFile; }
            set
            {
                if (value != notificationSoundFile)
                {
                    notificationSoundFile = value;
                    NotifyOfPropertyChange(() => NotificationSoundFile);
                    Dirty = true;
                }
            }
        }

        private bool enableNotifications;
        public bool EnableNotifications
        {
            get { return enableNotifications; }
            set
            {
                if (value != enableNotifications)
                {
                    enableNotifications = value;
                    NotifyOfPropertyChange(() => EnableNotifications);
                    Dirty = true;
                }
            }
        }

        private double notificationVolume;
        public double NotificationVolume
        {
            get { return notificationVolume; }
            set
            {
                if (value != notificationVolume)
                {
                    notificationVolume = value;
                    NotifyOfPropertyChange(() => NotificationVolume);
                    Dirty = true;
                }
            }
        }
        #endregion

        #region Constructor
        protected override void OnActivate()
        {
            Discard();
            CheckApiKey();
            Events.Aggregator.Subscribe(this);
        }

        protected override void OnDeactivate(bool close)
        {
            Events.Aggregator.Unsubscribe(this);
            base.OnDeactivate(close);
        }
        #endregion

        #region Actions
        public void Save()
        {
            localLog.Information("save changes");
            if (lang != Settings.Lang)
                MessageBox.Show("Changing the language requires an app restart!");
            Settings.Lang = lang;
            Settings.ApiKey = apiKey;
            Settings.IrcUsername = ircUsername;
            Settings.IrcPassword = ircPassword;
            Settings.IrcTimeout = ircTimeout;
            Settings.EnablePrivateIrc = enablePrivateIrc;
            Settings.DefaultTimerCommand = defaultTimerCommand;
            Settings.DefaultTimerAfterGame = defaultTimerAfterGame;
            Settings.DefaultTimerAfterPick = defaultTimerAfterPick;
            Settings.NotificationSoundFile = notificationSoundFile;
            Settings.EnableNotifications = enableNotifications;
            Settings.NotificationVolume = Math.Round(notificationVolume / 100, 2);
            Dirty = false;
        }

        public void Discard()
        {
            localLog.Information("discard changes");
            SelectedLanguage = Settings.Lang;
            ApiKey = Settings.ApiKey;
            IrcUsername = Settings.IrcUsername;
            IrcPassword = Settings.IrcPassword;
            IrcTimeout = Settings.IrcTimeout;
            EnablePrivateIrc = Settings.EnablePrivateIrc;
            DefaultTimerCommand = Settings.DefaultTimerCommand;
            DefaultTimerAfterGame = Settings.DefaultTimerAfterGame;
            DefaultTimerAfterPick = Settings.DefaultTimerAfterPick;
            NotificationSoundFile = Settings.NotificationSoundFile;
            EnableNotifications = Settings.EnableNotifications;
            NotificationVolume = Settings.NotificationVolume * 100;
            Dirty = false;
        }

        public void SelectNotificationSoundFile()
        {
            localLog.Information("open notification sound file selection");
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() != true)
                return;

            localLog.Information("set notification sound file");
            NotificationSoundFile = openFileDialog.FileName;
        }

        public void OpenConfigFileDirectory()
        {
            Process.Start(Settings.CONFIG_PATH);
        }
        #endregion
    }
}
