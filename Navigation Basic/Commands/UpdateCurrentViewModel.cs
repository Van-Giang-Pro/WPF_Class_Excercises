using Navigation_Basic.ViewModels;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using System.Windows.Input;

namespace Navigation_Basic.Commands
{
    public class UpdateCurrentViewModel : ICommand // Đây không phải là kế thừa nha
    {
        private readonly MainWindowViewModel _viewModel;

        public UpdateCurrentViewModel(MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            if (parameter.ToString() == "Auto") _viewModel.CurrentViewModel = new AutoViewModel();
            else if (parameter.ToString() == "Vision") _viewModel.CurrentViewModel = new VisionViewModel();
        }
    }
}
