using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Setting_Parameters
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            txtPassword.Password = "12345678";
        }

        private void txtSpeed_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtSpeed == null) return;

            if (double.TryParse(txtSpeed.Text, out double speed))
            {
                if (speed < 0 || speed > 100)
                {
                    txtSpeed.BorderBrush = Brushes.Red;
                    txtSpeed.BorderThickness = new Thickness(1.5);
                }
                else
                {
                    txtSpeed.BorderBrush = Brushes.Black;
                    txtSpeed.BorderThickness = new Thickness(1);
                }    
            }
            else
            {
                txtSpeed.BorderBrush = Brushes.Red;
                txtSpeed.BorderThickness = new Thickness(1.5);
            }    
        }
    }
}