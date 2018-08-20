// Copyright 2018 Dan Pike
// Use of this source code is governed by a MIT license that can be
// found in the LICENSE file.

using System;
using System.Windows.Input;

namespace V8Builder
{
    /// <summary>
    /// WPF command handler wrapper code. Set the Action, IsExecutable,
    /// GuiThread and GuiDispatcher properties as appropriate
    /// </summary>
    public class CommandHandler : ICommand
    {
        public Action Action { get; set; }

        public bool Enabled
        {
            get => enabled_;
            set
            {
                enabled_ = value;
                CanExecuteChanged?.Invoke(this, new EventArgs());
            }
        }

        public bool CanExecute(object parameter) => (Action != null) && Enabled;

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) => Action();

        private bool enabled_ = true;
    }
}