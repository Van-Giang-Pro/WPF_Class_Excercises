using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using System.Windows.Input;

namespace Navigation_Basic.Commands
{
    public class UpdateCurrentViewModel : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            throw new NotImplementedException();
        }
    }
}
