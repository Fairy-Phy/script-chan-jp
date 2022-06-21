using Caliburn.Micro;
using MaterialDesignThemes.Wpf;
using script_chan2.DataTypes;
using script_chan2.Discord;
using script_chan2.Enums;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace script_chan2.GUI
{
    public class MatchViewModel : Screen, IHandle<object>, IHandle<string>
    {
        private ILogger localLog = Log.ForContext<MatchViewModel>();

        #region Lists
        public BindableCollection<MatchTeamViewModel> TeamsViews
        {
            get
            {
                var list = new BindableCollection<MatchTeamViewModel>();
                if (match.TeamMode == TeamModes.TeamVS)
                {
                    list.Add(new MatchTeamViewModel(match, TeamColors.Red));
                    list.Add(new MatchTeamViewModel(match, TeamColors.Blue));
                }
                return list;
            }
        }

        public BindableCollection<MatchPlayerViewModel> PlayersViews
        {
            get
            {
                var list = new BindableCollection<MatchPlayerViewModel>();
                if (match.TeamMode == TeamModes.HeadToHead)
                {
                    foreach (var player in match.Players.OrderByDescending(x => x.Value))
                    {
                        list.Add(new MatchPlayerViewModel(match, player.Key));
                    }
                }
                return list;
            }
        }

        public BindableCollection<MatchBRTeamViewModel> BRTeamsViews
        {
            get
            {
                var list = new BindableCollection<MatchBRTeamViewModel>();
                if (match.TeamMode == TeamModes.BattleRoyale)
                {
                    foreach (var team in match.TeamsBR.OrderByDescending(x => x.Key.Name))
                    {
                        list.Add(new MatchBRTeamViewModel(match, team.Key));
                    }
                }
                return list;
            }
        }

        public BindableCollection<Mappool> Mappools
        {
            get
            {
                var list = new BindableCollection<Mappool>();
                foreach (var mappool in Database.Database.Mappools)
                {
                    if (mappool.Tournament != match.Tournament)
                        continue;
                    list.Add(mappool);
                }
                return list;
            }
        }

        public BindableCollection<Team> Teams
        {
            get
            {
                var list = new BindableCollection<Team>();
                if (match.TeamMode == TeamModes.TeamVS)
                {
                    list.Add(match.TeamBlue);
                    list.Add(match.TeamRed);
                }
                return list;
            }
        }

        public BindableCollection<Player> Players
        {
            get
            {
                var list = new BindableCollection<Player>();
                if (match.TeamMode == TeamModes.HeadToHead)
                {
                    foreach (var player in match.Players.OrderBy(x => x.Key.Name))
                        list.Add(player.Key);
                }
                return list;
            }
        }

        public BindableCollection<MatchBeatmapViewModel> BeatmapsViews
        {
            get
            {
                var list = new BindableCollection<MatchBeatmapViewModel>();
                if (SelectedMappool != null)
                {
                    foreach (var beatmap in SelectedMappool.Beatmaps.OrderBy(x => x.ListIndex))
                    {
                        list.Add(new MatchBeatmapViewModel(match, beatmap, true));
                    }
                }
                return list;
            }
        }

        public BindableCollection<MatchBanchoEventViewModel> BanchoEventsViews
        {
            get
            {
                var list = new BindableCollection<MatchBanchoEventViewModel>();
                foreach (var message in match.ChatMessages)
                {
                    if (message.User == "BanchoBot")
                    {
                        if (message.Message.Contains("All players are ready"))
                            list.Add(new MatchBanchoEventViewModel(MatchBanchoEvents.AllPlayersReady, message.Timestamp));
                        if (message.Message.Contains("The match has finished"))
                            list.Add(new MatchBanchoEventViewModel(MatchBanchoEvents.AllPlayersFinished, message.Timestamp));
                        if (message.Message.Contains("rolls"))
                        {
                            var regex = new Regex(@"^(.+) rolls (\d+) point\(s\)$");
                            var regexResult = regex.Match(message.Message);
                            if (regexResult.Success)
                            {
                                var user = regexResult.Groups[1].Value;
                                var roll = regexResult.Groups[2].Value;

                                TeamColors? team = null;
                                if (match.TeamMode == TeamModes.TeamVS)
                                {
                                    if (match.TeamRed.Players.Any(x => x.Name == user))
                                        team = TeamColors.Red;
                                    if (match.TeamBlue.Players.Any(x => x.Name == user))
                                        team = TeamColors.Blue;
                                }
                                list.Add(new MatchBanchoEventViewModel(MatchBanchoEvents.PlayerRoll, message.Timestamp, user, team, roll));
                            }
                        }
                    }
                }
                return list;
            }
        }

        public List<GameModes> GameModesList
        {
            get { return Enum.GetValues(typeof(GameModes)).Cast<GameModes>().ToList(); }
        }

        public List<WinConditions> WinConditionsList
        {
            get { return Enum.GetValues(typeof(WinConditions)).Cast<WinConditions>().ToList(); }
        }

        public BindableCollection<MatchCustomCommandViewModel> CustomCommandViews
        {
            get
            {
                var list = new BindableCollection<MatchCustomCommandViewModel>();
                foreach (var customCommand in Database.Database.CustomCommands)
                {
                    if (customCommand.Tournament != null && customCommand.Tournament != match.Tournament)
                        continue;
                    list.Add(new MatchCustomCommandViewModel(match, customCommand));
                }
                return list;
            }
        }

        private BindableCollection<MatchRoomSlotViewModel> roomSlotsViews;
        public BindableCollection<MatchRoomSlotViewModel> RoomSlotsViews
        {
            get { return roomSlotsViews; }
            set
            {
                if (value != roomSlotsViews)
                {
                    roomSlotsViews = value;
                    NotifyOfPropertyChange(() => RoomSlotsViews);
                }
            }
        }
        #endregion

        #region Constructor
        public MatchViewModel(DataTypes.Match match)
        {
            this.match = match;
            if (match.RoomId > 0)
                OsuIrc.OsuIrc.JoinChannel("#mp_" + match.RoomId);
            RoomSlotsViews = new BindableCollection<MatchRoomSlotViewModel>();
            for (var i = 0; i < match.RoomSize; i++)
                RoomSlotsViews.Add(new MatchRoomSlotViewModel(match, i + 1));
            NotifyOfPropertyChange(() => RoomSlotsViews);
        }
        #endregion

        #region Events
        protected override void OnActivate()
        {
            localLog.Information("open window of match '{match}'", match.Name);

            Events.Aggregator.Subscribe(this);
            match.ReloadMessages();
            Execute.OnUIThread(() =>
            {
                MultiplayerChat = new FlowDocument
                {
                    FontFamily = new FontFamily("Lucida Console"),
                    FontSize = 12,
                    TextAlignment = TextAlignment.Left
                };
                foreach (var message in match.ChatMessages)
                {
                    AddMessageToChat(message, true);
                }
            });

            base.OnActivate();
        }

        protected override void OnViewLoaded(object view)
        {
            var actualView = (MatchView)view;
            var scrollViewer = FindScroll(actualView.ChatWindow);
            if (scrollViewer != null)
                scrollViewer.ScrollToEnd();

            WindowHeight = Settings.WindowHeight;
            WindowWidth = Settings.WindowWidth;
            if (!double.IsNaN(Settings.Overview2Height))
                actualView.Overview2Row.Height = new GridLength(Settings.Overview2Height);
            if (!double.IsNaN(Settings.Column1Width))
                actualView.Column1.Width = new GridLength(Settings.Column1Width);
            if (!double.IsNaN(Settings.Column2Width))
                actualView.Column2.Width = new GridLength(Settings.Column2Width);

            base.OnViewLoaded(view);
        }

        protected override void OnDeactivate(bool close)
        {
            localLog.Information("close window of match '{name}'", match.Name);
            MatchList.RemoveMatch(match);
            Events.Aggregator.Unsubscribe(this);

            var view = (MatchView)GetView();
            Settings.WindowHeight = WindowHeight;
            Settings.WindowWidth = WindowWidth;
            Settings.Overview2Height = view.Overview2Row.ActualHeight;
            Settings.Column1Width = view.Column1.ActualWidth;
            Settings.Column2Width = view.Column2.ActualWidth;

            base.OnDeactivate(close);
        }

        public async void Handle(object message)
        {
            if (message is RoomCreatedData)
            {
                var data = (RoomCreatedData)message;
                if (match.RoomId == data.Id)
                    return;
                if (data.Name == match.Name)
                {
                    localLog.Information("match '{match}' room creation data received", match.Name);
                    match.RoomId = data.Id;
                    match.Status = MatchStatus.InProgress;
                    match.Save();
                    NotifyOfPropertyChange(() => RoomLinkName);
                    NotifyOfPropertyChange(() => RoomClosedVisible);
                    NotifyOfPropertyChange(() => RoomOpenVisible);
                    OsuIrc.OsuIrc.JoinChannel("#mp_" + data.Id);
                    SendRoomSet();
                    if (match.Mappool != null && match.Mappool.Beatmaps.Count(x => x.PickCommand) > 0)
                    {
                        if (match.Mappool.Beatmaps.Any(x => x.Mods.Contains(GameMods.TieBreaker)))
                            SendRoomMessage($"!mp map {match.Mappool.Beatmaps.First(x => x.Mods.Contains(GameMods.TieBreaker)).Beatmap.Id} {(int)match.GameMode}");
                        else
                            SendRoomMessage($"!mp map {match.Mappool.Beatmaps.First(x => x.PickCommand).Beatmap.Id} {(int)match.GameMode}");
                    }
                    SendRoomMessage("!mp unlock");
                    SendRoomMessage("!mp settings");

                    if (!match.ViewerMode)
                    {
                        DiscordApi.SendMatchCreated(match);
                    }
                }
            }
            else if (message is ChannelMessageData)
            {
                var data = (ChannelMessageData)message;
                localLog.Information("match '{match}' irc message '{message}' received from user '{user}'", match.Name, data.Message, data.User);
                if (data.Channel == "#mp_" + match.RoomId)
                {
                    var ircMessage = new IrcMessage() { Channel = "#mp_" + match.RoomId, User = data.User, Timestamp = DateTime.Now, Match = match, Message = data.Message };
                    AddMessageToChat(ircMessage, false);

                    if (data.User == "BanchoBot" && data.Message.Contains("All players are ready"))
                    {
                        PlayNotificationSound();
                        foreach (var slot in RoomSlotsViews)
                        {
                            slot.State = RoomSlotStates.Ready;
                        }
                        NotifyOfPropertyChange(() => RoomSlotsViews);
                    }
                    if (data.User == "BanchoBot" && data.Message.Contains("The match has finished"))
                    {
                        PlayNotificationSound();
                        if (!match.PrivateRoom)
                        {
                            await UpdateScore(true);
                            if (!match.WarmupMode)
                            {
                                if (!match.ViewerMode)
                                {
                                    SendRoomStatus();
                                    DiscordApi.SendGameRecap(match);
                                }
                            }
                        }
                        foreach (var slot in RoomSlotsViews)
                        {
                            slot.State = RoomSlotStates.NotReady;
                        }
                    }
                    if (data.User == "BanchoBot" && data.Message.StartsWith("Room name:"))
                    {
                        foreach (var slot in RoomSlotsViews)
                        {
                            slot.Player = null;
                            slot.Team = null;
                            slot.Mods = new List<GameMods>();
                        }
                    }
                    if (data.User == "BanchoBot" && data.Message.StartsWith("Slot "))
                    {
                        var regex = new Regex(@"^Slot (\d+) ([\w ]+) https://osu.ppy.sh/u/(\d+) (.+) (\[.+\])?$");
                        var regexResult = regex.Match(data.Message);
                        if (regexResult.Success)
                        {
                            var slotNumber = Convert.ToInt32(regexResult.Groups[1].Value.Trim());

                            var readyString = regexResult.Groups[2].Value.Trim();
                            var ready = readyString.StartsWith("Ready");
                            var noMap = readyString.StartsWith("No Map");

                            var profileId = regexResult.Groups[3].Value;
                            var player = await Database.Database.GetPlayer(profileId);

                            var detailsString = regexResult.Groups[5].Value.Trim();

                            TeamColors? team = null;
                            if (detailsString.Contains("Team Blue"))
                                team = TeamColors.Blue;
                            if (detailsString.Contains("Team Red"))
                                team = TeamColors.Red;

                            var slot = RoomSlotsViews.FirstOrDefault(x => x.SlotNumber == slotNumber);
                            if (slot != null)
                            {
                                slot.Player = player;
                                slot.Team = team;
                                var mods = new List<GameMods>();
                                if (noMap)
                                    slot.State = RoomSlotStates.NoMap;
                                else if (ready)
                                    slot.State = RoomSlotStates.Ready;
                                else
                                    slot.State = RoomSlotStates.NotReady;
                                if (detailsString.Contains("Hidden"))
                                    mods.Add(GameMods.Hidden);
                                if (detailsString.Contains("HardRock"))
                                    mods.Add(GameMods.HardRock);
                                if (detailsString.Contains("NoFail"))
                                    mods.Add(GameMods.NoFail);
                                if (detailsString.Contains("Easy"))
                                    mods.Add(GameMods.Easy);
                                if (detailsString.Contains("Flashlight"))
                                    mods.Add(GameMods.Flashlight);
                                if (detailsString.Contains("Mirror"))
                                    mods.Add(GameMods.Mirror);
                                slot.Mods = mods;
                            }

                            NotifyOfPropertyChange(() => WrongTeamWarningVisible);
                        }
                    }
                    if (data.User == "BanchoBot" && data.Message.Contains("joined in slot"))
                    {
                        if (match.TeamMode == TeamModes.TeamVS)
                        {
                            var regex = new Regex(@"^(.+) joined in slot (\d+) for team (\w+)\.$");
                            var regexResult = regex.Match(data.Message);
                            if (regexResult.Success)
                            {
                                var player = await Database.Database.GetPlayer(regexResult.Groups[1].Value);
                                var slotNumber = Convert.ToInt32(regexResult.Groups[2].Value);
                                TeamColors? team = null;
                                switch (regexResult.Groups[3].Value)
                                {
                                    case "blue": team = TeamColors.Blue; break;
                                    case "red": team = TeamColors.Red; break;
                                }
                                var slot = RoomSlotsViews.FirstOrDefault(x => x.SlotNumber == slotNumber);
                                if (slot != null)
                                {
                                    slot.Player = player;
                                    slot.Team = team;
                                    slot.Mods = new List<GameMods>();
                                    slot.State = RoomSlotStates.NotReady;
                                }

                                NotifyOfPropertyChange(() => WrongTeamWarningVisible);
                            }
                        }
                        else if (match.TeamMode == TeamModes.HeadToHead || match.TeamMode == TeamModes.BattleRoyale)
                        {
                            var regex = new Regex(@"^(.+) joined in slot (\d+).$");
                            var regexResult = regex.Match(data.Message);
                            if (regexResult.Success)
                            {
                                var player = await Database.Database.GetPlayer(regexResult.Groups[1].Value);
                                var slotNumber = Convert.ToInt32(regexResult.Groups[2].Value);
                                var slot = RoomSlotsViews.FirstOrDefault(x => x.SlotNumber == slotNumber);
                                if (slot != null)
                                {
                                    slot.Player = player;
                                    slot.Mods = new List<GameMods>();
                                    slot.State = RoomSlotStates.NotReady;
                                }
                            }
                        }
                    }
                    if (data.User == "BanchoBot" && data.Message.Contains("changed to"))
                    {
                        var regex = new Regex(@"^(.+) changed to (\w+)$");
                        var regexResult = regex.Match(data.Message);
                        if (regexResult.Success)
                        {
                            var player = regexResult.Groups[1].Value;
                            TeamColors? team = null;
                            switch (regexResult.Groups[2].Value)
                            {
                                case "Blue": team = TeamColors.Blue; break;
                                case "Red": team = TeamColors.Red; break;
                            }
                            var slot = RoomSlotsViews.FirstOrDefault(x => x.PlayerName == player);
                            if (slot != null)
                            {
                                slot.Team = team;
                            }

                            NotifyOfPropertyChange(() => WrongTeamWarningVisible);
                        }
                    }
                    if (data.User == "BanchoBot" && data.Message.Contains("moved to slot"))
                    {
                        var regex = new Regex(@"^(.+) moved to slot (\d+)$");
                        var regexResult = regex.Match(data.Message);
                        if (regexResult.Success)
                        {
                            var player = regexResult.Groups[1].Value;
                            var slotNumber = Convert.ToInt32(regexResult.Groups[2].Value);
                            var oldSlot = RoomSlotsViews.FirstOrDefault(x => x.PlayerName == player);
                            var newSlot = RoomSlotsViews.FirstOrDefault(x => x.SlotNumber == slotNumber);
                            if (oldSlot != null && newSlot != null)
                            {
                                newSlot.Player = oldSlot.Player;
                                newSlot.Team = oldSlot.Team;
                                newSlot.Mods = oldSlot.Mods;
                                newSlot.State = oldSlot.State;
                                oldSlot.Player = null;
                                oldSlot.Team = null;
                                oldSlot.Mods = new List<GameMods>();
                            }

                            NotifyOfPropertyChange(() => WrongTeamWarningVisible);
                        }
                    }
                    if (data.User == "BanchoBot" && data.Message.Contains("left the game."))
                    {
                        var regex = new Regex(@"^(.+) left the game\.$");
                        var regexResult = regex.Match(data.Message);
                        if (regexResult.Success)
                        {
                            var player = regexResult.Groups[1].Value;
                            var slot = RoomSlotsViews.FirstOrDefault(x => x.PlayerName == player);
                            if (slot != null)
                            {
                                slot.Player = null;
                                slot.Team = null;
                                slot.Mods = new List<GameMods>();
                            }

                            NotifyOfPropertyChange(() => WrongTeamWarningVisible);
                        }
                    }
                }
            }
        }

        public void Handle(string message)
        {
            if (message == "UpdateColors")
            {
                localLog.Information("match '{match}' update colors", match.Name);
                Execute.OnUIThread(() =>
                {
                    MultiplayerChat = new FlowDocument
                    {
                        FontFamily = new FontFamily("Lucida Console"),
                        FontSize = 12,
                        TextAlignment = TextAlignment.Left
                    };
                    foreach (var chatMessage in match.ChatMessages)
                    {
                        AddMessageToChat(chatMessage, true);
                    }
                });
            }
            else if (message == "MapPicked")
            {
                NotifyOfPropertyChange(() => WarmupMode);
            }
            else if (message == "UpdateMappoolMap")
            {
                NotifyOfPropertyChange(() => BeatmapsViews);
            }
        }
        #endregion

        #region Properties
        private DataTypes.Match match;

        public string WindowTitle
        {
            get { return match.Name; }
        }

        private double windowHeight;
        public double WindowHeight
        {
            get { return windowHeight; }
            set
            {
                if (value != windowHeight)
                {
                    windowHeight = value;
                    NotifyOfPropertyChange(() => WindowHeight);
                }
            }
        }

        private double windowWidth;
        public double WindowWidth
        {
            get { return windowWidth; }
            set
            {
                if (value != windowWidth)
                {
                    windowWidth = value;
                    NotifyOfPropertyChange(() => WindowWidth);
                }
            }
        }

        public string DialogIdentifier
        {
            get { return "MatchDialogHost" + match.Id; }
        }

        public Visibility TeamVsVisible
        {
            get
            {
                if (match.TeamMode == TeamModes.TeamVS)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
        }

        public Visibility HeadToHeadVisible
        {
            get
            {
                if (match.TeamMode == TeamModes.HeadToHead)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
        }

        public Visibility BRVisible
        {
            get
            {
                if (match.TeamMode == TeamModes.BattleRoyale)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
        }

        public Visibility RoomClosedVisible
        {
            get
            {
                if (match.RoomId == 0)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
        }

        public Visibility RoomOpenVisible
        {
            get
            {
                if (match.RoomId > 0)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
        }

        public Visibility WrongTeamWarningVisible
        {
            get
            {
                if (match.TeamMode != TeamModes.TeamVS)
                    return Visibility.Collapsed;
                foreach (var slot in RoomSlotsViews)
                {
                    if (slot.Player != null && match.TeamRed.Players.Contains(slot.Player) && slot.Team == TeamColors.Blue)
                        return Visibility.Visible;
                    if (slot.Player != null && match.TeamBlue.Players.Contains(slot.Player) && slot.Team == TeamColors.Red)
                        return Visibility.Visible;
                }
                return Visibility.Collapsed;
            }
        }

        public Visibility PrivateRibbonVisible
        {
            get
            {
                if (match.PrivateRoom)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
        }

        public Mappool SelectedMappool
        {
            get { return match.Mappool; }
            set
            {
                if (value != match.Mappool)
                {
                    match.Mappool = value;
                    match.Save();
                    NotifyOfPropertyChange(() => SelectedMappool);
                    NotifyOfPropertyChange(() => BeatmapsViews);
                }
            }
        }

        public Team RollWinnerTeam
        {
            get { return match.RollWinnerTeam; }
            set
            {
                if (value != match.RollWinnerTeam)
                {
                    match.RollWinnerTeam = value;
                    match.Save();
                    NotifyOfPropertyChange(() => RollWinnerTeam);
                }
            }
        }

        public Player RollWinnerPlayer
        {
            get { return match.RollWinnerPlayer; }
            set
            {
                if (value != match.RollWinnerPlayer)
                {
                    match.RollWinnerPlayer = value;
                    match.Save();
                    NotifyOfPropertyChange(() => RollWinnerPlayer);
                }
            }
        }

        public Team FirstPickerTeam
        {
            get { return match.FirstPickerTeam; }
            set
            {
                if (value != match.FirstPickerTeam)
                {
                    match.FirstPickerTeam = value;
                    match.Save();
                    NotifyOfPropertyChange(() => FirstPickerTeam);
                }
            }
        }

        public Player FirstPickerPlayer
        {
            get { return match.FirstPickerPlayer; }
            set
            {
                if (value != match.FirstPickerPlayer)
                {
                    match.FirstPickerPlayer = value;
                    match.Save();
                    NotifyOfPropertyChange(() => FirstPickerPlayer);
                }
            }
        }

        public GameModes GameMode
        {
            get { return match.GameMode; }
            set { }
        }

        public WinConditions WinCondition
        {
            get { return match.WinCondition; }
            set { }
        }

        public int BO
        {
            get { return match.BO; }
            set
            {
                if (value != match.BO)
                {
                    match.BO = value;
                    match.Save();
                    NotifyOfPropertyChange(() => BO);
                }
            }
        }

        public int RoomSize
        {
            get { return match.RoomSize; }
            set { }
        }

        public int TeamSize
        {
            get { return match.TeamSize; }
            set { }
        }

        public int TimerCommand
        {
            get { return match.MpTimerCommand; }
            set
            {
                if (value != match.MpTimerCommand)
                {
                    match.MpTimerCommand = value;
                    match.Save();
                    NotifyOfPropertyChange(() => TimerCommand);
                }
            }
        }

        public int TimerAfterGame
        {
            get { return match.MpTimerAfterGame; }
            set
            {
                if (value != match.MpTimerAfterGame)
                {
                    match.MpTimerAfterGame = value;
                    match.Save();
                    NotifyOfPropertyChange(() => TimerAfterGame);
                }
            }
        }

        public int TimerAfterPick
        {
            get { return match.MpTimerAfterPick; }
            set
            {
                if (value != match.MpTimerAfterPick)
                {
                    match.MpTimerAfterPick = value;
                    match.Save();
                    NotifyOfPropertyChange(() => TimerAfterPick);
                }
            }
        }

        public int PointsForSecondBan
        {
            get { return match.PointsForSecondBan; }
            set
            {
                if (value != match.PointsForSecondBan)
                {
                    match.PointsForSecondBan = value;
                    match.Save();
                    NotifyOfPropertyChange(() => PointsForSecondBan);
                }
            }
        }

        public bool AllPicksFreemod
        {
            get { return match.AllPicksFreemod; }
            set
            {
                if (value != match.AllPicksFreemod)
                {
                    match.AllPicksFreemod = value;
                    match.Save();
                    NotifyOfPropertyChange(() => AllPicksFreemod);
                }
            }
        }

        public bool AllPicksNofail
        {
            get { return match.AllPicksNofail; }
            set
            {
                if (value != match.AllPicksNofail)
                {
                    match.AllPicksNofail = value;
                    match.Save();
                    NotifyOfPropertyChange(() => AllPicksNofail);
                }
            }
        }

        public bool ViewerMode
        {
            get { return match.ViewerMode; }
            set
            {
                if (value != match.ViewerMode)
                {
                    match.ViewerMode = value;
                    match.Save();
                    NotifyOfPropertyChange(() => ViewerMode);
                }
            }
        }

        private bool disableIrcMatchStatus = false;
        public bool DisableIrcMatchStatus
        {
            get { return disableIrcMatchStatus; }
            set
            {
                if (value != disableIrcMatchStatus)
                {
                    disableIrcMatchStatus = value;
                    NotifyOfPropertyChange(() => DisableIrcMatchStatus);
                }
            }
        }

        public string TeamBlueName
        {
            get
            {
                if (match.TeamMode == TeamModes.TeamVS)
                    return match.TeamBlue.Name;
                return "";
            }
        }

        public string TeamRedName
        {
            get
            {
                if (match.TeamMode == TeamModes.TeamVS)
                    return match.TeamRed.Name;
                return "";
            }
        }

        public string RoomLinkName
        {
            get
            {
                return "#mp__" + match.RoomId;
            }
        }

        private FlowDocument multiplayerChat;
        public FlowDocument MultiplayerChat
        {
            get { return multiplayerChat; }
            set
            {
                if (value != multiplayerChat)
                {
                    multiplayerChat = value;
                    NotifyOfPropertyChange(() => MultiplayerChat);
                }
            }
        }

        private string chatMessage;
        public string ChatMessage
        {
            get { return chatMessage; }
            set
            {
                if (value != chatMessage)
                {
                    chatMessage = value;
                    NotifyOfPropertyChange(() => ChatMessage);
                }
            }
        }

        public bool WarmupMode
        {
            get { return match.WarmupMode; }
            set
            {
                if (value != match.WarmupMode)
                {
                    match.WarmupMode = value;
                    match.Save();
                    NotifyOfPropertyChange(() => WarmupMode);
                }
            }
        }
        #endregion

        #region Window Events
        public void Drag(MouseButtonEventArgs e)
        {
            localLog.Information("match '{match}' drag window", match.Name);
            if (e.ChangedButton != MouseButton.Left)
                return;
            ((MatchView)GetView()).DragMove();
        }

        public void MinimizeWindow()
        {
            localLog.Information("match '{match}' minimize window", match.Name);
            ((MatchView)GetView()).WindowState = WindowState.Minimized;
        }

        public Visibility WindowMaximizeVisible
        {
            get
            {
                if (((MatchView)GetView()).WindowState != WindowState.Maximized)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
        }

        public void MaximizeWindow()
        {
            localLog.Information("match '{match}' maximize window", match.Name);
            ((MatchView)GetView()).WindowState = WindowState.Maximized;
            NotifyOfPropertyChange(() => WindowMaximizeVisible);
            NotifyOfPropertyChange(() => WindowRestoreVisible);
        }

        public Visibility WindowRestoreVisible
        {
            get
            {
                if (((MatchView)GetView()).WindowState == WindowState.Maximized)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
        }

        public void RestoreWindow()
        {
            localLog.Information("match '{match}' restore window", match.Name);
            ((MatchView)GetView()).WindowState = WindowState.Normal;
            NotifyOfPropertyChange(() => WindowMaximizeVisible);
            NotifyOfPropertyChange(() => WindowRestoreVisible);
        }

        public void CloseWindow()
        {
            localLog.Information("match '{match}' close window", match.Name);
            TryClose();
        }
        #endregion

        #region Actions
        public void OpenMpLink()
        {
            localLog.Information("match '{match}' open mp link", match.Name);
            System.Diagnostics.Process.Start("https://osu.ppy.sh/community/matches/" + match.RoomId);
        }

        private void SendRoomMessage(string message)
        {
            OsuIrc.OsuIrc.SendMessage("#mp_" + match.RoomId, message);
        }

        public void CreateRoom()
        {
            localLog.Information("match '{match}' create room", match.Name);
            OsuIrc.OsuIrc.SendMessage("BanchoBot", "!mp make " + match.Name);
            match.PrivateRoom = false;
            match.Save();
            NotifyOfPropertyChange(() => PrivateRibbonVisible);
        }

        public void CreatePrivateRoom()
        {
            localLog.Information("match '{match}' create private room", match.Name);
            OsuIrc.OsuIrc.SendMessage("BanchoBot", "!mp makeprivate " + match.Name);
            match.PrivateRoom = true;
            match.Save();
            NotifyOfPropertyChange(() => PrivateRibbonVisible);
        }

        public async void JoinRoom()
        {
            localLog.Information("match '{match}' open join room dialog", match.Name);
            var model = new MatchJoinRoomDialogViewModel();
            var view = ViewLocator.LocateForModel(model, null, null);
            ViewModelBinder.Bind(model, view, null);

            var result = Convert.ToBoolean(await DialogHost.Show(view, DialogIdentifier));

            if (result)
            {
                localLog.Information("match '{match}' join room '{room}'", match.Name, model.RoomId);
                match.RoomId = model.RoomId;
                match.PrivateRoom = model.PrivateRoom;
                match.Status = MatchStatus.InProgress;
                match.Save();
                NotifyOfPropertyChange(() => RoomLinkName);
                NotifyOfPropertyChange(() => RoomClosedVisible);
                NotifyOfPropertyChange(() => RoomOpenVisible);
                NotifyOfPropertyChange(() => PrivateRibbonVisible);
                OsuIrc.OsuIrc.JoinChannel("#mp_" + model.RoomId);
            }
        }

        public async void CloseRoom()
        {
            localLog.Information("match '{match}' open close room dialog", match.Name);
            var model = new MatchCloseRoomDialogViewModel();
            var view = ViewLocator.LocateForModel(model, null, null);
            ViewModelBinder.Bind(model, view, null);

            var result = Convert.ToBoolean(await DialogHost.Show(view, DialogIdentifier));

            if (result)
            {
                localLog.Information("match '{match}' close room", match.Name);
                SendRoomMessage("!mp close");
                match.RoomId = 0;
                match.Status = MatchStatus.Finished;
                match.PrivateRoom = false;
                match.Save();
                NotifyOfPropertyChange(() => RoomLinkName);
                NotifyOfPropertyChange(() => RoomClosedVisible);
                NotifyOfPropertyChange(() => RoomOpenVisible);
                NotifyOfPropertyChange(() => PrivateRibbonVisible);
            }
        }

        public void SwitchPlayers()
        {
            localLog.Information("match '{match}' switch players", match.Name);
            foreach (var player in match.GetPlayerList())
            {
                OsuIrc.OsuIrc.SendMessage("BanchoBot", "!mp switch #" + player.Id);
            }
        }

        public void SwitchPlayersBack()
        {
            localLog.Information("match '{match}' switch players back", match.Name);
            foreach (var player in match.GetPlayerList())
            {
                OsuIrc.OsuIrc.SendMessage("BanchoBot", "!mp switch #" + player.Id, true);
            }
        }

        public void InvitePlayers()
        {
            localLog.Information("match '{match}' invite players", match.Name);
            foreach (var player in match.GetPlayerList())
            {
                SendRoomMessage("!mp invite #" + player.Id);
            }
        }

        public void SendWelcomeString()
        {
            localLog.Information("match '{match}' send welcome string", match.Name);
            if (match.TeamMode == TeamModes.TeamVS)
            {
                SendRoomMessage(string.Format(Properties.Resources.MatchViewModel_TeamRoomSetupMessage, TeamRedName, match.TeamSize, TeamBlueName, match.TeamSize + 1, match.TeamSize * 2));
            }

            SendRoomMessage(match.Tournament.WelcomeString);
        }

        public void SendMessage()
        {
            if (string.IsNullOrEmpty(ChatMessage))
                return;
            localLog.Information("match '{match}' send irc message '{message}'", match.Name, ChatMessage);
            var message = ChatMessage;
            ChatMessage = "";
            SendRoomMessage(message);
        }

        public void SendSettings()
        {
            localLog.Information("match '{match}' send settings", match.Name);
            SendRoomMessage("!mp settings");
        }

        public void StartTimer()
        {
            localLog.Information("match '{match}' start timer", match.Name);
            if (match.MpTimerCommand > 0)
                SendRoomMessage("!mp timer " + match.MpTimerCommand);
        }

        public void StartGame()
        {
            localLog.Information("match '{match}' start game", match.Name);
            SendRoomMessage("!mp start 10");
        }

        public async void AbortMatch()
        {
            localLog.Information("match '{match}' open abort game dialog", match.Name);
            var model = new MatchAbortMapDialogViewModel();
            var view = ViewLocator.LocateForModel(model, null, null);
            ViewModelBinder.Bind(model, view, null);

            var result = Convert.ToBoolean(await DialogHost.Show(view, DialogIdentifier));

            if (result)
            {
                localLog.Information("match '{match}' abort game", match.Name);
                SendRoomMessage("!mp abort");
            }
        }

        public async void SetPassword()
        {
            localLog.Information("match '{match}' open set password dialog", match.Name);
            var model = new MatchPasswordDialogViewModel();
            var view = ViewLocator.LocateForModel(model, null, null);
            ViewModelBinder.Bind(model, view, null);

            var result = Convert.ToBoolean(await DialogHost.Show(view, DialogIdentifier));

            if (result)
            {
                if (string.IsNullOrEmpty(model.Password))
                    localLog.Information("match '{match}' remove password", match.Name);
                else
                    localLog.Information("match '{match}' set password", match.Name);
                SendRoomMessage("!mp password " + model.Password);
            }
        }

        public async void OpenPickOverview()
        {
            localLog.Information("match '{match}' open pick overview dialog", match.Name);
            var model = new MatchPickOverviewDialogViewModel(match, WindowHeight);
            var view = ViewLocator.LocateForModel(model, null, null);
            ViewModelBinder.Bind(model, view, null);

            await DialogHost.Show(view, DialogIdentifier);
            model.OnDeactivate();
            NotifyOfPropertyChange(() => BeatmapsViews);
        }

        private void AddMessageToChat(IrcMessage message, bool init)
        {
            localLog.Information("match '{match}' add message to chat", match.Name);
            if (!init)
                match.ChatMessages.Add(message);

            var scrollToEnd = false;
            var view = (MatchView)GetView();
            var chatWindow = view.ChatWindow;
            var scrollViewer = FindScroll(chatWindow);
            if (scrollViewer != null)
            {
                if (scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
                    scrollToEnd = true;
            }

            var scrollEventsToEnd = false;
            var eventsScrollViewer = view.BanchoEventsScrollViewer;
            if (eventsScrollViewer.VerticalOffset == eventsScrollViewer.ScrollableHeight)
                scrollEventsToEnd = true;

            var brush = new SolidColorBrush();
            if (message.User == Settings.IrcUsername)
                brush.Color = Settings.UserColors.First(x => x.Key == "Self").Color;
            else if (message.User == "BanchoBot")
                brush.Color = Settings.UserColors.First(x => x.Key == "BanchoBot").Color;
            else
                brush.Color = Settings.UserColors.First(x => x.Key == "Default").Color;

            var slotMovementCombined = false;
            Paragraph paragraph = null;
            if (message.User == "BanchoBot" && (message.Message.Contains(" moved to slot ") || message.Message.Contains(" changed to ")) && MultiplayerChat.Blocks.Count > 0)
            {
                var lastMessageParagraph = (Paragraph)MultiplayerChat.Blocks.LastBlock;
                var lastMessageInline = (Run)lastMessageParagraph.Inlines.FirstInline;
                if (lastMessageInline.Text.Contains(" moved to slot ") || lastMessageInline.Text.Contains(" changed to "))
                {
                    var tooltip = "";
                    if (lastMessageParagraph.ToolTip != null)
                    {
                        tooltip = lastMessageParagraph.ToolTip.ToString() + Environment.NewLine + $"[{message.Timestamp.ToString("HH:mm")}] {message.User.PadRight(15)} {message.Message}";
                    }
                    else
                    {
                        tooltip = lastMessageInline.Text + Environment.NewLine + $"[{message.Timestamp.ToString("HH:mm")}] {message.User.PadRight(15)} {message.Message}";
                    }
                    paragraph = new Paragraph(new Run($"[{message.Timestamp.ToString("HH:mm")}] {message.User.PadRight(15)} {message.Message} (...)")) { Margin = new Thickness(202, 0, 0, 0), TextIndent = -202, Foreground = brush, ToolTip = tooltip };
                    MultiplayerChat.Blocks.Remove(MultiplayerChat.Blocks.LastBlock);
                    slotMovementCombined = true;
                }
            }
            if (!slotMovementCombined)
                paragraph = new Paragraph(new Run($"[{message.Timestamp.ToString("HH:mm")}] {message.User.PadRight(15)} {message.Message}")) { Margin = new Thickness(202, 0, 0, 0), TextIndent = -202, Foreground = brush };

            MultiplayerChat.Blocks.Add(paragraph);
            NotifyOfPropertyChange(() => MultiplayerChat);
            NotifyOfPropertyChange(() => BanchoEventsViews);

            if (scrollToEnd)
                scrollViewer.ScrollToEnd();

            if (scrollEventsToEnd)
                eventsScrollViewer.ScrollToEnd();
        }

        public void ChatMessageKeyDown(ActionExecutionContext context)
        {
            var keyArgs = context.EventArgs as KeyEventArgs;
            if (keyArgs != null && keyArgs.Key == Key.Enter)
                SendMessage();
        }

        private ScrollViewer FindScroll(FlowDocumentScrollViewer flowDocumentScrollViewer)
        {
            if (VisualTreeHelper.GetChildrenCount(flowDocumentScrollViewer) == 0)
            {
                return null;
            }

            // Border is the first child of first child of a ScrolldocumentViewer
            DependencyObject firstChild = VisualTreeHelper.GetChild(flowDocumentScrollViewer, 0);
            if (firstChild == null)
            {
                return null;
            }

            Decorator border = VisualTreeHelper.GetChild(firstChild, 0) as Decorator;

            if (border == null)
            {
                return null;
            }

            return border.Child as ScrollViewer;
        }

        private void SendRoomStatus()
        {
            localLog.Information("match '{match}' send room status", match.Name);

            if (DisableIrcMatchStatus)
                return;

            if (match.TeamMode == TeamModes.TeamVS)
            {
                SendRoomMessage($"{match.TeamRed.Name} : {match.TeamRedPoints} | {match.TeamBluePoints} : {match.TeamBlue.Name}");

                if (match.TeamRedPoints * 2 > match.BO)
                {
                    SendRoomMessage(string.Format(Properties.Resources.MatchViewModel_WinMessage, match.TeamRed.Name));
                }
                else if (match.TeamBluePoints * 2 > match.BO)
                {
                    SendRoomMessage(string.Format(Properties.Resources.MatchViewModel_WinMessage, match.TeamBlue.Name));
                }
                else
                {
                    if (match.PointsForSecondBan > 0)
                    {
                        if (match.Games.Last().TeamRedWon && match.TeamRedPoints == match.PointsForSecondBan)
                        {
                            SendRoomMessage(string.Format(Properties.Resources.MatchViewModel_SecondBanMessage, match.TeamBlue.Name));
                        }
                        else if (match.Games.Last().TeamBlueWon && match.TeamBluePoints == match.PointsForSecondBan)
                        {
                            SendRoomMessage(string.Format(Properties.Resources.MatchViewModel_SecondBanMessage, match.TeamRed.Name));
                        }
                    }

                    if (match.TeamRedPoints * 2 == match.BO - 1 && match.TeamBluePoints * 2 == match.BO - 1)
                    {
                        SendRoomMessage(Properties.Resources.MatchViewModel_TiebreakerMessage);
                    }
                    else
                    {
                        if (match.Picks.Count > 0)
                        {
                            if (match.Picks.Last().Team == match.TeamRed)
                            {
                                if (match.Tournament.TeamSize == 1)
                                    SendRoomMessage(string.Format(Properties.Resources.MatchViewModel_NextPlayerPickMessage, match.TeamBlue.Players[0].Name));
                                else
                                    SendRoomMessage(string.Format(Properties.Resources.MatchViewModel_NextTeamPickMessage, match.TeamBlue.Name));
                            }
                            else
                            {
                                if (match.Tournament.TeamSize == 1)
                                    SendRoomMessage(string.Format(Properties.Resources.MatchViewModel_NextPlayerPickMessage, match.TeamRed.Players[0].Name));
                                else
                                    SendRoomMessage(string.Format(Properties.Resources.MatchViewModel_NextTeamPickMessage, match.TeamRed.Name));
                            }
                        }
                    }

                    if (match.MpTimerAfterGame > 0)
                    {
                        SendRoomMessage("!mp timer " + match.MpTimerAfterGame);
                    }
                }
            }
            else if (match.TeamMode == TeamModes.HeadToHead)
            {
                SendRoomMessage(Properties.Resources.MatchViewModel_RoomRankingMessage);

                var i = 1;
                foreach (var player in match.Players.OrderByDescending(x => x.Value))
                {
                    SendRoomMessage(string.Format(Properties.Resources.MatchViewModel_PlayerPointsMessage, i, player.Key.Name, player.Value));
                    i++;
                }

                var first = match.Players.OrderByDescending(x => x.Value).First();
                if (first.Value * 2 > match.BO)
                {
                    SendRoomMessage(string.Format(Properties.Resources.MatchViewModel_WinMessage, first.Key.Name));
                }
                else
                {
                    if (match.BO > 0 && match.Players.Count == 2 && match.Players.Values.ElementAt(0) * 2 == match.BO - 1 && match.Players.Values.ElementAt(1) * 2 == match.BO - 1)
                    {
                        SendRoomMessage(Properties.Resources.MatchViewModel_TiebreakerMessage);
                    }
                    else
                    {
                        if (match.Picks.Count > 0)
                        {
                            for (var j = 0; j < match.Players.Count; j++)
                            {
                                if (match.Players.ElementAt(j).Key == match.Picks.Last().Player)
                                {
                                    if (j < match.Players.Count - 1)
                                        SendRoomMessage(string.Format(Properties.Resources.MatchViewModel_NextPlayerPickMessage, match.Players.ElementAt(j + 1).Key.Name));
                                    else
                                        SendRoomMessage(string.Format(Properties.Resources.MatchViewModel_NextPlayerPickMessage, match.Players.ElementAt(0).Key.Name));
                                    break;
                                }
                            }
                        }
                    }

                    if (match.MpTimerAfterGame > 0)
                    {
                        SendRoomMessage("!mp timer " + match.MpTimerAfterGame);
                    }
                }
            }
        }

        private void PlayNotificationSound()
        {
            localLog.Information("match '{match}' play notification sound", match.Name);
            if (Settings.EnableNotifications)
                NotificationPlayer.PlayNotification();
        }

        public async Task UpdateScore(bool newGameExpected = false)
        {
            localLog.Information("match '{match}' update scores", match.Name);
            await match.UpdateScores(newGameExpected);
            if (match.TeamMode == TeamModes.TeamVS)
                NotifyOfPropertyChange(() => TeamsViews);
            if (match.TeamMode == TeamModes.HeadToHead)
                NotifyOfPropertyChange(() => PlayersViews);
            if (match.TeamMode == TeamModes.BattleRoyale)
                NotifyOfPropertyChange(() => BRTeamsViews);
        }

        private void SendRoomSet()
        {
            localLog.Information("match '{match}' set room {mode}", match.Name, $"{Utils.ConvertTeamModeToMpNumber(match.TeamMode)} {(int)match.WinCondition} {match.RoomSize}");
            SendRoomMessage($"!mp set {Utils.ConvertTeamModeToMpNumber(match.TeamMode)} {(int)match.WinCondition} {match.RoomSize}");
        }

        public async void EditRoomOptions()
        {
            localLog.Information("match '{match}' open room options dialog", match.Name);
            var model = new MatchRoomOptionsDialogViewModel();
            var view = ViewLocator.LocateForModel(model, null, null);
            ViewModelBinder.Bind(model, view, null);

            model.GameMode = match.GameMode;
            model.WinCondition = match.WinCondition;
            model.TeamSize = match.TeamSize;
            model.RoomSize = match.RoomSize;

            var result = Convert.ToBoolean(await DialogHost.Show(view, DialogIdentifier));

            if (result)
            {
                localLog.Information("match '{match}' set room options", match.Name);
                match.GameMode = model.GameMode;
                match.WinCondition = model.WinCondition;
                match.TeamSize = model.TeamSize;
                match.RoomSize = model.RoomSize;
                match.Save();
                NotifyOfPropertyChange(() => GameMode);
                NotifyOfPropertyChange(() => WinCondition);
                NotifyOfPropertyChange(() => RoomSize);
                NotifyOfPropertyChange(() => TeamSize);
                SendRoomSet();
            }
        }

        public void SendBanRecap()
        {
            localLog.Information("match '{match}' send ban recap", match.Name);
            if (!match.ViewerMode)
            {
                DiscordApi.SendMatchBanRecap(match);
            }
        }

        public void SendPickRecap()
        {
            localLog.Information("match '{match}' send pick recap", match.Name);
            if (!match.ViewerMode)
            {
                DiscordApi.SendMatchPickRecap(match);
            }
        }

        public void SendGameRecap()
        {
            localLog.Information("match '{match}' send game recap", match.Name);
            if (!match.ViewerMode)
            {
                DiscordApi.SendGameRecap(match);
            }
        }

        public void KickBRTeam(MatchBRTeamViewModel model)
        {
            KickBRTeam(model.Team);
        }

        public void KickLastBRTeam()
        {
            foreach (var team in match.TeamsBR.Where(x => x.Value == 0))
            {
                if (RoomSlotsViews.Any(x => team.Key.Players.Contains(x.Player)))
                {
                    KickBRTeam(team.Key);
                }
            }
        }

        public void KickBRTeam(Team team)
        {
            localLog.Information("match '{match}' kick team '{team}'", match.Name, team.Name);
            var slots = new List<int>();
            foreach (var player in team.Players)
            {
                var slot = RoomSlotsViews.FirstOrDefault(x => x.Player == player);
                if (slot != null)
                {
                    slots.Add(slot.SlotNumber);
                    slot.Player = null;
                    slot.Team = null;
                    slot.Mods = new List<GameMods>();
                    OsuIrc.OsuIrc.SendMessage("#mp_" + match.RoomId, $"!mp kick #{player.Id}");
                }
            }
            foreach (var slot in RoomSlotsViews.Where(x => x.Player != null).OrderByDescending(x => x.SlotNumber))
            {
                if (slots.Count <= 0)
                    break;
                OsuIrc.OsuIrc.SendMessage("#mp_" + match.RoomId, $"!mp move #{slot.Player.Id} {slots[0]}");
                slots.RemoveAt(0);
            }
        }

        public void PickNextMap()
        {
            foreach (var map in BeatmapsViews)
            {
                if (map.CanBanOrPick)
                {
                    map.Pick();
                    NotifyOfPropertyChange(() => BeatmapsViews);
                    break;
                }
            }
        }

        public void PickRandomMap()
        {
            var pickableMaps = BeatmapsViews.Where(x => x.CanBanOrPick && !x.Beatmap.Mods.Contains(GameMods.TieBreaker));
            if (pickableMaps.Count() < 1)
                return;
            var random = new Random();
            int randomMapIndex = random.Next(0, pickableMaps.Count() - 1);
            var randomMap = pickableMaps.ElementAt(randomMapIndex);
            randomMap.Pick();
            NotifyOfPropertyChange(() => BeatmapsViews);
        }

        public async void OpenTeamWindow(MatchTeamViewModel teamViewModel)
        {
            var team = teamViewModel.Team;
            localLog.Information("match '{match}' open team dialog of {team}", match.Name, team.Name);
            var model = new MatchTeamDialogViewModel(match, team);
            var view = ViewLocator.LocateForModel(model, null, null);
            ViewModelBinder.Bind(model, view, null);

            await DialogHost.Show(view, DialogIdentifier);
        }
        #endregion
    }
}
