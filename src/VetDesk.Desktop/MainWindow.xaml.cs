using System.Windows;

namespace VetDesk.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(IServiceProvider services)
    {
        InitializeComponent();
        WebView.Services = services;
    }
}
