using System;
using System.Collections.Generic;
using System.Text;

namespace Navigation_Basic.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private ViewModelBase _currentViewModel;

        public ViewModelBase CurrentViewModel
        {
            get { return _currentViewModel; }
            set { _currentViewModel = value; }
        }
    }   
}
