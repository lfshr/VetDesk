---
category: Icons
---
Line icon from the VetDesk design, drawn on a 24px grid and stroked in the current text colour: colour it by setting `color` on a parent. Mirrors `Shared/Icon.razor` (generated from it, so the names are the app's full set).

Sizes the app uses: 20 (default, sidebar), 18 (buttons, search boxes, banners), 16 (small buttons, list rows), 13–14 (inside chips).

```jsx
<button className="vd-btn vd-btn-primary"><Icon name="plus" size={18} /> New appointment</button>
<span style={{ color: "var(--vd-danger-text)" }}><Icon name="alert" size={16} /></span>
```
