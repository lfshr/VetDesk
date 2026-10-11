# Design sync notes

How VetDesk reaches Claude Design (claude.ai/design), and what to watch on a re-sync.

## How it works

VetDesk's UI is Blazor (`src/VetDesk.UI`), and Claude Design renders React. So `.design-sync/react/` is a small React package, `@vetdesk/ui`, that mirrors the shared Razor components in `src/VetDesk.UI/Shared/` with the same markup and classes. It uses the app's own `wwwroot/vetdesk.css` unchanged (`copy-css.mjs` copies it into `dist/`). The converter bundles that package, the stylesheet and IBM Plex (from @fontsource, OFL-licensed) into the upload.

- Config: `config.json` (shape `package`, global `window.VetDesk`).
- Agent docs: `docs/<Name>.md`, and `conventions.md` is prepended to the bundle README via `readmeHeader`.
- Preview cards: `previews/<Name>.tsx`, written from the app's real screens.
- `ScreenTitle` is not mirrored because it renders no markup.

## Commands

```sh
cd .design-sync/react && npm ci && npm run build      # the package
node .design-sync/react/gen-icon.mjs                  # regenerate Icon.tsx from Icon.razor
node .design-sync/verify/compare.mjs [case-prefix…]   # mirror check against the real Blazor render
```

The sync itself runs from Claude Code with `/design-sync` (needs `/design-login` once on that machine; cloud sessions can't authorise it).

## The mirror check

`verify/compare.mjs` renders each case in `verify/make-cases.mjs` twice: the real Razor component through bUnit (`verify/RefRender`, needs the .NET 10 SDK), and the React mirror in Chromium with the same clicks and typing. It compares the normalised DOM and the pixels (threshold 0). At the last sync all 137 cases matched. `alert-delete` is pixels-only because the app's ConfirmHost sits elsewhere in the layout.

- In a cloud session, install .NET with `apt-get install -y dotnet-sdk-10.0` (dot.net downloads are blocked). Chromium is at `/opt/pw-browsers/chromium` and needs playwright 1.56.x.
- `KEEP_SHOTS=1` keeps the screenshots in `verify/.out/`.

## Re-sync risks

- **The mirrors drift silently.** Any change to `Shared/*.razor` or the DTOs they take must be copied into `react/src/` and re-checked with `compare.mjs` before syncing. Icons regenerate with `gen-icon.mjs`.
- The DTO shapes are inlined in `config.json` → `dtsPropsFor`, and the AlertEditor presets are copied into `AlertEditor.tsx` (`alertPresets`). Both go stale if the app changes them.
- The search components (PatientSearch, ProductSearch, PatientPicker) filter the arrays they're given instead of calling the backend. `react/src/search.ts` copies the app's matching rules.

## Known quirks, mirrored on purpose

- The app's PatientPicker writes `aria-expanded` as a bare attribute (an accessibility bug in the app). The mirror copies it so the DOMs match. Fix it in the Razor first, then here.
- The Kpi ReportsRow card wraps to 2×2 because of vetdesk.css's narrow-viewport rule.
- The WitnessPicker select truncates long job titles, as the app does.
- en-GB formatting writes September as "Sept".

## Preview cards

- Modal, ConfirmDialog and Toasts use `position: fixed`. Their previews wrap them in a Stage (`position: relative; transform: translateZ(0)`) so the overlay draws inside the card, and they use single-card mode.
- Wide components use column card mode (see `overrides` in `config.json`).
