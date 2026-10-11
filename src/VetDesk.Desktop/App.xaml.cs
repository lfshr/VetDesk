using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VetDesk.Application;
using VetDesk.Infrastructure;
using VetDesk.Infrastructure.Persistence;
using VetDesk.Infrastructure.Services;
using VetDesk.UI.Services;

namespace VetDesk.Desktop;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _services;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
#if DEBUG
                .AddJsonFile("appsettings.Debug.json", optional: true)
#endif
                // Lets VetDesk.AppHost (Aspire) hand over ConnectionStrings__VetDesk for its SQL Server container.
                .AddEnvironmentVariables()
                .Build();

            var services = new ServiceCollection();
            services.AddLogging(logging => logging
                .AddDebug()
                .AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning));
            services.AddWpfBlazorWebView();
#if DEBUG
            // F12 opens the browser developer tools inside the window.
            services.AddBlazorWebViewDeveloperTools();
#endif
            services.AddApplicationServices();
            services.AddInfrastructureServices(configuration);
            services.AddVetDeskUi();
            _services = services.BuildServiceProvider();

            // Same start-up as VetDesk.Web: bring the database up to date, then start the nightly backup timer.
            await using (var scope = _services.CreateAsyncScope())
                await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitialiseAsync();
            _services.GetRequiredService<BackupScheduler>().Start();

            var window = new MainWindow(_services);
            MainWindow = window;
            window.Show();
        }
        catch (Exception ex)
        {
            // Usually the database can't be reached. Say so rather than closing silently.
            MessageBox.Show(
                $"VetDesk couldn't start.\n\n{ex.Message}\n\nCheck ConnectionStrings:VetDesk in appsettings.json next to VetDesk.Desktop.exe.",
                "VetDesk", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_services is not null)
        {
            await _services.DisposeAsync();
        }

        base.OnExit(e);
    }
}
