using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace MVVM_Practice
{
    public class MainWindowNewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string PropertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }
    }
}
