using Navigation_With_Dependency_Injection_And_Factory_Pattern.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Navigation_With_Dependency_Injection_And_Factory_Pattern.Services
{
    public interface INavigationService
    {
        ViewModelBase? CurrentViewModel { get; }
        void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
    }
}
