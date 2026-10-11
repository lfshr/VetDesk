---
category: Dialogs
---
The in-app confirm or alert box: VetDesk never uses browser message boxes. Focuses the main button; Escape cancels. `danger` gives a red button and a bin icon (deletes, voids); otherwise a teal info icon. `cancelLabel={null}` makes a single-button alert. Mirrors `Shared/ConfirmHost.razor`. Phrase the title as the question ("Are you sure you want to delete “Bites when restrained”?").

```jsx
<ConfirmDialog title="Are you sure you want to void invoice INV-2041?" message="The charges go back on the consultation." confirmLabel="Void invoice" danger onConfirm={voidIt} onCancel={close} />
```
