---
category: Search
---
Searches the price list (by name or code) when charging or prescribing on a consultation, dispensing, or ordering stock. Each match shows code, category, stock, price, a CD tag for controlled drugs and a red chip when it conflicts with the current patient's allergies. Picking clears the box; follow it with a `ChargeForm`. Mirrors `Shared/ProductSearch.razor`.

```jsx
<ProductSearch products={priceList} categories={["Medicine"]} placeholder="Prescribe a medicine" onPick={setPending} />
```
