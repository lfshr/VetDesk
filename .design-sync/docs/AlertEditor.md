---
category: Forms
---
Edits one patient alert inline on the patient record: pick from the practice's preset list or write your own, then choose Alert / Warning / Note. Editing a saved alert adds Delete, which asks for confirmation first. Mirrors `Shared/AlertEditor.razor`.

```jsx
<AlertEditor alert={{ text: "", severity: "Alert" }} onClose={close} onSave={save} />
<AlertEditor alert={{ id: 7, presetKey: "1", text: "Bites when restrained", severity: "Alert" }} onClose={close} onSave={save} onDelete={remove} />
```
