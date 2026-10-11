## Markup guide

Each pattern below is the markup the VetDesk app renders. Copy the structure and change the content. `[icon]` marks an
inline SVG icon (see Setup).

### App shell

```html
<div class="vd-app">
  <header class="vd-header">
    <div class="vd-header-brand">[icon] <strong>VetDesk</strong> <span class="vd-crumbs">Patients › Biscuit</span></div>
    <div class="vd-row"><button class="vd-header-btn">[icon] Lock</button><button class="vd-header-btn">Sign out</button></div>
  </header>
  <div class="vd-body">
    <nav class="vd-sidebar" aria-label="Main">
      <a href="#" class="active">[icon] Today</a>
      <a href="#">[icon] Schedule</a>
      <a href="#">[icon] Patients</a>
      <a href="#">[icon] Clients</a>
      <a href="#">[icon] Pharmacy <span class="vd-sidebar-badge">3</span></a>
      <a href="#">[icon] Billing</a>
      <a href="#">[icon] Reports</a>
      <span class="vd-spacer"></span>
      <a href="#">[icon] Settings</a>
      <div class="vd-usercard"><span class="vd-avatar vd-avatar-dark">D</span>
        <div><div class="vd-usercard-name">Douglas</div><div class="vd-usercard-role">Veterinary surgeon</div></div></div>
    </nav>
    <main class="vd-main"><div class="vd-page"><!-- page content --></div></main>
  </div>
</div>
```

The selected sidebar link has `class="active"`.

### Page header, filters, tabs

```html
<div class="vd-page-head">
  <div><h1 class="vd-page-title">Billing</h1><div class="vd-page-sub">Invoices, payments and insurance claims</div></div>
  <div class="vd-pills" role="group" aria-label="Filter invoices">
    <button class="vd-pill" aria-pressed="true">All <span class="vd-pill-count">10</span></button>
    <button class="vd-pill" aria-pressed="false">Unpaid <span class="vd-pill-count">4</span></button>
  </div>
</div>
<div class="vd-tabs" role="tablist">
  <button class="vd-tab" role="tab" aria-selected="true">Overview</button>
  <button class="vd-tab" role="tab" aria-selected="false">History</button>
</div>
```

For a toggle between two or three choices, use `<div class="vd-segmented">` with `<button aria-pressed="true|false">`.

### Figures

```html
<div class="vd-kpis">
  <div class="vd-card vd-kpi"><div class="vd-kpi-label">Appointments</div><div class="vd-kpi-value">12</div><div class="vd-kpi-change vd-up">+8% vs previous</div></div>
</div>
```

`vd-kpis` is a four-column grid. Use `vd-up` or `vd-down` on the change line.

### Two-column page

`<div class="vd-grid-main">` holds the main column plus a 300px side column (`vd-grid-main-wide`: 380px). Use
`vd-grid-2` for two equal columns and `vd-stack` for vertical 16px spacing.

### Cards and tables

```html
<section class="vd-card">
  <div class="vd-card-head"><h2 class="vd-section-title">Invoices</h2><span class="vd-small vd-muted">Outstanding £322.68</span></div>
  <table class="vd-table">
    <thead><tr><th>Invoice</th><th>Owner · patient</th><th class="vd-num">Due</th><th>Status</th></tr></thead>
    <tbody>
      <tr>
        <td class="vd-mono">INV-01049</td>
        <td><div class="vd-cell-primary">Dan Moore</div><div class="vd-cell-sub">Bramble</div></td>
        <td class="vd-num vd-mono">£41.40</td>
        <td><span class="vd-chip vd-chip-warning">Part paid</span></td>
      </tr>
    </tbody>
  </table>
  <div class="vd-card-foot vd-small vd-muted">Showing 10 of 24</div>
</section>
```

Use `<div class="vd-card-body">` for padded content. When nothing matches, use `<div class="vd-empty">No invoices match.</div>`.

### Buttons

```html
<button class="vd-btn vd-btn-primary">Complete and invoice</button>
<button class="vd-btn vd-btn-secondary">Save draft</button>
<button class="vd-btn vd-btn-ghost vd-btn-sm">[icon] Edit</button>
<button class="vd-btn vd-btn-danger">Delete</button>
<button class="vd-btn-link">Forgot your PIN?</button>
<button class="vd-icon-btn" aria-label="Close">[icon]</button>
```

