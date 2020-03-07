﻿using Caliburn.Micro;
using script_chan2.DataTypes;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace script_chan2.GUI
{
    public class TeamsViewModel : Screen, IHandle<string>
    {
        #region Teams list
        public BindableCollection<Team> Teams { get; set; }

        public BindableCollection<TeamListItemViewModel> TeamsViews
        {
            get
            {
                var list = new BindableCollection<TeamListItemViewModel>();
                foreach (var team in Teams)
                    list.Add(new TeamListItemViewModel(team));
                return list;
            }
        }
        #endregion

        #region Constructor
        protected override void OnActivate()
        {
            Reload();
            Events.Aggregator.Subscribe(this);
        }
        #endregion

        #region Events
        public void Reload()
        {
            Teams = new BindableCollection<Team>();
            foreach (var team in Database.Database.Teams.OrderBy(x => x.Name))
            {
                if (Settings.DefaultTournament != null && team.Tournament != Settings.DefaultTournament)
                    continue;
                Teams.Add(team);
            }
            NotifyOfPropertyChange(() => Teams);
            NotifyOfPropertyChange(() => TeamsViews);
        }

        public void Handle(string message)
        {
            if (message.ToString() == "DeleteTeam")
                Reload();
        }
        #endregion

        #region Filter
        public Tournament FilterTournament
        {
            get { return Settings.DefaultTournament; }
            set
            {
                if (value != Settings.DefaultTournament)
                {
                    Log.Information("GUI team list set tournament filter");
                    Settings.DefaultTournament = value;
                    NotifyOfPropertyChange(() => FilterTournament);
                    Reload();
                }
            }
        }
        #endregion

        #region New team dialog
        private string newTeamName;
        public string NewTeamName
        {
            get { return newTeamName; }
            set
            {
                if (value != newTeamName)
                {
                    newTeamName = value;
                    NotifyOfPropertyChange(() => NewTeamName);
                    NotifyOfPropertyChange(() => NewTeamSaveEnabled);
                }
            }
        }

        public BindableCollection<Tournament> Tournaments
        {
            get
            {
                var list = new BindableCollection<Tournament>();
                foreach (var tournament in Database.Database.Tournaments.OrderBy(x => x.Name))
                    list.Add(tournament);
                return list;
            }
        }

        private Tournament newTeamTournament;
        public Tournament NewTeamTournament
        {
            get { return newTeamTournament; }
            set
            {
                if (value != newTeamTournament)
                {
                    newTeamTournament = value;
                    NotifyOfPropertyChange(() => NewTeamTournament);
                    NotifyOfPropertyChange(() => NewTeamSaveEnabled);
                }
            }
        }

        public bool NewTeamSaveEnabled
        {
            get
            {
                if (string.IsNullOrEmpty(newTeamName))
                    return false;
                if (Teams.Any(x => x.Name == newTeamName && x.Tournament == newTeamTournament))
                    return false;
                return true;
            }
        }

        public void NewTeamDialogOpened()
        {
            Log.Information("GUI new team dialog open");
            NewTeamName = "";
            NewTeamTournament = Settings.DefaultTournament;
        }

        public void NewTeamDialogClosed()
        {
            Log.Information("GUI new team '{name}' save", NewTeamName);
            var team = new Team(NewTeamTournament, NewTeamName);
            team.Save();
            Settings.DefaultTournament = NewTeamTournament;
            NotifyOfPropertyChange(() => NewTeamTournament);
            Reload();
        }
        #endregion
    }
}
