---
category: Search
---
The quick patient search at the top of the Today screen (Ctrl K): type two letters of a pet's or owner's name, a phone number or a microchip, and the top five matches drop down with avatar, species, owner, phone and patient number. Arrow keys and Enter pick; "Search all patients" goes to the full list. Mirrors `Shared/PatientSearch.razor`; the app searches the database, the mirror searches the `patients` array. `defaultQuery` shows it mid-search in a static mockup.

```jsx
<PatientSearch patients={patients} defaultQuery="bi" onOpen={p => openRecord(p.id)} />
```
