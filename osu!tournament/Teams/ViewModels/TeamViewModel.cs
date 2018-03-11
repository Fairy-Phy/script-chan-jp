﻿using Caliburn.Micro;
using Osu.Api;
using Osu.Mvvm.Miscellaneous;
using Osu.Utils.TeamsOv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Osu.Mvvm.Teams.ViewModels
{
    public class TeamViewModel : Screen
    {
        #region Attributes
        /// <summary>
        /// The team object
        /// </summary>
        private TeamOv Team { get; set; }
        
        /// <summary>
        /// The list of player view models
        /// </summary>
        protected List<PlayerViewModel> players;
        #endregion


        public TeamViewModel(TeamOv team)
        {
            this.Team = team;

            players = new List<PlayerViewModel>();

            foreach (PlayerOv playerOv in Team.Players)
                players.Add(new PlayerViewModel(this, playerOv));
        }

        #region Properties
        /// <summary>
        /// Beatmaps property
        /// </summary>
        public IObservableCollection<PlayerViewModel> Players
        {
            get
            {
                return new BindableCollection<PlayerViewModel>(players.OrderBy(entry => entry.Player.Name));
            }
        }
        #endregion



        #region Public Methods
        /// <summary>
        /// Updates the view model
        /// </summary>
        public void Update()
        {
            NotifyOfPropertyChange(() => Players);
        }

        public override string DisplayName
        {
            get
            {
                return Team.Name;
            }
        }

        /// <summary>
        /// Add player(s)
        /// </summary>
        public async void AddPlayers()
        {
            // If the osu!api is not valid
            if (!OsuApi.Valid)
                // Error
                Dialog.ShowDialog("Whoops!", "The osu!api key is not valid!");
            // Else
            else
            {
                // Get an input
                string input = await Dialog.ShowInput("Add player(s)", "Enter the players id or name. You can use ';' as a separator");

                // If something was entered
                if (!string.IsNullOrEmpty(input))
                {
                    List<string> playerslist = new List<string>();
                    if (input.Contains(";"))
                    {
                        foreach (string p in input.Split(';'))
                        {
                            playerslist.Add(p);
                        }
                    }
                    else
                    {
                        playerslist.Add(input);
                    }

                    foreach (string player in playerslist)
                    {
                        // If the input is not valid
                        long id = -1;
                        bool isNumber = long.TryParse(player, out id);
                         
                        // If the player already exists in the list
                        if(Team.Players.Exists(x => x.Name == player || x.Id == id))
                        {
                            // Error
                            Dialog.ShowDialog("Whoops!", "The player already exists!");
                        }
                        else
                        {
                            OsuUser osu_player;

                            // Get the player
                            if (isNumber)
                                osu_player = await OsuApi.GetUser(id, OsuMode.Standard, false);
                            else
                                osu_player = await OsuApi.GetUser(player, OsuMode.Standard, false);

                            // Beatmap doesn't exist
                            if (osu_player == null)
                                // Error
                                Dialog.ShowDialog("Whoops!", "The player does not exist!");
                            // Beatmap exists
                            else
                            {
                                // Create a new player wrapper
                                PlayerOv playerOv = new PlayerOv();

                                playerOv.Id = osu_player.Id;
                                playerOv.Name = osu_player.Username;
                                playerOv.Country = osu_player.Country;

                                // Add this player to the team
                                Team.Players.Add(playerOv);

                                // Create a new view model and add it to our vm list
                                players.Add(new PlayerViewModel(this, playerOv));

                                // Beatmap list has changed
                                NotifyOfPropertyChange(() => Players);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Deletes a beatmap
        /// </summary>
        public void Delete(PlayerViewModel model)
        {
            players.Remove(model);
            Team.Players.RemoveAll(x => x.Id == model.Player.Id);

            NotifyOfPropertyChange(() => Players);
        }
        #endregion
    }
}