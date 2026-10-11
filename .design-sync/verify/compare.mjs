// Checks the React mirrors in .design-sync/react against the real Blazor components.
//
//   node .design-sync/verify/compare.mjs [case-id-prefix ...]
//
// 1. Renders every case in cases.json with the Razor component (RefRender: bUnit, with a fake backend answering
//    the searches from the case's data) after the case's actions (type, pick, click).
// 2. Mounts the React mirror in Chromium with the same props and performs the same actions with Playwright.
// 3. Compares the two DOMs after normalising markup that renders identically (whitespace, attribute and class
//    order, how a style or a select's choice is written, the random dialog title id).
// 4. Screenshots both with vetdesk.css and the IBM Plex fonts and counts differing pixels.
// Exits non-zero if any case differs. Screenshots of failures go to verify/.out/.
import { execFileSync } from "node:child_process";
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { createRequire } from "node:module";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));
const reactPkg = join(here, "../react");
const require = createRequire(join(reactPkg, "package.json"));
const { chromium } = require("playwright-core");
const { PNG } = require("pngjs");
const pixelmatch = require("pixelmatch").default ?? require("pixelmatch");
const esbuild = require("esbuild");

const only = process.argv.slice(2);
const cases = JSON.parse(readFileSync(join(here, "cases.json"), "utf8")).filter(c => !only.length || only.some(o => c.id.startsWith(o)));
const outDir = join(here, ".out");
mkdirSync(outDir, { recursive: true });
writeFileSync(join(outDir, "cases.json"), JSON.stringify(cases));

execFileSync("dotnet", ["run", "--project", join(here, "RefRender"), "--", join(outDir, "cases.json"), join(outDir, "ref.json")],
  { stdio: ["ignore", "ignore", "inherit"] });
const ref = JSON.parse(readFileSync(join(outDir, "ref.json"), "utf8"));

await esbuild.build({ entryPoints: [join(here, "harness.tsx")], bundle: true, format: "iife", outfile: join(outDir, "harness.js"),
  nodePaths: [join(reactPkg, "node_modules")], define: { "process.env.NODE_ENV": '"production"' }, logLevel: "warning" });
const harness = readFileSync(join(outDir, "harness.js"), "utf8");

