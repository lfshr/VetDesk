---
category: Forms
---
A second member of staff confirms a controlled drug entry with their own PIN: a witness select and a PIN box side by side, with a hint underneath. Used in the controlled drug register, dispensing and stock takes. Mirrors `Shared/WitnessPicker.razor`. Leave whoever is signed in out of `witnesses`.

```jsx
<WitnessPicker witnesses={[{ id: 2, shortName: "Janet", jobTitle: "Registered veterinary nurse" }]} witnessId={witness} onWitnessIdChange={setWitness} pin={pin} onPinChange={setPin} />
```
