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

namespace Machine_Control_Button_Interface;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private int startCount = 0;
    private int jogPosition = 0;
    
    public MainWindow()
    {
        InitializeComponent();
    }

    private void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        startCount++;
        StartCountText.Text = startCount.ToString();
    }

    private void BtnJog_OnClick(object sender, RoutedEventArgs e)
    {
        jogPosition++;
        JogCountText.Text = jogPosition.ToString();
    }
    
    private void Light_On(object sender, RoutedEventArgs e)
    {
        LightToggle.Content = "Light : On";
        LightStateText.Text = "Mở";
        LightToggle.Background = Brushes.Red;
    }

    private void Light_Off(object sender, RoutedEventArgs e)
    {
        LightToggle.Content = "Light : Off";
        LightStateText.Text = "Tắt";
        LightToggle.Background = Brushes.Yellow;
    }
}