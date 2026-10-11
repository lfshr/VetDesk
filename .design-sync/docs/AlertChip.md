---
category: Status
---
A patient alert or drug allergy chip, shown under the patient's name on the record and in lists. Mirrors `Shared/AlertChip.razor`.

| severity | colour | icon |
|---|---|---|
| Alert (or `isAllergy`) | red | warning triangle |
| Warning | amber | warning triangle |
| Note | green | info |

`large` is the bigger chip in the patient record header. Sort chips most serious first (allergies, Alert, Warning, Note).

```jsx
<div className="vd-row" style={{ gap: 6, flexWrap: "wrap" }}>
  <AlertChip text="Penicillin" isAllergy large />
  <AlertChip text="Bites when restrained" severity="Alert" large />
  <AlertChip text="Insured" severity="Note" large />
</div>
```
