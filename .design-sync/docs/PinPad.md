---
category: Forms
---
Four-digit PIN entry for the sign-in and lock screens: type it or tap the keypad (touch screens at the desk). Calls `onComplete` on the fourth digit and clears. `busy` disables it while the PIN is checked. Mirrors `Shared/PinPad.razor`. Staff sign in by tapping their name tile, then the PIN.

```jsx
<div className="vd-card vd-card-body" style={{ width: 300 }}>
  <h2 className="vd-section-title">Janet</h2>
  <PinPad label="Janet's PIN" onComplete={signIn} />
</div>
```
