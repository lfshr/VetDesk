---
category: Feedback
---
The red banner under a form when a request is refused (a double booking, a missing field the server rejected). Renders nothing without `text`. Mirrors `Shared/FormError.razor`. For field-level messages use `<div className="vd-error-text">`.

```jsx
<FormError text="That time is already booked for Heather." />
```
