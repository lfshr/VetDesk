---
category: Clinical
---
One line of a pre-flight checklist: green tick when `ok`, otherwise a red warning with bold text. Used in "Complete consultation and invoice". Mirrors `Shared/Check.razor`. It renders an `<li>`, so wrap the lines in `<ul className="vd-list">`.

```jsx
<div className="vd-label">Checks</div>
<ul className="vd-list">
  <Check ok text="Clinical note has all four sections filled in" />
  <Check ok={false} text="Amoxicillin conflicts with the penicillin allergy" />
</ul>
```
