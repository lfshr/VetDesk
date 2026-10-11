// Builds the Claude Design upload for VetDesk into ./ds-bundle.
//
// VetDesk's components are Blazor, so there is no JavaScript bundle to ship: this is a "tokens-only" design system.
// It uploads the app's real stylesheet (every --vd-* token and vd-* class), the fonts, and a README that teaches the
// design agent the markup each component renders. The README is conventions.md followed by guide.md.
//
// Usage (from the repo root): node .design-sync/build.mjs [--lib <design-sync skill>/lib]
// With --lib, the _ds_sync.json style hash uses the skill's own hashing, so a future sync can diff against it.

import { createHash } from 'node:crypto';
import { mkdirSync, readFileSync, rmSync, writeFileSync, copyFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { pathToFileURL } from 'node:url';

const root = resolve('.');
const cfg = JSON.parse(readFileSync(join(root, '.design-sync/config.json'), 'utf8'));
const out = join(root, 'ds-bundle');
const libFlag = process.argv.indexOf('--lib');
const lib = libFlag > 0 ? process.argv[libFlag + 1] : null;

const css = readFileSync(join(root, cfg.stylesheet), 'utf8');
const conventions = readFileSync(join(root, cfg.readmeHeader), 'utf8').trim();
const guide = readFileSync(join(root, '.design-sync/guide.md'), 'utf8').trim();
const readme = `${conventions}\n\n${guide}\n`;

// The README is trusted by the design agent, so every class and token it names must exist in the stylesheet.
const classes = new Set([...css.matchAll(/\.(vd-[a-z0-9-]+)/g)].map((m) => m[1]));
const tokens = new Set([...css.matchAll(/(--vd-[a-z0-9-]+)\s*:/g)].map((m) => m[1]));
const unknownClasses = [...new Set([...readme.matchAll(/(?<![-\w])(vd-[a-z0-9-]+)\b/g)].map((m) => m[1]))]
  .filter((c) => !classes.has(c) && !c.endsWith('-'));
// `--vd-{success,…}-{bg,text}` in the README is shorthand; expand it before checking.
const unknownTokens = [...new Set([...readme.matchAll(/(--vd-[a-z0-9-]+)(?![{\w-])/g)].map((m) => m[1]))]
  .filter((t) => !tokens.has(t));
if (unknownClasses.length || unknownTokens.length) {
  console.error(`README names things the stylesheet doesn't define: ${[...unknownClasses, ...unknownTokens].join(', ')}`);
  process.exit(1);
}

rmSync(out, { recursive: true, force: true });
mkdirSync(join(out, 'tokens'), { recursive: true });

copyFileSync(join(root, cfg.stylesheet), join(out, 'tokens/vetdesk.css'));
writeFileSync(join(out, 'styles.css'), [
  '/* VetDesk: the app\'s own stylesheet, plus IBM Plex from Google Fonts (as the design canvas loads it). */',
  '@import url("https://fonts.googleapis.com/css2?family=IBM+Plex+Sans:wght@400;500;600;700&family=IBM+Plex+Mono:wght@400;500&display=swap");',
  '@import "./tokens/vetdesk.css";',
  '',
].join('\n'));

const header = { namespace: cfg.globalName, components: [], sourceHashes: {}, inlinedExternals: [], builtBy: 'cc-design-sync' };
writeFileSync(join(out, '_ds_bundle.js'),
  `/* @ds-bundle: ${JSON.stringify(header)} */\n` +
  `// No JavaScript components: VetDesk's screens are Blazor. Build with plain HTML and the vd-* classes (see README).\n` +
  `window.${cfg.globalName} = window.${cfg.globalName} || {};\n`);

writeFileSync(join(out, 'README.md'), readme);
writeFileSync(join(out, '_ds_needs_recompile'), JSON.stringify({ by: 'design-sync-cli' }));
writeFileSync(join(out, '.ds-build-meta.json'), JSON.stringify({ componentCount: 0, shape: cfg.shape, tokensOnly: true }, null, 2));

const sha = (buf) => createHash('sha256').update(buf).digest('hex');
let styleSha;
if (lib) {
  const { styleShaFor } = await import(pathToFileURL(join(lib, 'sync-hashes.mjs')).href);
  styleSha = styleShaFor(out, { includeBundleBody: true });
} else {
  styleSha = sha(readFileSync(join(out, 'styles.css')) + readFileSync(join(out, 'tokens/vetdesk.css')));
}
writeFileSync(join(out, '_ds_sync.json'), JSON.stringify({
  shape: cfg.shape,
  styleSha,
  renderHashes: {},
  sourceKeys: {},
  sourceHashes: {},
  auxSha: sha(readme).slice(0, 12),
  bundleSha12: sha(readFileSync(join(out, '_ds_bundle.js'))).slice(0, 12),
}, null, 2));

console.log(`ds-bundle built: tokens-only, ${classes.size} classes, ${tokens.size} tokens, README ${readme.length} chars`);
