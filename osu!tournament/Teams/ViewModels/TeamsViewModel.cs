﻿using Caliburn.Micro;
using Osu.Mvvm.Miscellaneous;
using Osu.Utils.TeamsOv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Osu.Mvvm.Teams.ViewModels
{
    public class TeamsViewModel : Conductor<TeamViewModel>.Collection.OneActive
    {
        #region Attributes
        /// <summary>
        /// The list of teams
        /// </summary>
        private IObservableCollection<TeamViewModel> teams;

        /// <summary>
        /// The selected team
        /// </summary>
        private TeamViewModel selected;
        #endregion


        public TeamsViewModel()
        {
            DisplayName = "Teams";
            teams = new BindableCollection<TeamViewModel>();
            selected = null;

            foreach (TeamOv team in TeamManager.Teams)
                teams.Add(new TeamViewModel(team));
        }

        #region Properties
        /// <summary>
        /// Teams property
        /// </summary>
        public IObservableCollection<TeamViewModel> Teams
        {
            get
            {
                return teams;
            }
        }

        /// <summary>
        /// Selected mappool property
        /// </summary>
        public TeamViewModel SelectedTeam
        {
            get
            {
                return selected;
            }
            set
            {
                if (value != selected)
                {
                    selected = value;

                    // Activate the selected item
                    ActivateItem(selected);

                    // Selected mappool has changed
                    NotifyOfPropertyChange(() => SelectedTeam);

                    // We have a new mappool that can be deleted
                    NotifyOfPropertyChange(() => CanDeleteTeam);
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Checks if we can delete a team
        /// </summary>
        /// <returns>true/false</returns>
        public bool CanDeleteTeam
        {
            get
            {
                return SelectedTeam != null;
            }
        }

        /// <summary>
        /// Adds a new team
        /// </summary>
        public async void AddTeam()
        {
            // Get the team name
            string name = await Dialog.ShowInput("Add a team", "Enter the team name");

            // If the user entered a name
            if (!string.IsNullOrEmpty(name))
            {
                // Create a new team
                TeamOv team = TeamManager.Get(name);

                TeamViewModel model = new TeamViewModel(team);

                // Add this model to the team list
                teams.Add(model);

                Log.Info("Adding team \"" + name + "\"");

                // Change the currently selected team
                SelectedTeam = model;

                // team list has changed
                NotifyOfPropertyChange(() => Teams);
            }
        }

        /// <summary>
        /// Deletes the currently selected team
        /// </summary>
        public void DeleteTeam()
        {
            // Get the currently selected team
            TeamViewModel team = SelectedTeam;

            // Remove this team from our lists of teams
            teams.Remove(team);
            TeamManager.Remove(team.DisplayName);

            Log.Info("Deleting team \"" + team.DisplayName + "\"");

            // Select the first team from our list (Or null if there is no team)
            SelectedTeam = teams.Count == 0 ? null : teams[0];

            // team list has changed
            NotifyOfPropertyChange(() => Teams);
        }
        #endregion
    }
}