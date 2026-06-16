using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Navigation_With_Dependency_Injection_And_Factory_Pattern.Services;
using Navigation_With_Dependency_Injection_And_Factory_Pattern.ViewModels;
using Navigation_With_Dependency_Injection_And_Factory_Pattern.Views;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Navigation_With_Dependency_Injection_And_Factory_Pattern;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static IHost? AppHost { get; private set; }

    public App()
    {
        try
        {
            AppHost = Host.CreateDefaultBuilder().ConfigureServices((context, services) =>
            {
                services.AddSingleton<MainWindowView>();
                services.AddSingleton<AutoView>();
                services.AddSingleton<TeachingView>();
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<AutoViewModel>();
                services.AddSingleton<TeachingViewModel>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<Func<Type, ViewModelBase>>(serviceProvider => viewModelType => (ViewModelBase)serviceProvider.GetRequiredService(viewModelType));
            }).Build();
        }

        catch (Exception ex)
        {
            throw;
        }
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        if (AppHost == null)
        {
            MessageBox.Show("Applicstion Start Failed");
            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Shutdown();
            return;
        }
        await AppHost!.StartAsync();
        Window window = AppHost.Services.GetRequiredService<MainWindowView>();
        window.DataContext = AppHost.Services.GetRequiredService<MainWindowViewModel>();
        window.Show();
        base.OnStartup(e);
    }
}