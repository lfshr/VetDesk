---
category: Feedback
---
Short confirmations stacked in the bottom corner ("Biscuit checked in"). Success shows a tick; Warning (amber) and Error (red) show a warning icon. In the app errors stay until dismissed and the others leave after four seconds; at most four show. Mirrors `Shared/ToastHost.razor`. Write the text as what just happened, past tense, with the patient's name.

```jsx
<Toasts toasts={[{ id: "1", text: "Biscuit checked in" }, { id: "2", text: "Low stock: Meloxicam 1.5 mg/ml", kind: "Warning" }]} onDismiss={dismiss} />
```
