---
category: Clinical
---
A small teal line chart of a patient's weights over time (oldest on the left, latest dotted) with the five most recent listed under it. Fills its container's width; the app puts it in a side card on the patient record. Mirrors `Shared/WeightChart.razor`.

```jsx
<section className="vd-card vd-card-body">
  <h2 className="vd-section-title">Weight</h2>
  <WeightChart weights={[
    { recordedUtc: "2026-10-08T09:20:00Z", kg: 12.4, context: "Consultation" },
    { recordedUtc: "2026-07-14T14:05:00Z", kg: 12.85, context: "Nurse clinic" },
  ]} />
</section>
```
