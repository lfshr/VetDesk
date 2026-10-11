---
category: Search
---
Picks the patient inside a dialog, e.g. New appointment or New invoice: a search box, then recent patients (before typing) or matches, and a "New client and patient" form for someone new. Once chosen it collapses to a card with the patient's allergies and a Change button (no button when `locked`). Mirrors `Shared/PatientPicker.razor`.

```jsx
<PatientPicker patients={patients} recent={recent} selected={patient} onSelectedChange={setPatient} />
```