Each screen has one primary action. A disabled primary button turns solid grey.

### Chips and patient alerts

```html
<span class="vd-chip vd-chip-success">Paid</span>
<span class="vd-chip vd-chip-info">In consult</span>
<span class="vd-chip vd-chip-surgery">Surgery</span>
<span class="vd-chip vd-chip-outline">Booked</span>
<!-- Patient alerts under the patient's name: larger chips; triangle icon = alert, circled ! = warning, circled i = note -->
<span class="vd-chip vd-chip-danger vd-chip-lg">[icon] Allergy: amoxicillin</span>
<span class="vd-chip vd-chip-warning vd-chip-lg">[icon] Nervous — muzzle for nail clips</span>
<span class="vd-chip vd-chip-success vd-chip-lg">[icon] Insured</span>
<button class="vd-chip-add" aria-label="Add alert">[icon]</button>
```

A controlled drug is marked `<span class="vd-cd">CD</span>` after its name.

### Forms

```html
<div class="vd-form-grid">
  <div class="vd-field"><label for="f1">First name</label><input id="f1" placeholder="e.g. Sam"></div>
  <div class="vd-field"><label for="f2">Mobile</label><input id="f2" placeholder="07…"></div>
  <div class="vd-field vd-field-wide"><label for="f3">Reason</label><textarea id="f3"></textarea><div class="vd-hint">Shown on the schedule.</div></div>
</div>
<label class="vd-check"><input type="checkbox" checked> Send reminders by text and email</label>
```

Use `vd-form-grid-3` or `vd-form-grid-4` for more columns. Show an error with `<div class="vd-error-text">…</div>`.

### Dialog

```html
<div class="vd-overlay">
  <div class="vd-modal" role="dialog" aria-modal="true" aria-labelledby="d1" style="width:min(560px,100%)">
    <div class="vd-modal-head">
      <div><h2 id="d1">New client</h2><div class="vd-modal-sub">Add the owner first. You can add their pets straight after.</div></div>
      <button class="vd-icon-btn vd-modal-close" aria-label="Close">[icon]</button>
    </div>
    <div class="vd-modal-body"><!-- fields --></div>
    <div class="vd-modal-foot"><button class="vd-btn vd-btn-secondary">Cancel</button><button class="vd-btn vd-btn-primary">Add client</button></div>
  </div>
</div>
```

Dialogs are 520–860px wide, set with the inline width. The footer is grey and holds right-aligned buttons, the
main action last. To confirm a deletion, use `vd-modal vd-modal-narrow` with `vd-confirm` (a round `vd-confirm-icon`
holding a bin icon, then an `h2` question) and a `vd-btn-danger` button.

### Banners

```html
<div class="vd-banner vd-banner-info">[icon] This consultation is completed and locked.</div>
<div class="vd-banner vd-banner-warning">[icon] That number belongs to Hannah Clarke (CL-00001).</div>
```

`vd-banner-success` and `vd-banner-danger` work the same way. `vd-banner-compact` is the smaller inline version.

### Dropdown and search results

Place it inside a `position:relative` wrapper:
`<div class="vd-dropdown" role="listbox">` with `<button class="vd-dropdown-item" role="option">` rows (an
`vd-avatar`, then name and `vd-cell-sub` detail, then a `vd-mono vd-small vd-muted` ID), ending with
`<button class="vd-dropdown-foot">Search all patients [icon]</button>`. A short menu adds `vd-menu`.

### Sign-in

```html
<div class="vd-signin"><div class="vd-signin-card vd-signin-card-wide">
  <div class="vd-signin-brand">[icon] VetDesk</div>
  <h1>Who's at the desk?</h1>
  <div class="vd-tiles">
    <button class="vd-tile"><span class="vd-avatar vd-avatar-lg">D</span><span class="vd-tile-name">Douglas</span></button>
  </div>
</div></div>
```

The PIN step uses `vd-pinpad` > `input.vd-input.vd-pin-input` + `vd-pin-keys` > `button.vd-pin-key` (digits) and
`button.vd-pin-key.vd-pin-key-small` (Clear, Delete).
