using Navigation_With_Dependency_Injection_And_Factory_Pattern.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Navigation_With_Dependency_Injection_And_Factory_Pattern.Services
{
    public class NavigationService : ViewModelBase, INavigationService
    {
        public NavigationService(Func<Type, ViewModelBase> viewModelFactory)
        {
            this._viewModelFactory = viewModelFactory;
        }

        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            private set => SetProperty(ref _currentViewModel, value);
        }

        public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
        {
            ViewModelBase viewModel = _viewModelFactory.Invoke(typeof(TViewModel));
            CurrentViewModel = viewModel;
        }

        #region Field(s)
        private ViewModelBase _currentViewModel;
        private readonly Func<Type, ViewModelBase> _viewModelFactory;
        #endregion
    }
}
