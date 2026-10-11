---
category: Forms
---
Appears under the price-list search on a consultation once a product is picked: quantity, and for a prescription the label directions. If the product conflicts with the patient's allergies it shows a red banner and only a vet (`canOverride`) can add it, with a written reason ("Override and add"). Mirrors `Shared/ChargeForm.razor`.

```jsx
<ChargeForm product={{ name: "Meloxicam 1.5 mg/ml oral suspension", grossPrice: 14.4, unit: "bottle", isStocked: true, onHand: 23 }} isPrescription onCancel={close} onAdd={add} />
<ChargeForm product={{ name: "Amoxicillin 250 mg tablets", grossPrice: 0.84, unit: "tablet" }} conflicts={["Penicillin"]} />
```
