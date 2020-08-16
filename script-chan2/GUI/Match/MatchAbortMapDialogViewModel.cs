﻿using Caliburn.Micro;
using MaterialDesignThemes.Wpf;

namespace script_chan2.GUI
{
    public class MatchAbortMapDialogViewModel : Screen
    {
        #region Actions
        public void DialogEscape()
        {
            DialogHost.CloseDialogCommand.Execute(false, null);
        }
        #endregion
    }
}