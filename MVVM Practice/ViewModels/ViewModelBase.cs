using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace MVVM_Practice.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        protected void OnPropertyChanged(string PropertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }
    }}