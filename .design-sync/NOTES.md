# Design sync notes

- **VetDesk is Blazor, so this is a tokens-only design system.** There are no JavaScript components; the skill's
  converter (`package-build.mjs`) has nothing to build. The upload is produced off-script by `.design-sync/build.mjs`:
  `styles.css` (Google Fonts IBM Plex + `tokens/vetdesk.css`, the app's real stylesheet copied verbatim), an empty
  `_ds_bundle.js` with a valid `@ds-bundle` header (`window.VetDesk = {}`), `README.md` = `conventions.md` + `guide.md`,
  `_ds_sync.json`, and `_ds_needs_recompile`.
- **Rebuild and validate:** `node .design-sync/build.mjs --lib <skill>/lib`, then
  `node <skill>/package-validate.mjs ./ds-bundle --no-render-check`. `--no-render-check` is right here: there are no
  preview cards to render. Expected warnings: `[FONT_REMOTE]` (fonts come from Google Fonts on purpose, as the design
  canvas loads them) and `[RENDER_SKIPPED]`.
- **build.mjs refuses to build** if the README names a `vd-*` class or `--vd-*` token that `vetdesk.css` doesn't
  define. Fix the guide, not the check.
- **Verifying the guide:** every `html` code block in the README was rendered in Chrome against `ds-bundle/styles.css`
  (a throwaway page built from the blocks, `[icon]` swapped for an inline SVG) and matched the running app. Repeat
  that whenever `guide.md` or `vetdesk.css` changes.
- **In the guide, `[icon]` marks an inline SVG.** Don't use "…" for icons: it also appears in real placeholders
  ("07…").
- **Upload:** all six files at the project root. There are no `components/`, `_preview/`, `_vendor/` or `fonts/`
  directories, so the reconciliation delete pass has nothing to remove.

## Re-sync risks

- `vetdesk.css` changes with every UI change in the app, so re-run the build and re-upload after styling work.
- If a component's markup changes in the Blazor app (a renamed class, a new wrapper), update `guide.md` to match;
  the class check only proves names exist, not that the structure is still right.
- Staff names and the first-name-only and PIN sign-in rules are written into `conventions.md`. Update them if the
  practice's staff or sign-in change.
