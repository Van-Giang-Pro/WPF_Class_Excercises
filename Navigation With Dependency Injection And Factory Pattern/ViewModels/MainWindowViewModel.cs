using CommunityToolkit.Mvvm.Input;
using Navigation_With_Dependency_Injection_And_Factory_Pattern.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Navigation_With_Dependency_Injection_And_Factory_Pattern.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private INavigationService _navigationService;

        public MainWindowViewModel(INavigationService navigationService)
        {
            this._navigationService = navigationService;
            NavigationService = navigationService;
            NavigateToAutoCommand = new RelayCommand(() => NavigationService.NavigateTo<AutoViewModel>());
            NavigateToTeachingCommand = new RelayCommand(() => NavigationService.NavigateTo<TeachingViewModel>());
        }

        public RelayCommand NavigateToAutoCommand { get; set; }

        public RelayCommand NavigateToTeachingCommand { get; set; }

        public INavigationService NavigationService
        {
            get => _navigationService;
            set => SetProperty(ref _navigationService, value);
        }
    }
}
