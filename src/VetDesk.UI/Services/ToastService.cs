namespace VetDesk.UI.Services;

public enum ToastKind { Success, Warning, Error }

/// <summary>Short confirmations in the corner ("Biscuit checked in"). Errors stay until dismissed.</summary>
public class ToastService
{
    public record Toast(Guid Id, string Text, ToastKind Kind);

    private readonly List<Toast> _toasts = new();

    public IReadOnlyList<Toast> Toasts => _toasts;
    public event Action? Changed;

    public void Success(string text) => Show(text, ToastKind.Success);
    public void Warning(string text) => Show(text, ToastKind.Warning);
    public void Error(string text) => Show(text, ToastKind.Error);

    /// <summary>Shows a failed result's message; returns true when the result succeeded.</summary>
    public bool Check(SimpleResult.Result result, string? success = null)
    {
        if (result.IsFailure)
        {
            Error(result.ToString());
            return false;
        }

        if (success is not null)
            Success(success);
        return true;
    }

    private void Show(string text, ToastKind kind)
    {
        var toast = new Toast(Guid.NewGuid(), text, kind);
        _toasts.Add(toast);
        if (_toasts.Count > 4)
            _toasts.RemoveAt(0);
        Changed?.Invoke();

        if (kind != ToastKind.Error)
            _ = DismissLaterAsync(toast.Id);
    }

    private async Task DismissLaterAsync(Guid id)
    {
        await Task.Delay(TimeSpan.FromSeconds(4));
        Dismiss(id);
    }

    public void Dismiss(Guid id)
    {
        if (_toasts.RemoveAll(t => t.Id == id) > 0)
            Changed?.Invoke();
    }
}
