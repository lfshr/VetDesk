// Regenerates src/Icon.tsx from the real src/VetDesk.UI/Shared/Icon.razor, so the icon set never drifts by hand.
//   node gen-icon.mjs
import { readFileSync, writeFileSync } from "node:fs";

const razor = readFileSync(new URL("../../src/VetDesk.UI/Shared/Icon.razor", import.meta.url), "utf8");
const cases = [...razor.matchAll(/case "([^"]+)":\s*\n([\s\S]*?)\n\s*break;/g)].map(m => [m[1], m[2]]);
if (cases.length === 0) throw new Error("No icons found in Icon.razor: has its markup changed shape?");

const out = [
  "// Generated from src/VetDesk.UI/Shared/Icon.razor (same names, same paths) by gen-icon.mjs. Don't hand-edit.",
  'import type { ReactNode } from "react";',
  "",
  "export type IconName =\n  | " + cases.map(([n]) => `"${n}"`).join("\n  | ") + ";",
  "",
  "const paths: Record<IconName, ReactNode> = {",
  ...cases.map(([n, body]) => `  "${n}": <>${body.trim().split("\n").map(l => l.trim()).join(" ")}</>,`),
  "};",
  "",
  `export interface IconProps {
  /** Which icon. */
  name: IconName;
  /** Width and height in pixels. Defaults to 20. */
  size?: number;
}

/**
 * Line icon from the VetDesk design (24px grid, stroked in the current text colour, so colour it with \`color\`).
 * Mirrors Shared/Icon.razor.
 */
export function Icon({ name, size = 20 }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8"
      strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" focusable="false">
      {paths[name]}
    </svg>
  );
}`,
];
writeFileSync(new URL("./src/Icon.tsx", import.meta.url), out.join("\n") + "\n");
console.log(`wrote ${cases.length} icons`);
