---
category: Clinical
---
The teal S / O / A / P badge beside each section of a consultation note. Mirrors `Shared/SoapLetter.razor`.

```jsx
<label htmlFor="c-subj" className="vd-row" style={{ gap: 10, marginBottom: 8 }}>
  <SoapLetter letter="S" /><span className="vd-section-title" style={{ margin: 0 }}>Subjective</span>
  <span className="vd-small vd-muted">History from owner</span>
</label>
<textarea id="c-subj" className="vd-input" rows={3} />
```
