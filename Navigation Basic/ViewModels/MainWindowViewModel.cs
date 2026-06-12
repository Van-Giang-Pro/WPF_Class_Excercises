using Navigation_Basic.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Navigation_Basic.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            CurrentViewModel = new AutoViewModel();
            UpdateCurrentViewModel = new UpdateCurrentViewModel(this);
        }
        private ViewModelBase _currentViewModel;
        public ViewModelBase CurrentViewModel
        {
            get { return _currentViewModel; }
            set
            {
                _currentViewModel = value;
                OnPropertyChanged(nameof(CurrentViewModel));
            }
        }
        public ICommand UpdateCurrentViewModel { get; }
    }   
}
