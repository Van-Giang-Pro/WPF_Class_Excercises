using Navigation_Basic.Views;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Navigation_Basic
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            MainWindowView mainWindowView = new MainWindowView();
            mainWindowView.Show();
        }
    }

}
