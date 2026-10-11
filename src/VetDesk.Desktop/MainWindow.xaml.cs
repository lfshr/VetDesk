using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace VetDesk.Desktop;

/// <summary>
/// Shows the VetDesk server's pages. Everything (screens, data, sign-in) lives on the server; this window only adds
/// the desktop app's own frame, keeps the browser from saving PINs, and explains when the server can't be reached.
/// </summary>
public partial class MainWindow : Window
{
    private readonly ServerAddress _server;

    public MainWindow(ServerAddress server)
    {
        _server = server;
        InitializeComponent();
        Loaded += async (_, _) => await StartAsync();
    }

    private async Task StartAsync()
    {
        try
        {
            // The browser profile (cookies, cached scripts and styles) goes in the user's own folder: WebView2's
            // default, next to the .exe, isn't writable when the app is installed under Program Files.
            var profileFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VetDesk", "WebView2");
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: profileFolder);
            await WebView.EnsureCoreWebView2Async(environment);
        }
        catch (WebView2RuntimeNotFoundException)
        {
            MessageBox.Show(
                "VetDesk needs the Microsoft Edge WebView2 Runtime, which isn't installed on this PC.\n\nInstall it from https://go.microsoft.com/fwlink/p/?LinkId=2124703, then start VetDesk again.",
                "VetDesk", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
            return;
        }

        var core = WebView.CoreWebView2;
        // PINs and the administrator password must never be offered for saving, or filled in by the browser.
        core.Settings.IsPasswordAutosaveEnabled = false;
        core.Settings.IsGeneralAutofillEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
#if !DEBUG
        core.Settings.AreDevToolsEnabled = false;
#endif
        core.NavigationStarting += OnNavigationStarting;
        core.NewWindowRequested += OnNewWindowRequested;
        core.NavigationCompleted += OnNavigationCompleted;
        core.WebMessageReceived += OnWebMessageReceived;

        core.Navigate(_server.Uri.AbsoluteUri);
    }

    // The window stays on the VetDesk server. Any other web address opens in the PC's normal browser instead.
    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (ServerAddress.IsWebAddress(e.Uri) && !_server.Contains(e.Uri))
        {
            e.Cancel = true;
            OpenInBrowser(e.Uri);
        }
    }

    // No pop-up windows: they would be plain browser windows without this window's settings.
    private void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;
        if (_server.Contains(e.Uri))
            WebView.CoreWebView2.Navigate(e.Uri);
        else if (ServerAddress.IsWebAddress(e.Uri))
            OpenInBrowser(e.Uri);
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        // Only when no answer came back at all (server off, wrong address, untrusted certificate). A page the server
        // did send, such as its "only on the practice network" refusal, is shown as it is.
        if (e.IsSuccess || e.HttpStatusCode != 0 || e.WebErrorStatus == CoreWebView2WebErrorStatus.OperationCanceled)
            return;

        WebView.CoreWebView2.NavigateToString(OfflinePage.Html(_server, e.WebErrorStatus));
    }

    // The offline page's Retry button.
    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        // Only the offline page (not a web page) can ask for a retry.
        if (!ServerAddress.IsWebAddress(e.Source) && e.TryGetWebMessageAsString() == OfflinePage.RetryMessage)
            WebView.CoreWebView2.Navigate(_server.Uri.AbsoluteUri);
    }

    private static void OpenInBrowser(string address)
    {
        try
        {
            Process.Start(new ProcessStartInfo(address) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Couldn't open {address} in the browser.\n\n{ex.Message}", "VetDesk",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
