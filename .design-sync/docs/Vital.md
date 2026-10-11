---
category: Forms
---
One vital sign box on the consultation note: label, monospaced value, unit inside the right edge. Mirrors `Shared/Vital.razor`. The app shows six in a row; `disabled` once the consultation is completed (locked). `text` for free-text signs (mucous membranes, CRT).

```jsx
<div className="vd-form-grid-3" style={{ gridTemplateColumns: "repeat(6, minmax(0, 1fr))" }}>
  <Vital label="Temperature" unit="°C" id="v-temp" value="38.6" />
  <Vital label="Heart rate" unit="bpm" id="v-hr" value="96" />
  <Vital label="Mucous membr." id="v-mm" value="Pink, moist" text />
</div>
```
