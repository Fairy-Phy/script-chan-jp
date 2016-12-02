﻿using Caliburn.Micro;
using Osu.Mvvm.Miscellaneous;
using Osu.Scores;

namespace Osu.Mvvm.Mappools.ViewModels
{
    /// <summary>
    /// Represents a mappools view model
    /// </summary>
    public class MappoolsViewModel : Conductor<MappoolViewModel>.Collection.OneActive
    {
        #region Attributes
        /// <summary>
        /// The list of mappools
        /// </summary>
        private IObservableCollection<MappoolViewModel> mappools;

        /// <summary>
        /// The selected mappool
        /// </summary>
        private MappoolViewModel selected;
        #endregion

        #region Constructor
        /// <summary>
        /// Default constructor
        /// </summary>
        public MappoolsViewModel()
        {
            DisplayName = "Mappools";
            mappools = new BindableCollection<MappoolViewModel>();
            selected = null;

            foreach (Mappool mappool in Mappool.Mappools)
                mappools.Add(new MappoolViewModel(mappool));
        }
        #endregion

        #region Properties
        /// <summary>
        /// Mappools property
        /// </summary>
        public IObservableCollection<MappoolViewModel> Mappools
        {
            get
            {
                return mappools;
            }
        }

        /// <summary>
        /// Selected mappool property
        /// </summary>
        public MappoolViewModel SelectedMappool
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
                    NotifyOfPropertyChange(() => SelectedMappool);

                    // We have a new mappool that can be deleted
                    NotifyOfPropertyChange(() => CanDeleteMappool);
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Adds a new mappool
        /// </summary>
        public async void AddMappool()
        {
            // Get the mappool name
            string name = await Dialog.ShowInput("Add a mappool", "Enter the mappool name");

            // If the user entered a name
            if (!string.IsNullOrEmpty(name))
            {
                // Create a new mappool
                Mappool mappool = Mappool.Get(name);
                mappool.Name = name;

                MappoolViewModel model = new MappoolViewModel(mappool);

                // Add this model to the mappool list
                mappools.Add(model);

                Log.Info("Adding mappool \"" + name + "\"");

                // Change the currently selected mappool
                SelectedMappool = model;

                // Mappool list has changed
                NotifyOfPropertyChange(() => Mappools);
            }
        }

        /// <summary>
        /// Checks if we can delete a mappool
        /// </summary>
        /// <returns>true/false</returns>
        public bool CanDeleteMappool
        {
            get
            {
                return SelectedMappool != null;
            }
        }

        /// <summary>
        /// Deletes the currently selected mappool
        /// </summary>
        public void DeleteMappool()
        {
            // Get the currently selected mappool
            MappoolViewModel mappool = SelectedMappool;

            // Remove this mappool from our lists of mappools
            mappools.Remove(mappool);
            Mappool.Remove(mappool.DisplayName);

            Log.Info("Deleting mappool \"" + mappool.DisplayName + "\"");

            // Select the first mappool from our list (Or null if there is no mappool)
            SelectedMappool = mappools.Count == 0 ? null : mappools[0];

            // Mappool list has changed
            NotifyOfPropertyChange(() => Mappools);
        }
        #endregion
    }
}
