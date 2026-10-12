using Microsoft.AspNetCore.Components;

namespace VetDesk.UI.Shared.Live;

/// <summary>
/// A box in a <see cref="LiveEditForm"/>, named by <see cref="Field"/>: shows the form's value, types into it, and is
/// read-only while someone at another desk is typing in it. Any other attributes (id, maxlength…) go on the element.
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

    /// <summary>The element's class, plus "vd-held" while someone else is typing in it so it looks locked, not just greyed.</summary>
    protected string? Class => Held(Attributes?.GetValueOrDefault("class") as string);

    protected string? Held(string? css) => TypingBy is null ? css : $"{css} vd-held".TrimStart();

    protected Task Type(ChangeEventArgs e) => Form.Type(Field, e.Value?.ToString());

    protected void Leave() => Form.Leave(Field);

    protected override void OnParametersSet()
    {
        if (Form is null)
            throw new InvalidOperationException($"{GetType().Name} must be inside a {nameof(LiveEditForm)}.");
    }
}