// The fonts the Claude Design bundle ships, so both sides render in IBM Plex as designs will.
const fontCss = ["ibm-plex-sans/latin-400.css", "ibm-plex-sans/latin-500.css", "ibm-plex-sans/latin-600.css", "ibm-plex-sans/latin-700.css", "ibm-plex-mono/latin-400.css"]
  .map(f => { const p = require.resolve(`@fontsource/${f}`); return readFileSync(p, "utf8").replace(/url\(\.\/files\//g, `url(file://${dirname(p)}/files/`); }).join("\n");
const css = readFileSync(join(here, "../../src/VetDesk.UI/wwwroot/vetdesk.css"), "utf8");
const head = `<meta charset="utf-8"><style>${fontCss}</style><style>${css}</style>
<style>html,body{height:auto;background:#fff}#case{display:inline-block;padding:12px;width:640px;position:relative;transform:translateZ(0)}
/* overlays are position:fixed; the transform makes #case their containing block, so give it a fixed-size stage */
#case:has(.vd-overlay){width:680px;height:560px}
/* toasts sit fixed in the corner */
#case:has(.vd-toasts){height:260px}</style>`;
const page = body => `<!doctype html><html><head>${head}</head><body><div id="case">${body}</div></body></html>`;

// Serialises a DOM subtree so markup that renders the same compares equal.
const normalise = () => {
  const walk = n => {
    if (n.nodeType === 3) { const t = n.textContent.replace(/\s+/g, " "); return t.trim() ? t : ""; }
    if (n.nodeType !== 1) return "";
    const attrs = [...n.attributes]
      // bUnit marks event handlers and element references (blazor:onclick="1"); the browser never sees them.
      .filter(a => !a.name.startsWith("blazor:"))
      // An empty value on an input renders the same as no value.
      .filter(a => !(n.localName === "input" && a.name === "value" && a.value === ""))
      // A select is compared by the option it shows, however that was marked up.
      .filter(a => !(n.localName === "select" && a.name === "value") && !(n.localName === "option" && a.name === "selected"))
      .map(a => {
        if (a.name === "class") return `class="${a.value.split(/\s+/).filter(Boolean).sort().join(" ")}"`;
        if (a.name === "style") return `style="${n.style.cssText}"`;
        // Modal titles get a random id on both sides.
        if ((a.name === "id" || a.name === "aria-labelledby") && /^dlg-/.test(a.value)) return `${a.name}="dlg-*"`;
        return `${a.name}="${a.value}"`;
      })
      .filter(a => a !== 'class=""')
      .concat(n.localName === "select" ? [`(shows)="${n.value}"`] : [])
      .sort().join(" ");
    return `<${n.localName}${attrs ? " " + attrs : ""}>${[...n.childNodes].map(walk).join("").replace(/\s+/g, " ").trim()}</${n.localName}>`;
  };
  return [...document.getElementById("case").childNodes].map(walk).join("").replace(/> </g, "><");
};

// Writes live state (typed values, chosen options) into attributes so the grabbed HTML shows what the page shows.
const freeze = () => {
  const root = document.getElementById("case");
  for (const i of root.querySelectorAll("input")) if (i.value) i.setAttribute("value", i.value);
  for (const s of root.querySelectorAll("select")) for (const o of s.options) o.toggleAttribute("selected", o.selected && s.value !== "");
  return root.innerHTML;
};

const browser = await chromium.launch({ executablePath: process.env.CHROMIUM_PATH || "/opt/pw-browsers/chromium" });
const tab = await browser.newPage({ viewport: { width: 720, height: 600 }, deviceScaleFactor: 1, locale: "en-GB", timezoneId: "Europe/London" });
const shoot = async html => {
  await tab.setContent(page(html));
  // Blazor sets a bound select's choice as a property at runtime; its markup carries it as a value attribute.
  await tab.evaluate(() => { for (const s of document.querySelectorAll("select[value]")) s.value = s.getAttribute("value"); });
  await tab.evaluate(() => document.fonts.ready);
  return { dom: await tab.evaluate(normalise), png: await tab.locator("#case").screenshot() };
};
const reactHtml = async c => {
  await tab.setContent(page(""));
  await tab.addScriptTag({ content: harness });
  await tab.evaluate(([name, props]) => window.mount(name, props), [c.component, c.reactProps ?? c.props ?? {}]);
  await tab.waitForTimeout(30);
  for (const a of c.reactActions ?? c.actions ?? []) {
    if (a.wait) { await tab.waitForTimeout(a.wait); continue; }
    const el = tab.locator(`#case ${a.selector}`).first();
    if (a.fill !== undefined) await el.fill(a.fill);
    else if (a.select !== undefined) await el.selectOption(a.select);
    else if (a.click) await el.click();
    else if (a.key) await el.press(a.key);
    await tab.waitForTimeout(30);
  }
  return tab.evaluate(freeze);
};

let failures = 0;
for (const c of cases) {
  const mirrorHtml = await reactHtml(c);
  const a = await shoot(ref[c.id]);
  const b = await shoot(mirrorHtml);
  const ia = PNG.sync.read(a.png), ib = PNG.sync.read(b.png);
  let diffPixels = -1;
  if (ia.width === ib.width && ia.height === ib.height) diffPixels = pixelmatch(ia.data, ib.data, null, ia.width, ia.height, { threshold: 0 });
  // A case can accept a DOM difference that renders identically, with its reason (pixelsOnly), never a pixel one.
  const domSame = a.dom === b.dom;
  const ok = (domSame || !!c.pixelsOnly) && diffPixels === 0;
  if (process.env.KEEP_SHOTS || !ok) {
    writeFileSync(join(outDir, `${c.id}.blazor.png`), a.png);
    writeFileSync(join(outDir, `${c.id}.react.png`), b.png);
  }
  if (!ok) {
    failures++;
    console.log(`DIFF ${c.id}: dom ${domSame ? "same" : "differs"}, pixels ${diffPixels < 0 ? `size ${ia.width}x${ia.height} vs ${ib.width}x${ib.height}` : diffPixels}`);
    if (!domSame) { let i = 0; while (a.dom[i] === b.dom[i]) i++; console.log(`  blazor: …${a.dom.slice(Math.max(0, i - 150), i + 200)}\n  react:  …${b.dom.slice(Math.max(0, i - 150), i + 200)}`); }
  } else {
    console.log(`same ${c.id} (${ia.width}x${ia.height})${domSame ? "" : ` pixels only: ${c.pixelsOnly}`}`);
  }
}
await browser.close();
console.log(failures ? `${failures} of ${cases.length} cases differ` : `all ${cases.length} cases match the Blazor render`);
process.exit(failures ? 1 : 0);
