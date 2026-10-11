---
category: Dialogs
---
The dialog every VetDesk form opens in: dimmed backdrop, title and subtitle, close button, body, right-aligned footer. Escape closes it; clicking the backdrop deliberately doesn't (a half-filled form shouldn't vanish). Mirrors `Shared/Modal.razor`. The overlay is `position: fixed`, so render it at the top of the screen, not inside a card.

Sizes: `narrow` (confirmations, cash-up), default (most forms), `wide` and `xwide` (registers, invoices). `flush` removes body padding for an edge-to-edge table. Footer: secondary (Cancel) first, primary last.

```jsx
<Modal title="New appointment" subtitle="Biscuit · Sarah Whitfield" onClose={close}
  footer={<><button className="vd-btn vd-btn-secondary">Cancel</button><button className="vd-btn vd-btn-primary">Book</button></>}>
  <div className="vd-form-grid">
    <div className="vd-field"><label htmlFor="a-date">Date</label><input id="a-date" type="date" /></div>
    <div className="vd-field"><label htmlFor="a-time">Time</label><input id="a-time" /></div>
  </div>
</Modal>
```
