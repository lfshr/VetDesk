// Prints each dialog in a design artboard: its width, title, labels, buttons and other visible text.
import { readFileSync } from 'node:fs';
const file = process.argv[2];
const html = readFileSync(file, 'utf8').split('<script type="text/x-dc"')[0];
const re = /<div role="(?:alert)?dialog"[^>]*?max-width: (\d+)px[^>]*>/g;
let m;
while ((m = re.exec(html))) {
  // Walk forward to the matching close of this div.
  let depth = 0, i = m.index, end = i;
  const tag = /<\/?div\b[^>]*>/g; tag.lastIndex = i;
  let t;
  while ((t = tag.exec(html))) { depth += t[0].startsWith('</') ? -1 : 1; if (depth === 0) { end = t.index; break; } }
  const block = html.slice(m.index, end);
  const text = block.replace(/<svg[\s\S]*?<\/svg>/g, '').replace(/<option[^>]*>([^<]*)<\/option>/g, ' [opt:$1] ')
    .replace(/<button[^>]*>/g, ' [btn: ').replace(/<\/button>/g, '] ').replace(/<a [^>]*>/g, ' [link: ').replace(/<\/a>/g, '] ')
    .replace(/<input[^>]*type="(checkbox|radio)"[^>]*>/g, ' [$1] ').replace(/<input[^>]*placeholder="([^"]*)"[^>]*>/g, ' [input:$1] ').replace(/<input[^>]*>/g, ' [input] ')
    .replace(/<textarea[^>]*>/g, ' [textarea] ').replace(/<select[^>]*>/g, ' [select ').replace(/<\/select>/g, '] ')
    .replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').trim();
  console.log(`--- max-width ${m[1]}: ${text.slice(0, 1200)}\n`);
}
