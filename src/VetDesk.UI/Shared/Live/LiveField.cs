using Microsoft.AspNetCore.Components;

namespace VetDesk.UI.Shared.Live;

/// <summary>
/// A box in a <see cref="LiveEditForm"/>, named by <see cref="Field"/>: shows the form's value, types into it, and is
/// read-only while someone at another desk is in it. Any other attributes (id, maxlength…) go on the element.
/// </summary>
public abstract class LiveField : ComponentBase
{
    [CascadingParameter] protected LiveEditForm Form { get; set; } = default!;

    [Parameter, EditorRequired] public string Field { get; set; } = "";

    [Parameter] public bool Disabled { get; set; }

    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? Attributes { get; set; }

    protected string? Value => Form[Field];

    protected string? TypingBy => Form.TypingBy(Field);

    protected bool ReadOnly => Disabled || Form.IsReadOnly(Field);

    protected Task Type(ChangeEventArgs e) => Form.Type(Field, e.Value?.ToString());

    protected void Enter() => Form.Enter(Field);

    protected void Leave() => Form.Leave(Field);

    protected override void OnParametersSet()
    {
        if (Form is null)
            throw new InvalidOperationException($"{GetType().Name} must be inside a {nameof(LiveEditForm)}.");
    }
}
