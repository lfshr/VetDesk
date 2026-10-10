namespace VetDesk.UI.Services;

/// <summary>The breadcrumb in the header bar ("Patients › Biscuit"), set by each screen.</summary>
public class Crumbs
{
    public string Text { get; private set; } = "";
    public event Action? Changed;

    public void Set(string text)
    {
        if (text == Text)
            return;
        Text = text;
        Changed?.Invoke();
    }
}
