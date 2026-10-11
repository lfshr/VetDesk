---
category: Status
---
Payment status pill on the Billing screen and on invoices. Mirrors `Shared/InvoiceStatusChip.razor`. Paid is green, Unpaid and Part paid amber, Overdue red, Claim sent (insurance) blue, Refunded grey.

```jsx
<td><InvoiceStatusChip status="PartPaid" /></td>
```
