---
category: Status
---
Status pill for an appointment in the Today and Schedule tables. Mirrors `Shared/AppointmentStatusChip.razor`.

| status | consultation | surgery | look |
|---|---|---|---|
| Booked | Booked | Surgery | outlined (surgery: purple) |
| CheckedIn | Waiting | Admitted | amber |
| InConsult | In consult | In theatre | blue |
| Done | Done | Done | grey |
| Cancelled / NoShow | Cancelled / No-show | same | red |

```jsx
<td><AppointmentStatusChip status="CheckedIn" /></td>
<td><AppointmentStatusChip status="Booked" kind="Surgery" /></td>
```
