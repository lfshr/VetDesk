---
category: Reports
---
Headline figure card on the Reports screen: label, big value, and the change against the previous period (green when up, red when down). Mirrors `Shared/Kpi.razor`. Pass `value` already formatted (`£4,210.00`, `38`). Lay several out in a `vd-kpis` row.

```jsx
<div className="vd-kpis">
  <Kpi label="Revenue (net)" value="£18,240.50" change={6.2} />
  <Kpi label="Consultations" value="212" change={-3.1} />
  <Kpi label="New clients" value="14" />
</div>
```
