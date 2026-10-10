using Microsoft.JSInterop;

namespace VetDesk.UI.Services;

/// <summary>
/// Keyboard shortcuts for speed at the desk. The browser reports key presses here; the screen on view
/// subscribes to the ones it handles. Plain letters are not reported while typing in a box.
/// </summary>
public sealed class Hotkeys : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private DotNetObjectReference<Hotkeys>? _ref;

    public Hotkeys(IJSRuntime js)
    {
        _js = js;
    }

    public event Func<string, Task>? Pressed;

    public async Task StartAsync()
    {
        if (_ref is not null)
            return;
        _ref = DotNetObjectReference.Create(this);
        await _js.InvokeVoidAsync("vetdesk.registerHotkeys", _ref);
    }

    [JSInvokable]
    public async Task OnHotkey(string combo)
    {
        if (Pressed is null)
            return;
        foreach (var handler in Pressed.GetInvocationList().Cast<Func<string, Task>>())
            await handler(combo);
    }

    public ValueTask DisposeAsync()
    {
        _ref?.Dispose();
        return ValueTask.CompletedTask;
    }
}
