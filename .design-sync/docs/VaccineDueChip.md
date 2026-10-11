---
category: Status
---
Booster due chip on a patient's vaccination list. Mirrors `Shared/VaccineDueChip.razor`. Green "Due 14 Mar 2027" normally, blue when `isDueSoon`, amber "Overdue since …" when `isOverdue`, plain "No booster" without a date. Dates are ISO strings and show as "14 Mar 2027".

```jsx
<td><VaccineDueChip dueDate="2026-11-02" isDueSoon /></td>
```
