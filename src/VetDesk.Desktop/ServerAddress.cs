namespace VetDesk.Desktop;

/// <summary>
/// The VetDesk server this window shows, from <c>VetDesk:ServerUrl</c>. The window only navigates within it; any other
/// link opens in the PC's normal browser.
/// </summary>
public sealed class ServerAddress
{
    private ServerAddress(Uri uri) => Uri = uri;

    public Uri Uri { get; }

    /// <summary>Null when the setting is missing or isn't an absolute http(s) address.</summary>
    public static ServerAddress? Parse(string? setting)
    {
        if (string.IsNullOrWhiteSpace(setting)
            || !Uri.TryCreate(setting.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            return null;
        return new ServerAddress(uri);
    }

    /// <summary>True for a page on this server: same scheme, host and port, whatever the path.</summary>
    public bool Contains(string? address) =>
        Uri.TryCreate(address, UriKind.Absolute, out var uri)
        && Uri.Compare(uri, Uri, UriComponents.SchemeAndServer, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0;

    /// <summary>True for a web address (http or https), as opposed to the window's own about: or data: pages.</summary>
    public static bool IsWebAddress(string? address) =>
        Uri.TryCreate(address, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}
