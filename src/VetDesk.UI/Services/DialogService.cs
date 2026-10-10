namespace VetDesk.UI.Services;

/// <summary>
/// In-app replacement for message boxes. <see cref="Shared.ConfirmHost"/> in the layout renders the current
/// question; pages await the answer.
/// </summary>
public class DialogService
{
    public record Request(string Title, string? Message, string ConfirmLabel, string? CancelLabel, bool IsDanger, TaskCompletionSource<bool> Result);

    public Request? Current { get; private set; }
    public event Action? Changed;

    public Task<bool> ConfirmAsync(string title, string? message = null, string confirmLabel = "OK", string cancelLabel = "Cancel", bool isDanger = false) =>
        Show(new Request(title, message, confirmLabel, cancelLabel, isDanger, new TaskCompletionSource<bool>()));

    public Task AlertAsync(string title, string? message = null) =>
        Show(new Request(title, message, "OK", null, false, new TaskCompletionSource<bool>()));

    private Task<bool> Show(Request request)
    {
        // A question replaced before it was answered counts as cancelled.
        Current?.Result.TrySetResult(false);
        Current = request;
        Changed?.Invoke();
        return request.Result.Task;
    }

    public void Close(bool confirmed)
    {
        var current = Current;
        Current = null;
        Changed?.Invoke();
        current?.Result.TrySetResult(confirmed);
    }
}
