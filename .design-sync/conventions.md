# VetDesk conventions (read first)

VetDesk is a desktop app for one UK veterinary practice: front desk, consult room and dispensary. Screens are dense, keyboard-friendly and calm. Use British English and GBP, with dates like "12 Oct 2026".

## Setup

- No provider or theme wrapper is needed. Linking `styles.css` loads the design tokens, every `vd-*` class and the IBM Plex fonts. `html, body` already get Plex Sans at 14px on the canvas colour.
- Components are on `window.VetDesk` (for example `const { Modal, AlertChip, Icon } = window.VetDesk;`). Each one's props are in `components/<group>/<Name>/<Name>.d.ts`.
- The components hold no data. Search components filter the arrays you pass them, and forms call your callbacks.

## Build screens from the vd-* classes

Anything without a component is plain HTML with the app's own classes. Don't invent new colours, radii or fonts.

| Need | Classes |
|---|---|
| Page | `vd-page`, `vd-page-head`, `vd-page-title`, `vd-page-sub`, `vd-toolbar`, `vd-spacer` |
| Layout | `vd-grid-main` (content + 300px side), `vd-grid-2`, `vd-kpis`, `vd-stack`, `vd-stack-sm`, `vd-row`, `vd-row-between` |
| Cards | `vd-card`, `vd-card-head`, `vd-section-title`, `vd-card-body`, `vd-card-foot` |
| Buttons | `vd-btn` + `vd-btn-primary` / `vd-btn-secondary` / `vd-btn-ghost` / `vd-btn-danger`, `vd-btn-sm`, `vd-btn-link`, `vd-icon-btn` |
| Forms | `vd-form-grid` (2 cols), `vd-form-grid-3`, `vd-field` (label + input inside), `vd-field-wide`, `vd-input`, `vd-hint` |
| Tables | `vd-table` (+ `vd-table-compact`), `vd-cell-primary`, `vd-cell-sub`, `vd-clickable` rows, `vd-num` |
| Status | `vd-chip` + `vd-chip-success` / `-warning` / `-danger` / `-info` / `-surgery` / `-outline`; `vd-banner` + `vd-banner-danger` / `-warning` / `-info` / `-success`; `vd-cd` (controlled drug tag) |
| Tabs and toggles | `vd-tabs` + `vd-tab[aria-selected=true]`, `vd-segmented` buttons with `aria-pressed`, `vd-pills` + `vd-pill` |
| Text | `vd-muted`, `vd-faint`, `vd-small`, `vd-strong`, `vd-mono`, `vd-nowrap`, `vd-up`, `vd-down` |

Prefer the status components (`AlertChip`, `AppointmentStatusChip`, `InvoiceStatusChip`, `StockStatusChip`, `VaccineDueChip`) over raw chips, because they carry the right colour for each state.

## Tokens

When a class doesn't fit, use the CSS variables: `var(--vd-accent)` (teal brand), `--vd-text`, `--vd-text-secondary`, `--vd-text-faint`, `--vd-surface`, `--vd-surface-muted`, `--vd-canvas`, `--vd-border`, `--vd-border-strong`, `--vd-radius`, `--vd-radius-sm`, `--vd-shadow`, and the paired `--vd-<success|warning|danger|info>-bg` / `-text`. Money and IDs use `--vd-font-mono`.

## Safety cues matter

Allergies are always visible (`AlertChip isAllergy`, red). Controlled drugs carry the `vd-cd` tag and need a `WitnessPicker`. Completed consultations show a lock and are read-only.

## Example

```jsx
const { AlertChip, Icon } = window.VetDesk;
<div className="vd-page">
  <div className="vd-page-head"><div><h1 className="vd-page-title">Bramble</h1>
    <div className="vd-page-sub">Dog · Border Collie · Tom Bishop</div></div>
    <button className="vd-btn vd-btn-primary"><Icon name="plus" size={16} /> New consultation</button></div>
  <section className="vd-card">
    <div className="vd-card-head"><h2 className="vd-section-title">Alerts</h2></div>
    <div className="vd-card-body vd-row"><AlertChip text="Penicillin" severity="Alert" isAllergy /></div>
  </section>
</div>
```
