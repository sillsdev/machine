using System;
using Eto.Forms;

namespace SIL.Machine.Translation.TestApp
{
    public class RelayCommand : RelayCommand<object>
    {
        public RelayCommand(Action execute) : base(o => execute()) { }

        public RelayCommand(Action execute, Func<bool> canExecute) : base(o => execute(), o => canExecute()) { }
    }
}
