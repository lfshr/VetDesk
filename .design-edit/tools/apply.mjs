// Applies exact, once-only text replacements to a file; exits non-zero if any target isn't found exactly once.
import { readFileSync, writeFileSync } from 'node:fs';
const [file, specFile] = process.argv.slice(2);
let text = readFileSync(file, 'utf8');
const crlf = text.includes('\r\n');
if (crlf) text = text.replace(/\r\n/g, '\n');
const pairs = JSON.parse(readFileSync(specFile, 'utf8'));
for (const [from, to] of pairs) {
  const count = text.split(from).length - 1;
  if (count !== 1) { console.error(`Expected 1 match, found ${count}: ${from.slice(0, 80)}`); process.exit(1); }
  text = text.replace(from, () => to);
}
writeFileSync(file, crlf ? text.replace(/\n/g, '\r\n') : text);
console.log(`${file}: ${pairs.length} replacements`);
