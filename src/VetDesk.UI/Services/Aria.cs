namespace VetDesk.UI.Services;

/// <summary>
/// ARIA state attributes need the strings "true" and "false". Blazor renders a bound bool as an empty attribute
/// (true) or drops it (false), which neither screen readers nor the stylesheet's [aria-pressed=true] selectors read.
/// </summary>
public static class Aria
{
    public static string Bool(bool value) => value ? "true" : "false";
}
