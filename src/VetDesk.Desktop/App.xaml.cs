using System.Windows;
using Microsoft.Extensions.Configuration;

namespace VetDesk.Desktop;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
#if DEBUG
            .AddJsonFile("appsettings.Debug.json", optional: true)
#endif
            // Lets VetDesk.AppHost (Aspire) hand over VetDesk__ServerUrl, the address of its VetDesk.Web.
            .AddEnvironmentVariables()
            .Build();

        var setting = configuration["VetDesk:ServerUrl"];
        var server = ServerAddress.Parse(setting);
        if (server is null)
        {
            MessageBox.Show(
                $"VetDesk doesn't know which server to open.\n\nVetDesk:ServerUrl is \"{setting}\". Set it in appsettings.json next to VetDesk.Desktop.exe to the server's address, for example https://vetdesk.local.",
                "VetDesk", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        var window = new MainWindow(server);
        MainWindow = window;
        window.Show();
    }
}
