# VetDesk — how to build with this design system

VetDesk is practice-management software for Parklea Vets, a single-site UK veterinary clinic. The real app is
Blazor, so this design system has **no JavaScript components**: `window.VetDesk` is empty. Build every screen from
plain HTML elements carrying VetDesk's `vd-*` classes. That markup is exactly what the app renders, so a design made
this way carries straight over to the code.

## Setup

- Import `styles.css` and nothing else. It loads the IBM Plex fonts and `tokens/vetdesk.css`, the app's real
  stylesheet (every token and every `vd-*` class).
- No wrapper or provider. The stylesheet styles `html`/`body` directly: IBM Plex Sans, 14px, text `#17201E`, page
  background `var(--vd-canvas)`.
- Icons: inline SVG, 24×24 viewBox, `fill="none" stroke="currentColor" stroke-width="1.8"`, round caps, drawn at
  16px (in buttons, chips) or 18px (in the sidebar, banners). No emoji, no icon fonts.

## Styling idiom

Compose `vd-*` classes; use `var(--vd-*)` tokens for anything the classes don't cover. Never hard-code a hex that
a token covers, and don't invent new class names.

| Need | Classes |
| --- | --- |
| Buttons | `vd-btn` + `vd-btn-primary` / `-secondary` / `-ghost` / `-danger` / `-danger-outline`, `vd-btn-sm`; `vd-btn-link`; `vd-icon-btn` |
| Status chips | `vd-chip` + `vd-chip-success` / `-warning` / `-danger` / `-info` / `-surgery` / `-outline`; `vd-chip-lg` |
| Surfaces | `vd-card` > `vd-card-head` (+ `h2.vd-section-title`) / `vd-card-body` / `vd-card-foot` |
| Forms | `vd-field` > `label` + `input`/`select`/`textarea`, `vd-hint`, `vd-error-text`; grids `vd-form-grid` (2 col), `-3`, `-4`, `vd-field-wide`; `vd-check` |
| Tables | `table.vd-table`, `vd-cell-primary` + `vd-cell-sub`, `td.vd-num`; wrap wide ones in `vd-table-scroll` |
| Page | `vd-page` > `vd-page-head` (`h1.vd-page-title`, `vd-page-sub`); `vd-pills` > `button.vd-pill[aria-pressed]`; `vd-tabs` > `button.vd-tab[aria-selected]` |
| Dialog | `vd-overlay` > `vd-modal` > `vd-modal-head` / `vd-modal-body` / `vd-modal-foot` |
| Messages | `vd-banner` + `vd-banner-info` / `-success` / `-warning` / `-danger`; `vd-empty` |
| Text | `vd-muted`, `vd-faint`, `vd-small`, `vd-strong`, `vd-mono` (times, money, IDs, quantities) |
| Layout | `vd-row`, `vd-row-between`, `vd-stack`, `vd-spacer`, `vd-grid-2`, `vd-grid-main` |

Key tokens: `--vd-accent` (teal `#0B6E63`), `--vd-sidebar` (`#0E2A2A`), `--vd-canvas`, `--vd-surface`,
`--vd-surface-muted`, `--vd-border`, `--vd-border-strong`, `--vd-text`, `--vd-text-secondary`, `--vd-text-faint`,
`--vd-{success,warning,danger,info,neutral,surgery}-{bg,text}`, `--vd-radius` (8px), `--vd-radius-sm` (6px),
`--vd-font-mono`.

## Where the truth lives

Read `tokens/vetdesk.css` before styling anything: it is the app's stylesheet verbatim. The markup guide below
shows the exact structure for each pattern.

## Example

```html
<section class="vd-card">
  <div class="vd-card-head"><h2 class="vd-section-title">Vaccinations</h2><button class="vd-btn vd-btn-secondary vd-btn-sm">Add</button></div>
  <table class="vd-table">
    <thead><tr><th>Vaccine</th><th>Given</th><th>Due</th></tr></thead>
    <tbody><tr><td class="vd-cell-primary">Lepto 4</td><td class="vd-mono">12 Mar 26</td><td><span class="vd-chip vd-chip-warning">Due in 5 days</span></td></tr></tbody>
  </table>
</section>
```

## House rules

- Staff are named by **first name only** (Douglas, Heather, Jen, Janet, Lesley, Lorna, Liam): never "Dr" or "Nurse".
- Sign-in is a name tile plus a 4-digit PIN; there is no email or password sign-in.
- British English, sentence case, money in £ with `vd-mono`.
